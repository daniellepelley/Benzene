# benzene.app hosting (Terraform)

Infrastructure for the [Benzene website](../README.md) on AWS:

| URL | Serves | Origin |
|---|---|---|
| `https://benzene.app` | production site | `benzene-app-live` bucket, via the prod CloudFront distribution |
| `https://www.benzene.app` | 301 → `https://benzene.app` | (CloudFront function on the prod distribution) |
| `https://dev.benzene.app` | staging / trial site | `benzene-app-dev` bucket, via the dev CloudFront distribution |

**Why CloudFront and not plain S3 website hosting?** `.app` is on the HSTS preload list, so browsers
refuse plain HTTP. S3 website endpoints are HTTP-only, so each bucket is private and fronted by
CloudFront with an ACM certificate for HTTPS. Buckets are never public; CloudFront reaches them via
Origin Access Control.

Deploy flow: **push to `main` → dev** (`.github/workflows/deploy-website.yml`), then **manual promote
→ live** (`.github/workflows/promote-website.yml`, an S3 `dev → live` copy + prod invalidation).

## One-time setup

Run these from `website/deploy/` with AWS credentials that can create S3/CloudFront/ACM/Route 53.

### 1. Create the hosted zone first, to learn the nameservers

The ACM certificate is DNS-validated in the new Route 53 zone, but those records only resolve once
GoDaddy points at Route 53 — a chicken-and-egg. So apply the zone alone first:

```bash
terraform init
terraform apply -target=aws_route53_zone.this
terraform output nameservers
```

### 2. Point GoDaddy at Route 53 (one-time)

In GoDaddy → **benzene.app → DNS → Nameservers → Change → "I'll use my own nameservers"**, enter the
four `nameservers` from the output. **If GoDaddy shows DNSSEC as enabled, turn it off first** — moving
nameservers with stale DNSSEC keys breaks resolution. Propagation is usually minutes, up to a few hours.

### 3. Apply the rest

Once the nameservers have switched (check with `dig NS benzene.app`), apply everything. The ACM
validation step will now complete:

```bash
terraform apply
terraform output
```

### 4. Wire the GitHub Actions variables

Set these as **repository → Settings → Secrets and variables → Actions → Variables** (the workflows
already read `vars.AWS_ACCESS_KEY_ID` + `secrets.AWS_SECRET_ACCESS_KEY` and `vars.WEBSITE_AWS_REGION`
from the existing setup — keep those):

| Variable | Value (from `terraform output`) |
|---|---|
| `WEBSITE_DEV_BUCKET` | `dev_bucket` |
| `WEBSITE_LIVE_BUCKET` | `live_bucket` |
| `WEBSITE_DEV_DISTRIBUTION_ID` | `dev_distribution_id` |
| `WEBSITE_LIVE_DISTRIBUTION_ID` | `live_distribution_id` |

The old single `WEBSITE_S3_BUCKET` variable is no longer used and can be removed.

### 5. First deploy, then promote

- Push to `main` (or run **Actions → Deploy Website (dev) → Run workflow**) → publishes to
  `https://dev.benzene.app`.
- Review it, then run **Actions → Promote Website (dev → live)**, typing `promote` to confirm →
  copies dev to `https://benzene.app`.

## CI credentials — required IAM permissions

The static CI credentials need to sync both buckets and invalidate both distributions. Attach a
policy like this to that IAM user (tighten the CloudFront resource ARNs to the two distributions if
you prefer):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": ["arn:aws:s3:::benzene-app-dev", "arn:aws:s3:::benzene-app-live"]
    },
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": ["arn:aws:s3:::benzene-app-dev/*", "arn:aws:s3:::benzene-app-live/*"]
    },
    {
      "Effect": "Allow",
      "Action": ["cloudfront:CreateInvalidation"],
      "Resource": "*"
    }
  ]
}
```

## Notes

- **State.** This stack uses local Terraform state by default. For a shared/CI-managed setup, add a
  remote backend (e.g. an S3 backend) like the other `deploy/` stacks in this repo.
- **404s until first deploy.** The buckets start empty, so the site returns 404 until step 5 runs.
- **Cost.** Route 53 hosted zone (~$0.50/mo) + CloudFront/S3 usage (pennies at docs-site traffic).
  `price_class` defaults to `PriceClass_100` (NA+EU) to keep it cheap; set `PriceClass_All` for global
  edge coverage.
- **A shared 404 page.** Both distributions map an origin 403 (a missing object behind OAC) to a
  `/404.html`; add a `404.html` to the generated site if you want a branded not-found page.
