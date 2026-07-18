variable "region" {
  description = "AWS region to deploy into."
  type        = string
  default     = "eu-west-1"
}

variable "project" {
  description = "Name prefix for all resources."
  type        = string
  default     = "benzene-mesh"
}

variable "artifact_bucket_name" {
  description = "S3 bucket for the mesh registry + catalog artifacts. Must be globally unique; defaults to <project>-<account-id>."
  type        = string
  default     = ""
}

variable "discovery_tag_key" {
  description = "The resource tag key discovery filters on. Services carry this tag; the mesh Lambda does not (so it never discovers itself)."
  type        = string
  default     = "benzene"
}

variable "lambda_architecture" {
  description = "Lambda architecture. Must match the self-contained publish RID in CI (x86_64 -> linux-x64, arm64 -> linux-arm64)."
  type        = string
  default     = "x86_64"
}

variable "aggregate_schedule" {
  description = "EventBridge schedule expression for the mesh aggregation pass."
  type        = string
  default     = "rate(5 minutes)"
}

# Paths to the built Lambda zip files (each contains a `bootstrap` executable). Produced by CI
# (dotnet publish self-contained -> add bootstrap -> zip). Defaults assume the CI layout.
variable "orders_zip" {
  type    = string
  default = "../artifacts/orders.zip"
}
variable "payments_zip" {
  type    = string
  default = "../artifacts/payments.zip"
}
variable "shipping_zip" {
  type    = string
  default = "../artifacts/shipping.zip"
}
variable "mesh_zip" {
  type    = string
  default = "../artifacts/mesh.zip"
}
