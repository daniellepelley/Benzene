# Infrastructure for the Google Cloud Mesh example. Terraform provisions the durable infra — Pub/Sub
# inbox topics, the mesh's GCS artifact bucket, a shared service account with the right IAM, and the
# Cloud Scheduler job that periodically refreshes the mesh. The functions themselves (8: one HTTP per
# service, one Pub/Sub per consumer, plus the mesh) are deployed by the CI workflow via
# `gcloud functions deploy`, which creates each function's trigger/subscription — see
# .github/workflows/deploy-google-cloud-mesh-example.yml.
#
# NOT validated against live GCP from this repo — review before production use.

terraform {
  required_version = ">= 1.5"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 6.0"
    }
  }
  backend "gcs" {} # configured at init via -backend-config=bucket=...,prefix=...
}

provider "google" {
  project = var.project
  region  = var.region
}

locals {
  # One Pub/Sub "inbox" topic per consuming service. Producers publish to the target's inbox with the
  # Benzene topic in the "topic" message attribute (Benzene routes by attribute, not by Pub/Sub topic)
  # — the Cloud-Functions-friendly analogue of AwsMesh's one-SQS-queue-per-service.
  inboxes = ["payments", "shipping", "notifications"]
}

# --- Pub/Sub inbox topics -------------------------------------------------------------------------
resource "google_pubsub_topic" "inbox" {
  for_each = toset(local.inboxes)
  name     = "${var.name_prefix}-${each.value}-inbox"
}

# --- Mesh artifact bucket -------------------------------------------------------------------------
resource "google_storage_bucket" "mesh" {
  name                        = "${var.project}-${var.name_prefix}-mesh"
  location                    = var.region
  uniform_bucket_level_access = true
  force_destroy               = true
}

# --- Shared runtime service account ---------------------------------------------------------------
# Every function runs as this identity. It can publish to Pub/Sub (producers) and read/write the mesh
# bucket (the mesh aggregator). Scope down per-function in a real deployment.
resource "google_service_account" "runtime" {
  account_id   = "${var.name_prefix}-runtime"
  display_name = "Benzene GoogleCloudMesh runtime"
}

# Grant publish on each inbox topic (topic-level, not project-level) so provisioning needs only
# pubsub.admin — not the broad resourcemanager.projectIamAdmin a project-level binding would require.
resource "google_pubsub_topic_iam_member" "publisher" {
  for_each = google_pubsub_topic.inbox
  topic    = each.value.name
  role     = "roles/pubsub.publisher"
  member   = "serviceAccount:${google_service_account.runtime.email}"
}

resource "google_storage_bucket_iam_member" "mesh_object_admin" {
  bucket = google_storage_bucket.mesh.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.runtime.email}"
}

# --- Cloud Scheduler: periodic mesh refresh (Cloud Functions has no timer trigger) ----------------
# Hits the mesh function's POST /mesh/refresh on a schedule, authenticated as the runtime SA (the mesh
# function is deployed with --no-allow-unauthenticated in CI). The URL is supplied after the mesh
# function is deployed (var.mesh_refresh_url), so this resource applies on the second Terraform pass.
resource "google_cloud_scheduler_job" "mesh_refresh" {
  count     = var.mesh_refresh_url == "" ? 0 : 1
  name      = "${var.name_prefix}-mesh-refresh"
  schedule  = "*/2 * * * *"
  time_zone = "Etc/UTC"

  http_target {
    http_method = "POST"
    uri         = var.mesh_refresh_url
    oidc_token {
      service_account_email = google_service_account.runtime.email
      audience              = var.mesh_refresh_url
    }
  }
}
