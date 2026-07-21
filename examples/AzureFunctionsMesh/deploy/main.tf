terraform {
  required_version = ">= 1.5.0"
  # Remote state in Azure Blob so it survives between CI runs (configured at init via -backend-config).
  backend "azurerm" {}
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    # For the identity-propagation delay before the role assignments (see time_sleep below).
    time = {
      source  = "hashicorp/time"
      version = "~> 0.9"
    }
  }
}

provider "azurerm" {
  features {}
  # The workflow registers the resource providers this stack needs (Storage, Web, ManagedIdentity)
  # before Terraform runs, so the provider need not mass-register on every apply.
  resource_provider_registrations = "none"
}

data "azurerm_client_config" "current" {}

locals {
  # The three Cloud Services (same deployable, MESH_SERVICE selects the domain). Tagged for discovery.
  services = ["orders", "payments", "shipping"]
}

# The resource group is bootstrapped imperatively by the workflow (`az group create`, idempotent) before
# Terraform runs — it holds the remote-state storage account — so Terraform reads it rather than owning it.
data "azurerm_resource_group" "this" {
  name = var.resource_group
}

# --- Storage: the Functions runtime store AND the mesh catalog blob container --------------------------
resource "azurerm_storage_account" "this" {
  name                     = var.storage_account
  resource_group_name      = data.azurerm_resource_group.this.name
  location                 = data.azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "mesh" {
  name                  = "mesh"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

# --- Consumption plan (Linux) ------------------------------------------------------------------------
resource "azurerm_service_plan" "this" {
  name                = "${var.project}-plan"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

# --- The three Cloud Service Function Apps (tagged for discovery) -------------------------------------
resource "azurerm_linux_function_app" "service" {
  for_each            = toset(local.services)
  name                = "${var.project}-${each.value}"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  service_plan_id     = azurerm_service_plan.this.id

  storage_account_name       = azurerm_storage_account.this.name
  storage_account_access_key = azurerm_storage_account.this.primary_access_key

  # Discovery finds services by this tag; the mesh Function App deliberately does NOT carry it.
  tags = { (var.discovery_tag_key) = "true" }

  site_config {
    # The mesh (and the platform) probe the Cloud Service's own health endpoint.
    # azurerm requires the eviction window whenever a health_check_path is set.
    health_check_path                 = "/benzene/health"
    health_check_eviction_time_in_min = 5
    application_stack {
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    MESH_SERVICE = each.value
  }
}

# --- The mesh Function App (NOT tagged) with a managed identity that can read resources + write blobs ---
resource "azurerm_linux_function_app" "mesh" {
  name                = "${var.project}-mesh"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  service_plan_id     = azurerm_service_plan.this.id

  storage_account_name       = azurerm_storage_account.this.name
  storage_account_access_key = azurerm_storage_account.this.primary_access_key

  identity { type = "SystemAssigned" }

  site_config {
    # The static Mesh UI returns 200, so the platform can detect and recycle a wedged mesh instance.
    # azurerm requires the eviction window whenever a health_check_path is set.
    health_check_path                 = "/mesh-ui"
    health_check_eviction_time_in_min = 5
    application_stack {
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    MESH_BLOB_URI       = azurerm_storage_account.this.primary_blob_endpoint
    MESH_BLOB_CONTAINER = azurerm_storage_container.mesh.name
    MESH_REGION         = data.azurerm_resource_group.this.location
    # Scope discovery explicitly to this deployment so a subscription-scoped Reader can't widen the sweep.
    MESH_SUBSCRIPTION_ID = data.azurerm_client_config.current.subscription_id
    MESH_RESOURCE_GROUP  = data.azurerm_resource_group.this.name
  }
}

# A system-assigned identity's principal must propagate to Entra ID before a role assignment that
# references it will succeed; on a cold subscription the assignments below otherwise intermittently fail
# the first apply with PrincipalNotFound. A short delay after the Function App exists removes that race.
resource "time_sleep" "identity_propagation" {
  depends_on      = [azurerm_linux_function_app.mesh]
  create_duration = "30s"
}

# Discover: the mesh identity can read (list) the resources in the resource group.
resource "azurerm_role_assignment" "mesh_reader" {
  scope                = data.azurerm_resource_group.this.id
  role_definition_name = "Reader"
  principal_id         = azurerm_linux_function_app.mesh.identity[0].principal_id
  depends_on           = [time_sleep.identity_propagation]
}

# Persist: the mesh identity can read/write the catalog blobs.
resource "azurerm_role_assignment" "mesh_blob" {
  scope                = azurerm_storage_account.this.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_linux_function_app.mesh.identity[0].principal_id
  depends_on           = [time_sleep.identity_propagation]
}
