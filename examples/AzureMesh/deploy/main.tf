terraform {
  required_version = ">= 1.5.0"
  # Remote state in Azure Blob so it survives between CI runs (configured at init via -backend-config).
  backend "azurerm" {}
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

locals {
  # The three Cloud Services (same image, MESH_SERVICE selects the domain). Tagged for discovery.
  services = ["orders", "payments", "shipping"]
}

resource "azurerm_resource_group" "this" {
  name     = var.resource_group
  location = var.location
}

# --- Container registry (holds the two images CI builds + pushes) ------------------------------------
resource "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "Basic"
  admin_enabled       = true
}

# --- Storage for the mesh catalog artifacts ----------------------------------------------------------
resource "azurerm_storage_account" "artifacts" {
  name                     = var.storage_account
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "mesh" {
  name                  = "mesh"
  storage_account_id    = azurerm_storage_account.artifacts.id
  container_access_type = "private"
}

# --- App Service plan (Linux) ------------------------------------------------------------------------
resource "azurerm_service_plan" "this" {
  name                = "${var.project}-plan"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  os_type             = "Linux"
  sku_name            = "B1"
}

# --- The three Cloud Service Web Apps (tagged for discovery) -----------------------------------------
resource "azurerm_linux_web_app" "service" {
  for_each            = toset(local.services)
  name                = "${var.project}-${each.value}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  service_plan_id     = azurerm_service_plan.this.id

  # Discovery finds services by this tag; the mesh Web App deliberately does NOT carry it.
  tags = { (var.discovery_tag_key) = "true" }

  site_config {
    application_stack {
      docker_registry_url = "https://${azurerm_container_registry.acr.login_server}"
      docker_image_name   = "${var.service_image}:${var.image_tag}"
    }
  }

  app_settings = {
    DOCKER_REGISTRY_SERVER_URL          = "https://${azurerm_container_registry.acr.login_server}"
    DOCKER_REGISTRY_SERVER_USERNAME     = azurerm_container_registry.acr.admin_username
    DOCKER_REGISTRY_SERVER_PASSWORD     = azurerm_container_registry.acr.admin_password
    WEBSITES_PORT                       = "8080"
    PORT                                = "8080"
    MESH_SERVICE                        = each.value
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
  }
}

# --- The mesh Web App (NOT tagged) with a managed identity that can read resources + write blobs ------
resource "azurerm_linux_web_app" "mesh" {
  name                = "${var.project}-mesh"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  service_plan_id     = azurerm_service_plan.this.id

  identity { type = "SystemAssigned" }

  site_config {
    application_stack {
      docker_registry_url = "https://${azurerm_container_registry.acr.login_server}"
      docker_image_name   = "${var.mesh_image}:${var.image_tag}"
    }
  }

  app_settings = {
    DOCKER_REGISTRY_SERVER_URL          = "https://${azurerm_container_registry.acr.login_server}"
    DOCKER_REGISTRY_SERVER_USERNAME     = azurerm_container_registry.acr.admin_username
    DOCKER_REGISTRY_SERVER_PASSWORD     = azurerm_container_registry.acr.admin_password
    WEBSITES_PORT                       = "8080"
    PORT                                = "8080"
    MESH_BLOB_URI                       = azurerm_storage_account.artifacts.primary_blob_endpoint
    MESH_BLOB_CONTAINER                 = azurerm_storage_container.mesh.name
    MESH_REGION                         = azurerm_resource_group.this.location
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
  }
}

# Discover: the mesh identity can read (list) the resources in the resource group.
resource "azurerm_role_assignment" "mesh_reader" {
  scope                = azurerm_resource_group.this.id
  role_definition_name = "Reader"
  principal_id         = azurerm_linux_web_app.mesh.identity[0].principal_id
}

# Persist: the mesh identity can read/write the catalog blobs.
resource "azurerm_role_assignment" "mesh_blob" {
  scope                = azurerm_storage_account.artifacts.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_linux_web_app.mesh.identity[0].principal_id
}
