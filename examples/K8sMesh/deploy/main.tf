# EKS deployment for examples/K8sMesh — the AWS leg of the Kubernetes mesh example, mirroring
# examples/AwsMesh/deploy's conventions (S3 remote state, project-prefixed names, deployed by
# .github/workflows/deploy-eks-mesh-example.yml).
#
# Terraform owns the AWS infrastructure only: two ECR repositories and an EKS cluster with one
# managed node group, on the account's default VPC. The Kubernetes objects themselves are the
# UNCHANGED examples/K8sMesh/k8s manifests, applied by the workflow via the deploy/eks kustomize
# overlay (ECR images + a LoadBalancer Service for the mesh UI) — the same dogfooding as the kind
# path, just on real AWS. The internet-facing load balancer is created by Kubernetes (the mesh
# Service becoming type LoadBalancer), not by Terraform, which is why teardown deletes the
# namespace before `terraform destroy` (see the workflow's destroy path).

terraform {
  required_version = ">= 1.5.0"
  # Remote state in S3 so the state survives between (ephemeral) CI runs — same bucket the AwsMesh
  # example uses, different key. Configured at `terraform init` time via -backend-config.
  backend "s3" {}
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region
}

data "aws_caller_identity" "current" {}

# The account's default VPC keeps the example free of a hand-rolled network: its public subnets span
# every AZ, which satisfies EKS's two-AZ minimum, and public IPs on the nodes avoid NAT gateways.
# (A region whose default VPC has been deleted needs var.subnet_ids set explicitly.)
data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

locals {
  subnet_ids = length(var.subnet_ids) > 0 ? var.subnet_ids : data.aws_subnets.default.ids
}

# ---------------------------------------------------------------------------------------------------
# ECR: one repository per image (the single service image deployed three times, and the mesh).
# ---------------------------------------------------------------------------------------------------
resource "aws_ecr_repository" "service" {
  name         = "${var.project}-service"
  force_delete = true
}

resource "aws_ecr_repository" "mesh" {
  name         = "${var.project}-mesh"
  force_delete = true
}

# ---------------------------------------------------------------------------------------------------
# IAM: the standard EKS control-plane and worker-node roles.
# ---------------------------------------------------------------------------------------------------
data "aws_iam_policy_document" "cluster_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["eks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "cluster" {
  name               = "${var.project}-cluster-role"
  assume_role_policy = data.aws_iam_policy_document.cluster_assume.json
}

resource "aws_iam_role_policy_attachment" "cluster" {
  role       = aws_iam_role.cluster.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
}

data "aws_iam_policy_document" "node_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ec2.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "node" {
  name               = "${var.project}-node-role"
  assume_role_policy = data.aws_iam_policy_document.node_assume.json
}

resource "aws_iam_role_policy_attachment" "node_worker" {
  role       = aws_iam_role.node.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy"
}

resource "aws_iam_role_policy_attachment" "node_cni" {
  role       = aws_iam_role.node.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy"
}

# Nodes pull the two images above straight from ECR — no imagePullSecrets anywhere.
resource "aws_iam_role_policy_attachment" "node_ecr" {
  role       = aws_iam_role.node.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
}

# ---------------------------------------------------------------------------------------------------
# The cluster + one small managed node group.
# ---------------------------------------------------------------------------------------------------
resource "aws_eks_cluster" "this" {
  name     = var.project
  role_arn = aws_iam_role.cluster.arn

  vpc_config {
    subnet_ids = local.subnet_ids
  }

  # API access entries with the creating principal bootstrapped as cluster-admin, so the same CI
  # credentials that ran `terraform apply` can immediately `kubectl apply` — no aws-auth ConfigMap
  # editing step.
  access_config {
    authentication_mode                         = "API_AND_CONFIG_MAP"
    bootstrap_cluster_creator_admin_permissions = true
  }

  depends_on = [aws_iam_role_policy_attachment.cluster]
}

resource "aws_eks_node_group" "this" {
  cluster_name    = aws_eks_cluster.this.name
  node_group_name = "${var.project}-nodes"
  node_role_arn   = aws_iam_role.node.arn
  subnet_ids      = local.subnet_ids
  instance_types  = var.node_instance_types

  scaling_config {
    desired_size = var.node_desired_size
    min_size     = 1
    max_size     = var.node_desired_size + 1
  }

  depends_on = [
    aws_iam_role_policy_attachment.node_worker,
    aws_iam_role_policy_attachment.node_cni,
    aws_iam_role_policy_attachment.node_ecr,
  ]
}
