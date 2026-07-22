terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Buckets/CloudFront/Route53. CloudFront + Route53 are global; the bucket region is cosmetic behind
# a CDN, so var.region only affects where the two origin buckets physically live.
provider "aws" {
  region = var.region
}

# CloudFront requires its ACM certificate in us-east-1, regardless of where anything else lives.
provider "aws" {
  alias  = "us_east_1"
  region = "us-east-1"
}

locals {
  live_domains = [var.domain, "www.${var.domain}"] # apex + www on the prod distribution
  dev_domain   = "dev.${var.domain}"
  tags         = { Project = "benzene-website", ManagedBy = "terraform" }
}

# ---------------------------------------------------------------------------------------------------
# DNS: a Route 53 hosted zone. After the first apply, set benzene.app's nameservers at GoDaddy to the
# `nameservers` output (see README). Everything else here is created inside this zone.
# ---------------------------------------------------------------------------------------------------
resource "aws_route53_zone" "this" {
  name = var.domain
  tags = local.tags
}

# ---------------------------------------------------------------------------------------------------
# TLS: one DNS-validated certificate covering the apex, www, and dev — in us-east-1 for CloudFront.
# ---------------------------------------------------------------------------------------------------
resource "aws_acm_certificate" "this" {
  provider                  = aws.us_east_1
  domain_name               = var.domain
  subject_alternative_names = ["www.${var.domain}", local.dev_domain]
  validation_method         = "DNS"
  tags                      = local.tags

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_route53_record" "cert_validation" {
  for_each = {
    for dvo in aws_acm_certificate.this.domain_validation_options : dvo.domain_name => {
      name   = dvo.resource_record_name
      type   = dvo.resource_record_type
      record = dvo.resource_record_value
    }
  }

  zone_id         = aws_route53_zone.this.zone_id
  name            = each.value.name
  type            = each.value.type
  records         = [each.value.record]
  ttl             = 60
  allow_overwrite = true
}

resource "aws_acm_certificate_validation" "this" {
  provider                = aws.us_east_1
  certificate_arn         = aws_acm_certificate.this.arn
  validation_record_fqdns = [for r in aws_route53_record.cert_validation : r.fqdn]
}

# ---------------------------------------------------------------------------------------------------
# Origins: two private buckets (live + dev). No public access, no S3 website hosting — CloudFront
# reaches them through Origin Access Control (OAC). ".app" forces HTTPS, which S3 website endpoints
# can't do, so this CloudFront-fronted, private-bucket shape is mandatory, not just preferred.
# ---------------------------------------------------------------------------------------------------
resource "aws_s3_bucket" "live" {
  bucket = var.live_bucket_name
  tags   = local.tags
}

resource "aws_s3_bucket" "dev" {
  bucket = var.dev_bucket_name
  tags   = local.tags
}

