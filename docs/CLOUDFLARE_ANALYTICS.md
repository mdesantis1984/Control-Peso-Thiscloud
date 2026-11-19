# Cloudflare Web Analytics

Cloudflare Web Analytics gives traffic and Core Web Vitals metrics without cookies. In Control Peso it is enabled **from the Cloudflare dashboard only**: the application has no setting, token or script for it.

## How it works here

- `controlpeso.thiscloud.com.ar` is proxied by Cloudflare (orange cloud), so Cloudflare can inject the beacon script into HTML responses by itself ("automatic setup").
- The Content Security Policy already allows the beacon (`SecurityHeadersMiddleware`):
  - `script-src ... https://static.cloudflareinsights.com`
  - `connect-src ... https://cloudflareinsights.com`
- Nothing needs to be added to `App.razor`, `appsettings*.json` or the environment. There is no `CloudflareAnalytics` configuration key.

Google Analytics 4 is independent and optional; it is only enabled when `GoogleAnalytics:MeasurementId` has a real value ([SECURITY.md](SECURITY.md)).

## Enable it

1. Cloudflare dashboard → select the zone → **Analytics & Logs → Web Analytics**.
2. Add the site `controlpeso.thiscloud.com.ar` and choose **Enable** with automatic setup (available for proxied hostnames).
3. Wait a few minutes and open the site; the data appears in the Web Analytics view.

To verify, load any page and check that the HTML contains a `static.cloudflareinsights.com/beacon.min.js` script and that the browser console reports no CSP violation.

## Manual snippet (only if automatic setup is not used)

If the hostname is not proxied, Cloudflare gives a snippet with a site token. The token is not a secret, but it is environment specific: put it in configuration, not in a tracked file, and add the script to the `<head>` of `Components/App.razor`. The CSP above already allows it.

## Privacy

The beacon sets no cookies and does not use client-side storage. Mention it in the privacy policy if the policy is updated (`/privacy`).

## Troubleshooting

| Symptom | Check |
|---------|-------|
| No data after 10-15 minutes | Hostname is proxied (orange cloud) and the site is added in Web Analytics |
| CSP violation for `cloudflareinsights.com` | The deployed version must include the domains listed above in `script-src` and `connect-src` |
| Ad blockers hide visits | Expected: blockers can block the beacon; the numbers are a lower bound |
