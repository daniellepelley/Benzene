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
  # The six Cloud Services (each its own deployable now). Tagged for discovery. orders/payments/shipping
  # form the command chain + publish events; inventory/notifications/analytics are pure event consumers.
  services = ["orders", "payments", "shipping", "inventory", "notifications", "analytics"]

  # Per-service app settings — the messaging connection strings + entity names each service actually
  # uses (merged with the common settings on each Function App below). Connection strings come from the
  # namespaces created further down; empty for a service that neither sends nor consumes on a transport.
  service_app_settings = {
    orders = {
      ServiceBusConnection = azurerm_servicebus_namespace.this.default_primary_connection_string
      PAYMENTS_QUEUE       = azurerm_servicebus_queue.payments.name
      EventHubConnection   = azurerm_eventhub_namespace.this.default_primary_connection_string
      ORDER_PLACED_HUB     = azurerm_eventhub.order_placed.name
    }
    payments = {
      ServiceBusConnection = azurerm_servicebus_namespace.this.default_primary_connection_string
      SHIPPING_QUEUE       = azurerm_servicebus_queue.shipping.name
      EventGridEndpoint    = azurerm_eventgrid_topic.this.endpoint
      EventGridKey         = azurerm_eventgrid_topic.this.primary_access_key
    }
    shipping = {
      ServiceBusConnection = azurerm_servicebus_namespace.this.default_primary_connection_string
      EventGridEndpoint    = azurerm_eventgrid_topic.this.endpoint
      EventGridKey         = azurerm_eventgrid_topic.this.primary_access_key
    }
    inventory     = { EventHubConnection = azurerm_eventhub_namespace.this.default_primary_connection_string }
    notifications = { EventHubConnection = azurerm_eventhub_namespace.this.default_primary_connection_string }
    analytics     = {}
  }

  # Event Grid routing: which consumer Function's EventGrid-trigger function each event fans out to
  # (matched by the event's type = the Benzene topic). Function name = the BenzeneEventGridTrigger Name.
  eventgrid_routes = {
    "payment_captured-notifications"    = { event_type = "payment:captured", service = "notifications", function = "notifications-eg" }
    "payment_captured-analytics"        = { event_type = "payment:captured", service = "analytics", function = "analytics-eg" }
    "shipment_dispatched-inventory"     = { event_type = "shipment:dispatched", service = "inventory", function = "inventory-eg" }
    "shipment_dispatched-notifications" = { event_type = "shipment:dispatched", service = "notifications", function = "notifications-eg" }
    "shipment_dispatched-analytics"     = { event_type = "shipment:dispatched", service = "analytics", function = "analytics-eg" }
  }
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
      # The deployables are self-contained (they bring their own .NET runtime), so this pin only
      # has to be a version the plan's host supports - see var.dotnet_version.
      dotnet_version              = var.dotnet_version
      use_dotnet_isolated_runtime = true
    }
  }

  # Each service is its own deployable now (no MESH_SERVICE); it gets exactly the messaging connection
  # strings + entity names it uses. FUNCTIONS_WORKER_RUNTIME is set so the isolated worker starts.
  app_settings = merge(
    { FUNCTIONS_WORKER_RUNTIME = "dotnet-isolated" },
    local.service_app_settings[each.value]
  )
}

# ---------------------------------------------------------------------------------------------------
# Inter-service messaging — each transport used for what it's good at:
#   • Service Bus queues (point-to-point commands): orders → payments → shipping.
#   • Event Hub (fan-out stream): orders publishes order:placed → inventory + notifications each read
#     their own consumer group.
#   • Event Grid (routed integration events): payments publishes payment:captured, shipping publishes
#     shipment:dispatched → routed by event type to inventory/notifications/analytics.
# ---------------------------------------------------------------------------------------------------
resource "azurerm_servicebus_namespace" "this" {
  # NB: a Service Bus namespace name may not end with "-sb" or "-mgmt" (reserved), so this is "-bus".
  name                = "${var.project}-bus"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  sku                 = "Standard" # Standard is the cheapest SKU that supports queues (Basic has no topics/sessions)
}

resource "azurerm_servicebus_queue" "payments" {
  name         = "payments"
  namespace_id = azurerm_servicebus_namespace.this.id
}

resource "azurerm_servicebus_queue" "shipping" {
  name         = "shipping"
  namespace_id = azurerm_servicebus_namespace.this.id
}

resource "azurerm_eventhub_namespace" "this" {
  name                = "${var.project}-eh"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  sku                 = "Standard" # Basic has no consumer groups beyond $Default, so no fan-out
  capacity            = 1
}

resource "azurerm_eventhub" "order_placed" {
  name              = "order-placed"
  namespace_id      = azurerm_eventhub_namespace.this.id
  partition_count   = 2
  message_retention = 1
}

# One consumer group per subscriber, so inventory and notifications each read the whole stream (fan-out).
resource "azurerm_eventhub_consumer_group" "inventory" {
  name                = "inventory"
  namespace_name      = azurerm_eventhub_namespace.this.name
  eventhub_name       = azurerm_eventhub.order_placed.name
  resource_group_name = data.azurerm_resource_group.this.name
}

resource "azurerm_eventhub_consumer_group" "notifications" {
  name                = "notifications"
  namespace_name      = azurerm_eventhub_namespace.this.name
  eventhub_name       = azurerm_eventhub.order_placed.name
  resource_group_name = data.azurerm_resource_group.this.name
}

resource "azurerm_eventgrid_topic" "this" {
  name                = "${var.project}-eg"
  resource_group_name = data.azurerm_resource_group.this.name
  location            = data.azurerm_resource_group.this.location
  # Benzene's Event Grid sender publishes CloudEvents 1.0 (EventGridPublisherClient.SendEventsAsync with
  # CloudEvent), so the topic must accept that schema - the default "EventGridSchema" would reject them.
  input_schema = "CloudEventSchemaV1_0"
}

# Route each integration event to the consuming Function's EventGrid-trigger function, filtered by the
# event type (= the Benzene topic). NOTE: the Azure-function endpoint must already exist, so these
# subscriptions are created only after the code has been published (see the deploy workflow's second
# terraform apply) - a first apply before publish will leave them to a later run.
resource "azurerm_eventgrid_event_subscription" "route" {
  for_each             = var.wire_eventgrid_subscriptions ? local.eventgrid_routes : {}
  name                 = replace(each.key, "_", "-")
  scope                = azurerm_eventgrid_topic.this.id
  included_event_types = [each.value.event_type]
  event_delivery_schema = "CloudEventSchemaV1_0"

  azure_function_endpoint {
    function_id = "${azurerm_linux_function_app.service[each.value.service].id}/functions/${each.value.function}"
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
      # The deployables are self-contained (they bring their own .NET runtime), so this pin only
      # has to be a version the plan's host supports - see var.dotnet_version.
      dotnet_version              = var.dotnet_version
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
