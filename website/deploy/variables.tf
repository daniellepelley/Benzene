variable "domain" {
  description = "The apex domain."
  type        = string
  default     = "benzene.app"
}

variable "region" {
  description = "Region for the two origin S3 buckets (cosmetic behind CloudFront; the ACM cert is always us-east-1)."
  type        = string
  default     = "eu-west-2"
}

variable "live_bucket_name" {
  description = "Globally-unique name for the production origin bucket."
  type        = string
  default     = "benzene-app-live"
}

variable "dev_bucket_name" {
  description = "Globally-unique name for the dev/staging origin bucket."
  type        = string
  default     = "benzene-app-dev"
}

variable "price_class" {
  description = "CloudFront price class. PriceClass_100 (NA+EU) is the cheapest and fine for a docs site; use PriceClass_All for global edge coverage."
  type        = string
  default     = "PriceClass_100"
}
