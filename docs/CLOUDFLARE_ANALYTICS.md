# Cloudflare Analytics - Setup Guide

## Overview

Cloudflare Web Analytics is a free, privacy-first analytics solution that provides insights into website traffic without cookies or tracking personal data.

## Features

- **Privacy-First**: No cookies, no tracking, GDPR/CCPA compliant
- **Real-time Analytics**: Monitor visitors, page views, and performance
- **Core Web Vitals**: Track LCP, FID, CLS metrics
- **Zero Performance Impact**: Async beacon load
- **Free Tier**: No cost for basic analytics

## Setup Steps

### 1. Create Cloudflare Account

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/sign-up)
2. Sign up with email/password or SSO
3. Verify email address

### 2. Add Site to Cloudflare

**Option A: Full DNS Management (Recommended)**

1. Click "Add a Site" in dashboard
2. Enter domain: `controlpeso.thiscloud.com.ar`
3. Select Free Plan
4. Cloudflare will scan existing DNS records
5. Review and confirm DNS records
6. Update nameservers at domain registrar:
   - Remove current nameservers
   - Add Cloudflare nameservers (provided in dashboard)
   - Example: `ns1.cloudflare.com`, `ns2.cloudflare.com`
7. Wait for DNS propagation (5 minutes - 48 hours)
8. Cloudflare will verify and activate site

**Option B: CNAME Setup (Partial)**

1. If you can't change nameservers, use CNAME setup
2. Add CNAME record in your registrar:
   - Type: `CNAME`
   - Name: `www` or `@`
   - Value: (provided by Cloudflare)
3. Add page rule in Cloudflare for HTTPS redirect

### 3. Enable Web Analytics

1. In Cloudflare Dashboard, select your site
2. Navigate to **Analytics** → **Web Analytics**
3. Click "Enable Web Analytics"
4. Copy the beacon script snippet (will look like):
   ```html
   <script defer src='https://static.cloudflareinsights.com/beacon.min.js' 
           data-cf-beacon='{"token": "YOUR_TOKEN_HERE"}'></script>
   ```

### 4. Integration Options

**Option A: Direct Script (Current Setup)**

The beacon script is already whitelisted in our Content Security Policy:

```csharp
// SecurityHeadersMiddleware.cs
script-src 'self' ... https://static.cloudflareinsights.com
connect-src 'self' ... https://cloudflareinsights.com
```

To integrate:

1. Copy the beacon script from Cloudflare dashboard
2. Add to `App.razor` in `<head>` section:
   ```razor
   @* Cloudflare Web Analytics *@
   <script defer src='https://static.cloudflareinsights.com/beacon.min.js' 
           data-cf-beacon='{"token": "YOUR_TOKEN_HERE"}'></script>
   ```
3. Replace `YOUR_TOKEN_HERE` with actual token from Cloudflare

**Option B: appsettings.json Configuration (Recommended)**

For better secret management:

1. Add to `appsettings.json`:
   ```json
   "CloudflareAnalytics": {
     "Token": ""
   }
   ```
2. Store actual token in User Secrets (Development) or Environment Variables (Production)
3. Inject token in `App.razor.cs`:
   ```csharp
   private string cloudflareToken = string.Empty;
   
   protected override void OnInitialized()
   {
       cloudflareToken = Configuration["CloudflareAnalytics:Token"] ?? string.Empty;
   }
   ```
4. Reference in `App.razor`:
   ```razor
   <script defer src='https://static.cloudflareinsights.com/beacon.min.js' 
           data-cf-beacon='{"token": "@cloudflareToken"}'></script>
   ```

### 5. DNS Configuration (Full Setup)

Once nameservers are updated, configure DNS records in Cloudflare:

**Required Records:**

| Type  | Name | Value                          | Proxy Status | TTL  |
|-------|------|--------------------------------|--------------|------|
| A     | @    | `YOUR_SERVER_IP`               | Proxied (🟠) | Auto |
| CNAME | www  | `controlpeso.thiscloud.com.ar` | Proxied (🟠) | Auto |

**Optional (Email, Subdomains):**

| Type  | Name | Value              | Proxy Status     | TTL  |
|-------|------|--------------------|------------------|------|
| MX    | @    | `mail.example.com` | DNS Only (Gray)  | Auto |
| TXT   | @    | `v=spf1...`        | DNS Only (Gray)  | Auto |

**Proxied vs DNS Only:**

- **Proxied (Orange Cloud)**: Traffic passes through Cloudflare (HTTPS, caching, analytics, DDoS protection)
- **DNS Only (Gray Cloud)**: Direct connection to origin server (no Cloudflare features)

### 6. Enable HTTPS

1. In Cloudflare Dashboard → **SSL/TLS**
2. Set encryption mode to **Full (Strict)**:
   - Requires valid SSL certificate on origin server
   - Use Let's Encrypt or Cloudflare Origin Certificate
