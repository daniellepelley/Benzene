# Benzene example Azure Function App - Terraform equivalent of main.bicep in this directory:
# an HTTP-triggered isolated-worker Function App (see HttpFunction.cs/StartUp.cs) plus the
# Storage Account the Functions runtime requires and a workspace-based Application Insights
# resource for telemetry.
#
# This template has been hand-checked against the azurerm provider docs but not run through
# `terraform validate`/`terraform plan` or actually deployed - no Terraform CLI is available in
# the environment this was authored in. Review before using in production, and check the
# azurerm provider's current constraints (especially the .NET version accepted by
# `application_stack.dotnet_version` - see the comment on that block). The equivalent
# step-by-step `az cli` walkthrough is in ../../../docs/azure-functions.md.
#
# Deploy:
#   terraform init
#   terraform plan  -var function_app_name=my-benzene-function
#   terraform apply -var function_app_name=my-benzene-function
#
# Unlike main.bicep (which deploys into a resource group you create with `az group create`),
# Terraform manages the resource group itself, per Terraform convention.
#
# This only provisions the HTTP trigger path the example project actually uses. If you add
# Event Hub, Kafka, Service Bus, Cosmos DB, Queue/Blob Storage, or Event Grid triggers (see
# docs/azure-functions.md's "Non-HTTP triggers" section), add the corresponding namespace/
# account resources - and their role assignments if you use managed identity
# (docs/cookbooks/managed-identity.md has the role table and an example
# azurerm_role_assignment is sketched at the bottom of this file).

terraform {
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

variable "function_app_name" {
  type        = string
  description = "Name of the Function App. Must be globally unique."
}

variable "location" {
  type        = string
  description = "Azure region for all resources."
  default     = "eastus"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group to create."
  default     = "benzene-example-rg"
}

variable "hosting_plan_sku" {
  type        = string
  description = "Consumption (Y1) is the default; switch to EP1 (Elastic Premium) or a Dedicated SKU for VNet integration or no cold starts."
  default     = "Y1"
}

locals {
  # Must be globally unique, lowercase, 3-24 chars, no hyphens - same derivation as main.bicep.
  storage_account_name = "${substr(replace(lower(var.function_app_name), "-", ""), 0, 17)}stor"
}

resource "azurerm_resource_group" "example" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "example" {
  name                            = local.storage_account_name
  resource_group_name             = azurerm_resource_group.example.name
  location                        = azurerm_resource_group.example.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
}

# Workspace-based Application Insights - the current recommended mode (classic, non-workspace
# App Insights is being retired). See docs/monitoring.md and
# docs/cookbooks/logging-application-insights.md for what Benzene logs into this.
resource "azurerm_log_analytics_workspace" "example" {
  name                = "${var.function_app_name}-logs"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "example" {
  name                = "${var.function_app_name}-ai"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  workspace_id        = azurerm_log_analytics_workspace.example.id
  application_type    = "web"
}

resource "azurerm_service_plan" "example" {
  name                = "${var.function_app_name}-plan"
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  os_type             = "Linux"
  sku_name            = var.hosting_plan_sku
}

resource "azurerm_linux_function_app" "example" {
  name                = var.function_app_name
  resource_group_name = azurerm_resource_group.example.name
  location            = azurerm_resource_group.example.location
  service_plan_id     = azurerm_service_plan.example.id

  # Key-based, mirroring main.bicep: the classic Consumption plan's deployment content share
  # still requires key access - see docs/cookbooks/managed-identity.md's Consumption-plan
  # caveat. On Elastic Premium/Dedicated/Flex you can switch to
  # storage_uses_managed_identity = true and drop the access key.
  storage_account_name       = azurerm_storage_account.example.name
  storage_account_access_key = azurerm_storage_account.example.primary_access_key

  https_only = true

  # System-assigned managed identity: grant RBAC roles to the principal_id output below for
  # identity-based trigger connections - see docs/cookbooks/managed-identity.md.
  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.example.connection_string

    application_stack {
      # .NET 10 has no published isolated-worker runtime identifier at the time of writing
      # (same caveat as main.bicep's DOTNET-ISOLATED|10.0 linuxFxVersion) - and the azurerm
      # provider validates dotnet_version against a fixed list, so check the provider release
      # notes and adjust if your provider version doesn't accept "10.0" yet.
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    WEBSITE_RUN_FROM_PACKAGE = "1"
  }
}

# Example role assignment for identity-based trigger connections - uncomment and adapt when you
# add a Service Bus/Event Hubs/Storage resource (Cosmos DB data-plane roles use
# azurerm_cosmosdb_sql_role_assignment instead - see docs/cookbooks/managed-identity.md):
#
# resource "azurerm_role_assignment" "service_bus_receiver" {
#   scope                = azurerm_servicebus_namespace.example.id
#   role_definition_name = "Azure Service Bus Data Receiver"
#   principal_id         = azurerm_linux_function_app.example.identity[0].principal_id
# }

output "function_app_host_name" {
  description = "Default hostname of the deployed Function App."
  value       = azurerm_linux_function_app.example.default_hostname
}

output "function_app_principal_id" {
  description = "Object id of the Function App's system-assigned managed identity - the principal to grant RBAC roles to (see docs/cookbooks/managed-identity.md)."
  value       = azurerm_linux_function_app.example.identity[0].principal_id
}

output "application_insights_connection_string" {
  description = "Application Insights connection string (also set on the Function App)."
  value       = azurerm_application_insights.example.connection_string
  sensitive   = true
}
