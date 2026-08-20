# Website analytics & Search Console — setup guide

How to turn on traffic monitoring for [benzene.app](https://benzene.app).

**Status, 2026-08-20: GA4 is live; only Search Console is left.** `deploy-website.yml` now defaults
`GOOGLE_ANALYTICS_ID` to a real measurement id in the clear
(`${{ vars.GOOGLE_ANALYTICS_ID || 'G-WDSQNTXQSS' }}`), so Part 1 and its half of Part 3 are done and
every deploy ships the GA4 snippet — a repo variable now only *overrides* that default, it is no
longer what switches analytics on. `GOOGLE_SITE_VERIFICATION` has **no default**, so it stays unset
until someone completes Part 2 and sets the variable.

So the remaining work is **Part 2 only**, and it may turn out to be nothing: Search Console can
verify ownership through the Google Analytics property that is already live, in which case no token
and no variable are needed at all (see Part 2's note). The code side is done either way — see
"What's already wired", below — and no further code changes are needed.

You do all of this from a normal Google account — no billing, no card. Budget ~10 minutes.

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

## Part 1 — Google Analytics 4 (who's visiting, and what they do) — **DONE**

*Kept as a record of how the property was created, and for whoever has to recreate or move it. The
measurement id it produced is the one baked into `deploy-website.yml` as the default.*

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

## Part 2 — Google Search Console (how Google's search sees the site) — **OUTSTANDING**

*The only part still to do. Note the alternative at the end of this section: because Part 1 is now
live, Search Console can verify through the GA4 property, which skips the token and the variable
entirely.*

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

## Part 3 — Paste the IDs into the repo (the only place the code needs them)

In GitHub: **Settings → Secrets and variables → Actions → Variables tab → New repository variable**.

| Variable name | Value | From | State |
|---|---|---|---|
| `GOOGLE_ANALYTICS_ID` | `G-XXXXXXXXXX` | GA4 Measurement ID (Part 1) | **Not needed** — the workflow defaults to the live measurement id; set this only to point the site at a *different* property |
| `GOOGLE_SITE_VERIFICATION` | `AbC123...the_token...` | Search Console HTML-tag token (Part 2) | **Outstanding** — no default; unset means no verification tag is emitted |

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

## Cookie consent (already built in)

GA4 sets cookies, so to stay GDPR/PECR-friendly the site **asks first**: Google Analytics is not
requested and no cookie is set until the visitor clicks **Accept** on a consent banner. This is
prior (opt-in) consent — the compliant default for EU/UK visitors — and it's automatic whenever
`GOOGLE_ANALYTICS_ID` is set; there's nothing extra to turn on. Behaviour:

- On a first visit a small banner appears (bottom of the page) with **Accept** and **Reject**.
- **Accept** → GA loads and the choice is remembered (in `localStorage`) so the banner doesn't
  reappear on later pages/visits.
- **Reject** → nothing loads, no analytics cookie is set, and the choice is remembered. (It also
  flips GA's `ga-disable` flag, so a withdrawal after a previous Accept takes effect immediately.)
- A **"Cookie preferences"** link is added to the footer so a visitor can reopen the banner and
  change their mind at any time.
- With no consent stored, no GA is loaded — so crawlers and anyone who ignores the banner are never
  tracked.

If you'd rather avoid the banner entirely, a **cookieless analytics** provider (e.g. Plausible,
Fathom) needs no consent prompt; it's a bigger change but would reuse the same `SiteOptions` hook to
carry its snippet instead. Say the word if you want to go that route.

---

## Quick reference

- **Build flags**: `--google-analytics-id G-XXXXXXXXXX`, `--google-site-verification <token>`
  (both optional; empty → nothing emitted).
- **Repo variables**: `GOOGLE_ANALYTICS_ID` (already defaulted in `deploy-website.yml` — an
  override, not a switch), `GOOGLE_SITE_VERIFICATION` (no default; the one still outstanding).
- **Where it's injected**: `website/generator/Layout.cs` → `GoogleHead()`, called from `SeoHead()`,
  so it's on every page. Options model: `website/generator/SiteOptions.cs`.
- **Sitemap to submit**: `https://benzene.app/sitemap.xml`.
- **Dashboards**: GA <https://analytics.google.com> · Search Console
  <https://search.google.com/search-console>.
