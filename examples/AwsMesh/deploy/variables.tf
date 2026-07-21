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

variable "adot_collector_layer_arn" {
  description = <<-EOT
    ARN of the AWS Distro for OpenTelemetry (ADOT) collector Lambda layer, region-specific. When set,
    it is attached to every function and Benzene's OTLP exporter is pointed at the layer's in-process
    collector (http://localhost:4317) automatically — the collector's default config forwards OTLP
    traces to X-Ray (awsxray), so the per-middleware spans land as subsegments in the same X-Ray trace
    as the AWS-level segments. Look up the current ARN for your region/architecture from
    https://github.com/aws-observability/aws-otel-lambda (e.g. the aws-otel-collector layer). No
    AWS_LAMBDA_EXEC_WRAPPER is set — these are custom-runtime functions that already emit their own
    spans, so only the collector half of the layer is used. Leave empty to not attach it.
  EOT
  type        = string
  default     = ""
}

variable "otlp_endpoint" {
  description = <<-EOT
    Explicit override for OTEL_EXPORTER_OTLP_ENDPOINT on every function. Usually leave empty and set
    adot_collector_layer_arn instead (which points the exporter at the layer's localhost collector).
    Set this only to target an out-of-process / external collector. Empty AND no ADOT layer = spans are
    recorded but exported nowhere.
  EOT
  type        = string
  default     = ""
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