resource "aws_s3_bucket_public_access_block" "live" {
  bucket                  = aws_s3_bucket.live.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_public_access_block" "dev" {
  bucket                  = aws_s3_bucket.dev.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_cloudfront_origin_access_control" "s3" {
  name                              = "benzene-website-oac"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# ---------------------------------------------------------------------------------------------------
# The shared viewer-request function: www -> apex 301 + directory-index rewrite (see site-router.js).
# ---------------------------------------------------------------------------------------------------
resource "aws_cloudfront_function" "router" {
  name    = "benzene-website-router"
  runtime = "cloudfront-js-2.0"
  comment = "www->apex redirect + directory index.html rewrite"
  publish = true
  code    = file("${path.module}/site-router.js")
}

# AWS-managed cache policies (stable well-known IDs).
data "aws_cloudfront_cache_policy" "optimized" {
  name = "Managed-CachingOptimized"
}

data "aws_cloudfront_cache_policy" "disabled" {
  name = "Managed-CachingDisabled"
}

# ---------------------------------------------------------------------------------------------------
# Production distribution: benzene.app + www.benzene.app -> live bucket. Caches aggressively; the
# deploy/promote workflows invalidate it.
# ---------------------------------------------------------------------------------------------------
resource "aws_cloudfront_distribution" "live" {
  enabled             = true
  is_ipv6_enabled     = true
  comment             = "benzene.app (production)"
  default_root_object = "index.html"
  aliases             = local.live_domains
  price_class         = var.price_class
  tags                = local.tags

  origin {
    origin_id                = "live-s3"
    domain_name              = aws_s3_bucket.live.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.s3.id
  }

  default_cache_behavior {
    target_origin_id       = "live-s3"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true
    cache_policy_id        = data.aws_cloudfront_cache_policy.optimized.id

    function_association {
      event_type   = "viewer-request"
      function_arn = aws_cloudfront_function.router.arn
    }
  }

  # A missing object behind OAC returns 403 from S3; surface it as a clean 404.
  custom_error_response {
    error_code            = 403
    response_code         = 404
    response_page_path    = "/404.html"
    error_caching_min_ttl = 60
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate_validation.this.certificate_arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }
}

# ---------------------------------------------------------------------------------------------------
# Dev distribution: dev.benzene.app -> dev bucket. Caching disabled so trial changes show at once.
# ---------------------------------------------------------------------------------------------------
resource "aws_cloudfront_distribution" "dev" {
  enabled             = true
  is_ipv6_enabled     = true
  comment             = "dev.benzene.app (staging)"
  default_root_object = "index.html"
  aliases             = [local.dev_domain]
  price_class         = var.price_class
  tags                = local.tags

  origin {
    origin_id                = "dev-s3"
    domain_name              = aws_s3_bucket.dev.bucket_regional_domain_name
    origin_access_control_id = aws_cloudfront_origin_access_control.s3.id
  }

  default_cache_behavior {
    target_origin_id       = "dev-s3"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true
    cache_policy_id        = data.aws_cloudfront_cache_policy.disabled.id

    function_association {
      event_type   = "viewer-request"
      function_arn = aws_cloudfront_function.router.arn
    }
  }

  custom_error_response {
    error_code            = 403
    response_code         = 404
    response_page_path    = "/404.html"
    error_caching_min_ttl = 0
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate_validation.this.certificate_arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }
}

# ---------------------------------------------------------------------------------------------------
# Bucket policies: allow each distribution (only) to read its bucket via OAC.
# ---------------------------------------------------------------------------------------------------
data "aws_iam_policy_document" "live" {
  statement {
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.live.arn}/*"]

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.live.arn]
    }
  }
}

data "aws_iam_policy_document" "dev" {
  statement {
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.dev.arn}/*"]

    principals {
      type        = "Service"
      identifiers = ["cloudfront.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "AWS:SourceArn"
      values   = [aws_cloudfront_distribution.dev.arn]
    }
  }
}

resource "aws_s3_bucket_policy" "live" {
  bucket = aws_s3_bucket.live.id
  policy = data.aws_iam_policy_document.live.json
}

resource "aws_s3_bucket_policy" "dev" {
  bucket = aws_s3_bucket.dev.id
  policy = data.aws_iam_policy_document.dev.json
}

# ---------------------------------------------------------------------------------------------------
# DNS records: apex + www -> prod distribution, dev -> dev distribution (A + AAAA aliases).
# ---------------------------------------------------------------------------------------------------
resource "aws_route53_record" "apex_a" {
  zone_id = aws_route53_zone.this.zone_id
  name    = var.domain
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.live.domain_name
    zone_id                = aws_cloudfront_distribution.live.hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "apex_aaaa" {
  zone_id = aws_route53_zone.this.zone_id
  name    = var.domain
  type    = "AAAA"

  alias {
    name                   = aws_cloudfront_distribution.live.domain_name
    zone_id                = aws_cloudfront_distribution.live.hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "www_a" {
  zone_id = aws_route53_zone.this.zone_id
  name    = "www.${var.domain}"
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.live.domain_name
    zone_id                = aws_cloudfront_distribution.live.hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "www_aaaa" {
  zone_id = aws_route53_zone.this.zone_id
  name    = "www.${var.domain}"
  type    = "AAAA"

  alias {
    name                   = aws_cloudfront_distribution.live.domain_name
    zone_id                = aws_cloudfront_distribution.live.hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "dev_a" {
  zone_id = aws_route53_zone.this.zone_id
  name    = local.dev_domain
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.dev.domain_name
    zone_id                = aws_cloudfront_distribution.dev.hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "dev_aaaa" {
  zone_id = aws_route53_zone.this.zone_id
  name    = local.dev_domain
  type    = "AAAA"

  alias {
    name                   = aws_cloudfront_distribution.dev.domain_name
    zone_id                = aws_cloudfront_distribution.dev.hosted_zone_id
    evaluate_target_health = false
  }
}
