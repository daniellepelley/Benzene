output "nameservers" {
  description = "Set these as benzene.app's nameservers at GoDaddy (one-time). DNS only resolves once this is done."
  value       = aws_route53_zone.this.name_servers
}

output "live_bucket" {
  description = "Production origin bucket — set as the repo variable WEBSITE_LIVE_BUCKET."
  value       = aws_s3_bucket.live.bucket
}

output "dev_bucket" {
  description = "Dev origin bucket — set as the repo variable WEBSITE_DEV_BUCKET (the auto-deploy target)."
  value       = aws_s3_bucket.dev.bucket
}

output "live_distribution_id" {
  description = "Production CloudFront distribution — set as the repo variable WEBSITE_LIVE_DISTRIBUTION_ID (invalidated on promote)."
  value       = aws_cloudfront_distribution.live.id
}

output "dev_distribution_id" {
  description = "Dev CloudFront distribution — set as the repo variable WEBSITE_DEV_DISTRIBUTION_ID (invalidated on each dev deploy)."
  value       = aws_cloudfront_distribution.dev.id
}

output "live_url" {
  value = "https://${var.domain}"
}

output "dev_url" {
  value = "https://dev.${var.domain}"
}
