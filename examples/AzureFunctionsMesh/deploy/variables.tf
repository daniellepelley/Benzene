variable "location" {
  description = "Azure region to deploy into."
  type        = string
  default     = "westeurope"
}

variable "dotnet_version" {
  description = <<-EOT
    The .NET version pinned on the Functions host stack (linuxFxVersion). The apps are published
    SELF-CONTAINED, carrying their own .NET runtime, so this does NOT need to match the version the
    apps target - it only needs to be a version the plan's host supports, so the host can start the
    isolated worker process. Classic Linux Consumption (Y1) does not offer the newest .NET versions
    (those land on Flex Consumption), which is why this defaults to the 8.0 LTS rather than 10.0.
  EOT
  type        = string
  default     = "8.0"
}

variable "project" {
  description = "Name prefix for all resources (must yield globally-unique Function App names)."
  type        = string
  default     = "benzene-fnmesh"
}

variable "resource_group" {
  description = "Resource group name."
  type        = string
  default     = "benzene-fnmesh-rg"
}

variable "storage_account" {
  description = "Storage account for the Functions runtime + the mesh catalog artifacts (globally unique, lowercase alphanumeric)."
  type        = string
}

variable "discovery_tag_key" {
  description = "The resource tag key discovery filters on. Services carry it; the mesh does not."
  type        = string
  default     = "benzene"
}

variable "wire_eventgrid_subscriptions" {
  description = <<-EOT
    Whether to create the Event Grid -> Function subscriptions. They point at each consumer's Functions
    Event Grid extension webhook, which is validated against the live running function, so the target
    must be published AND warm. The deploy therefore does one apply with this false (everything except
    the subscriptions), publishes the code, warms the consumer apps, then a second apply with this true.
  EOT
  type        = bool
  default     = false
}

variable "usage_window_hours" {
  description = "Lookback window (hours) the mesh's Application Insights usage source counts topic requests over, and the window the Mesh UI shows. Coarse usage only — fine-grained analysis belongs in App Insights/Grafana."
  type        = number
  default     = 24
}
