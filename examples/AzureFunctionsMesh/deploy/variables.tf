variable "location" {
  description = "Azure region to deploy into."
  type        = string
  default     = "westeurope"
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