3. Enable **Always Use HTTPS**:
   - Auto-redirect HTTP → HTTPS
4. Enable **Automatic HTTPS Rewrites**:
   - Fixes mixed content warnings

### 7. Configure Page Rules (Optional)

1. Navigate to **Rules** → **Page Rules**
2. Create rule for root redirect:
   - URL: `controlpeso.thiscloud.com.ar/*`
   - Setting: **Always Use HTTPS** (On)
   - Setting: **Cache Level** (Standard)

### 8. Performance Optimization (Optional)

- **Auto Minify**: Enable HTML, CSS, JS minification
- **Brotli**: Enable compression
- **HTTP/2**: Enabled by default
- **HTTP/3 (QUIC)**: Enable in Network settings
- **Cache**: Configure cache TTL for static assets

## Verification

### Test DNS Propagation

```bash
# Check nameservers
nslookup -type=NS controlpeso.thiscloud.com.ar

# Check A record
nslookup controlpeso.thiscloud.com.ar

# Check HTTPS
curl -I https://controlpeso.thiscloud.com.ar
```

### Test Analytics Beacon

1. Open browser DevTools → Network tab
2. Navigate to `https://controlpeso.thiscloud.com.ar`
3. Look for request to `cloudflareinsights.com/cdn-cgi/rum`
4. Should return `204 No Content` (success)
5. Check Console for errors (should be clean)

### Verify in Cloudflare Dashboard

1. Wait 5-10 minutes after deployment
2. Navigate to **Analytics** → **Web Analytics**
3. Should see traffic data:
   - Page views
   - Visitors
   - Referrers
   - Countries
   - Core Web Vitals (LCP, FID, CLS)

## Troubleshooting

### Beacon Not Loading

- Check Content Security Policy allows `static.cloudflareinsights.com`
- Verify token is correct in script `data-cf-beacon` attribute
- Check browser Console for CSP or network errors
- Ensure script has `defer` attribute (non-blocking load)

### No Data in Dashboard

- Wait 5-10 minutes for data to appear
- Ensure site has real traffic (not just localhost)
- Verify beacon script is present in page source (View Page Source)
- Check Network tab for successful beacon request (204 status)

### DNS Not Resolving

- Wait 24-48 hours for full DNS propagation
- Use [DNS Checker](https://dnschecker.org/) to verify globally
- Ensure nameservers at registrar match Cloudflare's nameservers
- Clear local DNS cache: `ipconfig /flushdns` (Windows) or `sudo dscacheutil -flushcache` (macOS)

### HTTPS Certificate Errors

- Ensure origin server has valid SSL certificate
- Use Cloudflare Origin Certificate (generate in SSL/TLS → Origin Server)
- Set SSL mode to **Full (Strict)** in Cloudflare
- Wait for certificate provisioning (may take 15 minutes)

## Best Practices

1. **Privacy Compliance**: Cloudflare Analytics is cookieless, no consent banner needed
2. **CSP Whitelisting**: Already configured in `SecurityHeadersMiddleware.cs`
3. **Token Security**: Store token in User Secrets (Development) or env vars (Production), never commit to Git
4. **Monitoring**: Check dashboard regularly for traffic insights and Core Web Vitals
5. **Page Load Performance**: Beacon uses `defer` attribute to avoid blocking page load
6. **Multiple Environments**: Use different Cloudflare sites for Dev/Staging/Production

## Additional Resources

- [Cloudflare Web Analytics Docs](https://developers.cloudflare.com/analytics/web-analytics/)
- [DNS Setup Guide](https://developers.cloudflare.com/dns/zone-setups/full-setup/)
- [SSL/TLS Configuration](https://developers.cloudflare.com/ssl/)
- [Core Web Vitals](https://web.dev/vitals/)

## Security Notes

- Beacon token is **not** sensitive (public-facing, no security risk)
- However, still recommended to store in configuration for easier rotation
- CSP already configured to allow Cloudflare domains
- No cookies = GDPR/CCPA compliant by default
- No personal data collected (IP addresses anonymized)

## TODO

- [ ] Create Cloudflare account
- [ ] Add site `controlpeso.thiscloud.com.ar`
- [ ] Update nameservers at domain registrar
- [ ] Wait for DNS propagation verification
- [ ] Enable Web Analytics in Cloudflare Dashboard
- [ ] Copy beacon token
- [ ] Add token to User Secrets: `dotnet user-secrets set "CloudflareAnalytics:Token" "YOUR_TOKEN"`
- [ ] Integrate beacon script in `App.razor` (uncomment placeholder)
- [ ] Deploy to production
- [ ] Verify analytics data appears in dashboard
- [ ] Configure SSL/TLS to Full (Strict)
- [ ] Enable Always Use HTTPS
- [ ] Test HTTPS redirect working
- [ ] Monitor Core Web Vitals for performance optimization
