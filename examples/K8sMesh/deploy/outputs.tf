output "cluster_name" {
  description = "The EKS cluster name (also the value for `aws eks update-kubeconfig --name ...`)."
  value       = aws_eks_cluster.this.name
}

output "kubeconfig_command" {
  description = "Run this to point kubectl at the cluster."
  value       = "aws eks update-kubeconfig --region ${var.region} --name ${aws_eks_cluster.this.name}"
}

output "service_image_repository" {
  description = "ECR repository for the Cloud Service image (push before applying the manifests)."
  value       = aws_ecr_repository.service.repository_url
}

output "mesh_image_repository" {
  description = "ECR repository for the mesh image (push before applying the manifests)."
  value       = aws_ecr_repository.mesh.repository_url
}

# The internet-facing URL is minted by Kubernetes, not Terraform — the mesh Service becomes a
# LoadBalancer under the deploy/eks overlay, and its ELB hostname appears on the Service status:
#   kubectl -n benzene-mesh get svc mesh -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'
# The deploy workflow polls for it and prints http://<hostname>/mesh-ui at the end of the run.
