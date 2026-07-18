#!/usr/bin/env bash
# Delete ALL AwsMesh application resources for a clean-slate redeploy — including any duplicate/partial
# API Gateway resources left behind by a run that failed midway (API Gateway allows duplicate names, so
# a half-finished run can leave orphans that make name-based import ambiguous).
#
# This deletes everything the stack manages EXCEPT the Terraform *state* bucket
# (benzene-mesh-tfstate-<account>), which it must never touch. After running this, a normal
# `terraform apply` recreates the whole stack from config and reconciles state.
#
# Idempotent and best-effort: every delete tolerates "not found". Requires awscli.
set -uo pipefail

PROJECT="${PROJECT:-benzene-mesh}"
REGION="${REGION:-eu-west-1}"
ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
ARTIFACT_BUCKET="${PROJECT}-${ACCOUNT}"       # NOTE: distinct from ${PROJECT}-tfstate-${ACCOUNT}

echo "== Deleting HTTP APIs (all duplicates by name) =="
for name in orders payments shipping mesh; do
  api_name="${PROJECT}-${name}-api"
  for api in $(aws apigatewayv2 get-apis --region "$REGION" \
      --query "Items[?Name=='${api_name}'].ApiId" --output text 2>/dev/null); do
    [ -z "$api" ] && continue
    echo "· delete-api $api_name ($api)"
    aws apigatewayv2 delete-api --api-id "$api" --region "$REGION" 2>/dev/null || true
  done
done

echo "== Deleting Lambda functions =="
for name in orders payments shipping mesh; do
  fn="${PROJECT}-${name}"
  echo "· delete-function $fn"
  aws lambda delete-function --function-name "$fn" --region "$REGION" 2>/dev/null || true
done

echo "== Deleting EventBridge schedule =="
aws events remove-targets --rule "${PROJECT}-aggregate" --ids mesh --region "$REGION" 2>/dev/null || true
aws events delete-rule --name "${PROJECT}-aggregate" --region "$REGION" 2>/dev/null || true

echo "== Deleting IAM roles =="
for role in "${PROJECT}-service-role" "${PROJECT}-mesh-role"; do
  for p in $(aws iam list-attached-role-policies --role-name "$role" \
      --query 'AttachedPolicies[].PolicyArn' --output text 2>/dev/null); do
    aws iam detach-role-policy --role-name "$role" --policy-arn "$p" 2>/dev/null || true
  done
  for p in $(aws iam list-role-policies --role-name "$role" \
      --query 'PolicyNames[]' --output text 2>/dev/null); do
    aws iam delete-role-policy --role-name "$role" --policy-name "$p" 2>/dev/null || true
  done
  echo "· delete-role $role"
  aws iam delete-role --role-name "$role" 2>/dev/null || true
done

echo "== Emptying + deleting artifact bucket ($ARTIFACT_BUCKET) =="
if [ "$ARTIFACT_BUCKET" = "${PROJECT}-tfstate-${ACCOUNT}" ]; then
  echo "!! refusing to delete the state bucket" >&2
  exit 1
fi
aws s3 rm "s3://${ARTIFACT_BUCKET}" --recursive --region "$REGION" 2>/dev/null || true
aws s3api delete-bucket --bucket "$ARTIFACT_BUCKET" --region "$REGION" 2>/dev/null || true

echo "Cleanup complete. A subsequent 'terraform apply' will recreate the stack from config."
