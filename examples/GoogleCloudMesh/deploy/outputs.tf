output "inbox_topics" {
  value       = { for k, t in google_pubsub_topic.inbox : k => t.id }
  description = "Per-consumer Pub/Sub inbox topic ids (projects/*/topics/*) — producers publish to these."
}

output "runtime_service_account" {
  value       = google_service_account.runtime.email
  description = "The service account every function runs as."
}

output "mesh_bucket" {
  value       = google_storage_bucket.mesh.name
  description = "The GCS bucket the mesh writes its catalog artifacts to."
}
