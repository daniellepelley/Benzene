variable "region" {
  description = "AWS region to deploy into."
  type        = string
  default     = "eu-west-1"
}

variable "project" {
  description = "Name prefix for all resources, and the EKS cluster name."
  type        = string
  default     = "benzene-k8smesh"
}

variable "subnet_ids" {
  description = <<-EOT
    Subnets for the cluster and node group (at least two AZs). Empty (the default) uses every subnet
    of the account's default VPC — the zero-setup path. Set explicitly if the region's default VPC
    has been deleted, or to place the cluster in your own network.
  EOT
  type        = list(string)
  default     = []
}

variable "node_instance_types" {
  description = "Instance types for the managed node group. The four example pods are tiny; small burstable instances are plenty."
  type        = list(string)
  default     = ["t3.small"]
}

variable "node_desired_size" {
  description = "Desired node count. Two spreads the pods across AZs; one also works for a pure demo."
  type        = number
  default     = 2
}
