variable "project" {
  type        = string
  description = "The Google Cloud project id."
}

variable "region" {
  type        = string
  default     = "europe-west2"
  description = "The region functions, topics, and the bucket live in."
}

variable "name_prefix" {
  type        = string
  default     = "benzene-gcpmesh"
  description = "Prefix for all resource names."
}

variable "mesh_refresh_url" {
  type        = string
  default     = ""
  description = "The mesh function's POST /mesh/refresh URL. Empty on the first apply (before the mesh function exists); set on the second apply to create the Cloud Scheduler job."
}
