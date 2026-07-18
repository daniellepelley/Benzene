output "mesh_ui_url" {
  description = "Open this in a browser to see the Mesh UI (the catalog of discovered services)."
  value       = "${aws_apigatewayv2_api.mesh.api_endpoint}/mesh-ui"
}

output "mesh_manifest_url" {
  description = "The catalog manifest the Mesh UI reads (handy for curl-ing the raw output)."
  value       = "${aws_apigatewayv2_api.mesh.api_endpoint}/manifest.json"
}

output "mesh_refresh_url" {
  description = "POST here to trigger a discovery + aggregation pass on demand (instead of waiting for the schedule)."
  value       = "${aws_apigatewayv2_api.mesh.api_endpoint}/mesh/refresh"
}

output "service_spec_ui_urls" {
  description = "Each service's Spec UI — open in a browser to explore its contract."
  value       = { for k, api in aws_apigatewayv2_api.service : k => "${api.api_endpoint}/benzene/spec-ui" }
}

output "service_health_urls" {
  description = "Each service's aggregated health endpoint."
  value       = { for k, api in aws_apigatewayv2_api.service : k => "${api.api_endpoint}/benzene/health" }
}

output "artifact_bucket" {
  description = "The S3 bucket holding the discovered registry.json and the generated catalog artifacts."
  value       = aws_s3_bucket.artifacts.id
}
