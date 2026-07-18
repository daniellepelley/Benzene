#!/usr/bin/env bash
# Idempotently adopt already-existing Azure resources into Terraform state.
#
# Why this exists: the deploy resource group (and, after a partial run, other resources) can already
# exist in Azure while Terraform's remote state does not yet track them - the workflow's own init step
# creates the resource group up front to hold the state storage account, and a previous apply may have
# died part-way. A fresh apply then fails with:
#   "a resource with the ID ... already exists - to be managed via Terraform this resource needs to be
#    imported into the State".
# This script imports each resource *if* it isn't already tracked in state; the import is a harmless
# (swallowed) error when the resource doesn't exist in Azure, so it is safe to run on every deploy:
#   - clean subscription  -> every import is a no-op, apply creates everything.
#   - dirty/partial state -> existing resources are adopted, apply continues in place.
#
# Requires: terraform (already `init`-ed with the remote backend) + az (logged in). Run from deploy/.
set -uo pipefail

RG="${RG:-benzene-mesh-rg}"
PROJECT="${PROJECT:-benzene-mesh}"
ACR_NAME="${ACR_NAME:?set ACR_NAME}"
SA="${SA:?set SA (artifacts storage account)}"
LOCATION="${LOCATION:?set LOCATION}"

SUB="$(az account show --query id -o tsv)"
RG_ID="/subscriptions/$SUB/resourceGroups/$RG"
SA_ID="$RG_ID/providers/Microsoft.Storage/storageAccounts/$SA"

# Every import must evaluate the config, which references these variables.
TF_VARS=(-var "location=$LOCATION" -var "acr_name=$ACR_NAME" -var "storage_account=$SA")

# Snapshot the tracked addresses once (empty on first run).
STATE_ADDRS="$(terraform state list 2>/dev/null || true)"

tf_import() {
  local addr="$1" id="$2"
  if printf '%s\n' "$STATE_ADDRS" | grep -qxF "$addr"; then
    echo "· already in state: $addr"
    return 0
  fi
  echo "· try import: $addr  <-  $id"
  terraform import -input=false "${TF_VARS[@]}" "$addr" "$id" \
    || echo "  (skipped: not present in Azure, or not importable)"
}

# NB: the resource group itself is a Terraform *data source* (created imperatively by the workflow),
# so it is never imported here — only the resources this stack actually manages are adopted below.
tf_import 'azurerm_container_registry.acr'    "$RG_ID/providers/Microsoft.ContainerRegistry/registries/$ACR_NAME"
tf_import 'azurerm_storage_account.artifacts' "$SA_ID"
tf_import 'azurerm_storage_container.mesh'    "https://$SA.blob.core.windows.net/mesh"
tf_import 'azurerm_service_plan.this'         "$RG_ID/providers/Microsoft.Web/serverfarms/$PROJECT-plan"
for svc in orders payments shipping; do
  tf_import "azurerm_linux_web_app.service[\"$svc\"]" "$RG_ID/providers/Microsoft.Web/sites/$PROJECT-$svc"
done
tf_import 'azurerm_linux_web_app.mesh'        "$RG_ID/providers/Microsoft.Web/sites/$PROJECT-mesh"

# Role assignments only exist after a phase-2 partial run. Resolve the assignment GUID from the scope +
# the mesh app's managed-identity principal; skip cleanly if the app/identity/assignment isn't there.
MESH_PID="$(az webapp identity show -n "$PROJECT-mesh" -g "$RG" --query principalId -o tsv 2>/dev/null || true)"
if [ -n "$MESH_PID" ]; then
  reader_id="$(az role assignment list --scope "$RG_ID" --assignee "$MESH_PID" \
    --query "[?roleDefinitionName=='Reader'].id | [0]" -o tsv 2>/dev/null || true)"
  blob_id="$(az role assignment list --scope "$SA_ID" --assignee "$MESH_PID" \
    --query "[?roleDefinitionName=='Storage Blob Data Contributor'].id | [0]" -o tsv 2>/dev/null || true)"
  [ -n "${reader_id:-}" ] && [ "$reader_id" != "null" ] && tf_import 'azurerm_role_assignment.mesh_reader' "$reader_id"
  [ -n "${blob_id:-}" ]   && [ "$blob_id" != "null" ]   && tf_import 'azurerm_role_assignment.mesh_blob'   "$blob_id"
fi

echo "adopt-existing: done"
