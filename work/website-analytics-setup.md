# Website analytics & Search Console — setup guide

How to turn on traffic monitoring for [benzene.app](https://benzene.app). The **code side is done**
(see "What's already wired", below); what's left is the **manual account setup** in Google, then
pasting two identifiers into the repo's CI variables. No further code changes are needed.

You do all of this from a normal Google account — no billing, no card. Budget ~20 minutes.

---

## What's already wired (no action needed)

Two things already ship on every page, so analytics is the third leg, not a from-scratch effort:

1. **Search-engine discoverability** (done earlier — the "tags for search engines" work). Every page
   carries a self-referencing `<link rel="canonical">`, Open Graph + Twitter Card tags (so a shared
   link unfurls with a title, blurb, and image), and a `<meta name="description">`. The build also
   emits `sitemap.xml` (every page) and `robots.txt` (which points crawlers at the sitemap). This is
   what helps Google *find and index* the site; analytics and Search Console are what let you *see*
   the resulting traffic.
2. **Analytics hooks.** The generator now accepts `--google-analytics-id` and
   `--google-site-verification`. When set, every page's `<head>` gets the GA4 snippet and the
   Search Console ownership tag. When unset (the default), nothing is injected — so the site stays
   tracking-free until you deliberately turn it on. The deploy workflow passes these through from two
   repo **variables**, so turning analytics on is just setting those variables.

The only reason it isn't already live: the two IDs come from *your* Google account, which only you
can create. That's the manual part below.

---

## Part 1 — Google Analytics 4 (who's visiting, and what they do)

GA4 is the "engagement" product: sessions, page views, which pages, referrers, countries, devices,
real-time visitors.

1. Go to <https://analytics.google.com> and sign in.
2. **Admin** (gear, bottom-left) → **Create** → **Account**. Name it e.g. `Benzene`. Accept the data
   terms.
3. Create a **Property**: name `benzene.app`, set your reporting time zone and currency → **Next**,
   fill the business details → **Create**.
4. Under **Data collection → Data streams**, add a **Web** stream:
   - **Website URL**: `https://benzene.app`
   - **Stream name**: `benzene.app`
   - Leave *Enhanced measurement* on (it auto-tracks scrolls, outbound clicks, file downloads).
5. The stream page shows a **Measurement ID** like **`G-XXXXXXXXXX`**. Copy it — that's the whole
   handoff to the site.

**Then set the repo variable** (see Part 3). Data starts flowing to GA within a few minutes of the
next deploy; the **Realtime** report is the quickest confirmation it's working.

> One property covers both `dev.benzene.app` and `benzene.app`, because promotion ships the same
> bytes to both. That's fine — if you want to exclude dev traffic from reports, in GA add a filter
> on **hostname** (`Admin → Data settings → Data filters`, or a comparison on `Hostname`).

---

## Part 2 — Google Search Console (how Google's search sees the site)

Search Console is the old "Google Webmaster Tools" — it's the "how do I rank" side: which queries
show the site, click-through rates, indexing/coverage problems, and mobile issues. Complementary to
GA4, not a replacement.

1. Go to <https://search.google.com/search-console> and sign in.
2. **Add property**. You have two choices:
   - **Domain** property (`benzene.app`, covers every subdomain + scheme) — the better choice, but it
     requires a **DNS TXT record**. Since the DNS is Route 53 (managed in `website/deploy/`), this
     means adding a record there. Recommended if you're comfortable with that.
   - **URL-prefix** property (`https://benzene.app`) — verifiable with the **HTML tag** method, which
     is the one the site already supports with zero DNS work. Easiest path:
3. For the **URL-prefix** path, pick the **HTML tag** verification method. Google shows a tag like:
   ```html
   <meta name="google-site-verification" content="AbC123...the_token..." />
   ```
   Copy **only the `content` token** (`AbC123...the_token...`), not the whole tag — the generator
   builds the `<meta>` around it.
4. **Set the repo variable** (Part 3), let the site redeploy, then click **Verify** in Search Console.
5. Once verified, **submit the sitemap**: Search Console → **Sitemaps** → enter `sitemap.xml` →
   **Submit**. (The site already publishes `https://benzene.app/sitemap.xml`.)

> Alternative: if you set up GA4 first (Part 1), Search Console offers a **"Google Analytics"**
> verification option that reuses it — no token needed. If you go that route you can skip the
> `GOOGLE_SITE_VERIFICATION` variable entirely.

---

## Part 3 — Paste the two IDs into the repo (the only place the code needs them)

In GitHub: **Settings → Secrets and variables → Actions → Variables tab → New repository variable**.
Add whichever you have:

| Variable name | Value | From |
|---|---|---|
| `GOOGLE_ANALYTICS_ID` | `G-XXXXXXXXXX` | GA4 Measurement ID (Part 1) |
| `GOOGLE_SITE_VERIFICATION` | `AbC123...the_token...` | Search Console HTML-tag token (Part 2) |

These are **variables, not secrets** — they're public values that appear in the page source anyway,
so they don't need to be hidden, and putting them in *Variables* keeps them readable/editable.

**Then trigger a deploy** so the values get baked in:

- Push any change to the site (`docs/**`, `README.md`, `website/**`), **or**
- Run the **Deploy Website (dev)** workflow manually (Actions tab → *Deploy Website (dev)* → *Run
  workflow*). This publishes to `dev.benzene.app`.
- To push it to the live site, run **Promote Website (dev → live)** (type `promote` to confirm).

Verify it worked: open the site, **View Source**, and search for `googletagmanager` (GA) and
`google-site-verification` (Search Console). Both should be in the `<head>`.

---

## One thing to decide: cookie consent

GA4 sets cookies and processes visitor data, so depending on your audience (EU/UK visitors in
particular) you may need a **cookie-consent banner** before analytics loads, to stay GDPR/PECR-
compliant. The current setup loads GA on every page unconditionally. Options, roughly in order of
effort:

- **Accept the risk for now** — it's a developer docs site with no personal data collected beyond
  GA's defaults; many small OSS sites run GA plainly. Lowest effort.
- **Turn on Google Consent Mode / a lightweight consent banner** — a follow-up task; the generator
  would gate the `gtag` snippet behind consent. Say the word and I'll implement it.
- **Use a cookieless alternative** (e.g. Plausible, Fathom) — privacy-friendly, no banner needed,
  but paid/self-hosted. A bigger change; the same `SiteOptions` hook would carry its snippet instead.

---

## Quick reference

- **Build flags**: `--google-analytics-id G-XXXXXXXXXX`, `--google-site-verification <token>`
  (both optional; empty → nothing emitted).
- **Repo variables**: `GOOGLE_ANALYTICS_ID`, `GOOGLE_SITE_VERIFICATION`.
- **Where it's injected**: `website/generator/Layout.cs` → `GoogleHead()`, called from `SeoHead()`,
  so it's on every page. Options model: `website/generator/SiteOptions.cs`.
- **Sitemap to submit**: `https://benzene.app/sitemap.xml`.
- **Dashboards**: GA <https://analytics.google.com> · Search Console
  <https://search.google.com/search-console>.
