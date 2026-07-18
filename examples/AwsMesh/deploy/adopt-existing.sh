#!/usr/bin/env bash
# Idempotently adopt already-existing AWS resources into Terraform state.
#
# Why this exists: the first deploy created real resources, but its local state file died with the
# ephemeral CI runner. With remote state now configured, a fresh (empty) state would still collide
# with those resources (409 AlreadyExists). This script imports each one *if* it exists in AWS and
# is *not* already tracked in state, so it is safe to run on every deploy:
#   - clean account  -> discovery finds nothing, every import is skipped, apply creates everything.
#   - after adoption -> resources are in state, every import is skipped, apply updates in place.
#
# Requires: terraform (initialised with the remote backend) + awscli, run from the deploy/ dir.
set -euo pipefail

PROJECT="${PROJECT:-benzene-mesh}"
REGION="${REGION:-eu-west-1}"
ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
BASIC_EXEC="arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"

# Cache the current state addresses once (empty on first run).
STATE_ADDRS="$(terraform state list 2>/dev/null || true)"

tf_import() {
  local addr="$1" id="$2"
  if [ -z "$id" ] || [ "$id" = "None" ]; then
    echo "· skip (not in AWS): $addr"
    return 0
  fi
  if printf '%s\n' "$STATE_ADDRS" | grep -qxF "$addr"; then
    echo "· already in state: $addr"
    return 0
  fi
  echo "· import: $addr  <-  $id"
  terraform import -input=false -var "region=$REGION" "$addr" "$id"
}

# All HTTP API ids with a given name (a failed run can leave duplicate names).
api_ids() {
  aws apigatewayv2 get-apis --region "$REGION" \
    --query "Items[?Name=='$1'].ApiId" --output text 2>/dev/null || true
}

# First integration id under an api ("None" if it has none).
first_integration() {
  aws apigatewayv2 get-integrations --api-id "$1" --region "$REGION" \
    --query 'Items[0].IntegrationId' --output text 2>/dev/null || true
}

# Pick the *complete* api for a name — the one that actually has an integration — so we never adopt a
# half-created duplicate. Falls back to the first id, or empty if the name doesn't exist at all.
pick_complete_api() {
  local first=""
  for id in $(api_ids "$1"); do
    [ -z "$id" ] && continue
    [ -z "$first" ] && first="$id"
    local int; int="$(first_integration "$id")"
    if [ -n "$int" ] && [ "$int" != "None" ]; then
      echo "$id"
      return 0
    fi
  done
  echo "$first"
}

# ---- S3 + IAM (deterministic ids) ----------------------------------------------------------------
tf_import 'aws_s3_bucket.artifacts' "${PROJECT}-${ACCOUNT}"
tf_import 'aws_iam_role.service' "${PROJECT}-service-role"
tf_import 'aws_iam_role.mesh' "${PROJECT}-mesh-role"
tf_import 'aws_iam_role_policy_attachment.service_logs' "${PROJECT}-service-role/${BASIC_EXEC}"
tf_import 'aws_iam_role_policy_attachment.mesh_logs' "${PROJECT}-mesh-role/${BASIC_EXEC}"
tf_import 'aws_iam_role_policy.mesh' "${PROJECT}-mesh-role:${PROJECT}-mesh-policy"

# ---- Lambda functions + permissions --------------------------------------------------------------
for svc in orders payments shipping; do
  tf_import "aws_lambda_function.service[\"$svc\"]" "${PROJECT}-${svc}"
  tf_import "aws_lambda_permission.service_api[\"$svc\"]" "${PROJECT}-${svc}/AllowApiGatewayInvoke"
done
tf_import 'aws_lambda_function.mesh' "${PROJECT}-mesh"
tf_import 'aws_lambda_permission.mesh_api' "${PROJECT}-mesh/AllowApiGatewayInvoke"
tf_import 'aws_lambda_permission.mesh_events' "${PROJECT}-mesh/AllowEventBridgeInvoke"

# ---- API Gateway (opaque ids — discover by name) -------------------------------------------------
adopt_http_api() {
  local addr_key="$1" api_name="$2"
  local id; id="$(pick_complete_api "$api_name")"
  if [ -z "$id" ] || [ "$id" = "None" ]; then
    echo "· skip (no API named $api_name)"
    return 0
  fi
  tf_import "aws_apigatewayv2_api.${addr_key}" "$id"

  local int_id; int_id="$(first_integration "$id")"
  [ "$int_id" = "None" ] && int_id=""
  if [ -n "$int_id" ]; then
    tf_import "aws_apigatewayv2_integration.${addr_key}" "$id/$int_id"
  else
    echo "· skip integration (none on $id — apply will create it)"
  fi

  local route_id; route_id="$(aws apigatewayv2 get-routes --api-id "$id" --region "$REGION" \
    --query "Items[?RouteKey=='\$default'].RouteId | [0]" --output text 2>/dev/null || true)"
  [ "$route_id" = "None" ] && route_id=""
  if [ -n "$route_id" ]; then
    tf_import "aws_apigatewayv2_route.${addr_key}" "$id/$route_id"
  else
    echo "· skip route (none on $id — apply will create it)"
  fi

  local stage; stage="$(aws apigatewayv2 get-stages --api-id "$id" --region "$REGION" \
    --query "Items[?StageName=='\$default'].StageName | [0]" --output text 2>/dev/null || true)"
  if [ -n "$stage" ] && [ "$stage" != "None" ]; then
    tf_import "aws_apigatewayv2_stage.${addr_key}" "$id/\$default"
  else
    echo "· skip stage (no \$default stage on $id — apply will create it)"
  fi
}

for svc in orders payments shipping; do
  adopt_http_api "service[\"$svc\"]" "${PROJECT}-${svc}-api"
done
adopt_http_api "mesh" "${PROJECT}-mesh-api"

# ---- EventBridge schedule ------------------------------------------------------------------------
tf_import 'aws_cloudwatch_event_rule.aggregate' "default/${PROJECT}-aggregate"
tf_import 'aws_cloudwatch_event_target.aggregate' "default/${PROJECT}-aggregate/mesh"

echo "Adoption pass complete."
