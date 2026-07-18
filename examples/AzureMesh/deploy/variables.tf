variable "location" {
  description = "Azure region to deploy into."
  type        = string
  default     = "westeurope"
}

variable "project" {
  description = "Name prefix for all resources (must yield globally-unique web app names)."
  type        = string
  default     = "benzene-mesh"
}

variable "resource_group" {
  description = "Resource group name."
  type        = string
  default     = "benzene-mesh-rg"
}

variable "acr_name" {
  description = "Azure Container Registry name (globally unique, alphanumeric)."
  type        = string
}

variable "storage_account" {
  description = "Storage account for the mesh catalog artifacts (globally unique, lowercase alphanumeric)."
  type        = string
}

variable "discovery_tag_key" {
  description = "The resource tag key discovery filters on. Services carry it; the mesh does not."
  type        = string
  default     = "benzene"
}

variable "service_image" {
  description = "The Cloud Service image repository in the ACR (without tag)."
  type        = string
  default     = "benzene-azuremesh-service"
}

variable "mesh_image" {
  description = "The mesh image repository in the ACR (without tag)."
  type        = string
  default     = "benzene-azuremesh-mesh"
}

variable "image_tag" {
  description = "The image tag CI pushed (usually the commit SHA)."
  type        = string
  default     = "latest"
}
