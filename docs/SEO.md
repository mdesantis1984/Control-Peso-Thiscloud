# SEO Documentation - Control Peso Thiscloud

## Overview

SEO strategy optimized for body weight tracking keywords, focusing on public pages (Home, Login) with `index,follow`, while protecting user data with `noindex,nofollow` on authenticated pages.

## Meta Tags Implementation

### Structure (All Pages)

```razor
<PageTitle>Page Title - Control Peso Thiscloud</PageTitle>

<HeadContent>
    <meta name="description" content="70-150 character description" />
    <meta name="keywords" content="control peso, seguimiento, registro, peso corporal" />
    <meta name="robots" content="index, follow" OR "noindex, nofollow" />
    <link rel="canonical" href="https://controlpeso.thiscloud.com.ar/path" />
    
    <!-- Open Graph -->
    <meta property="og:title" content="Page Title" />
    <meta property="og:description" content="Description" />
    <meta property="og:type" content="website" />
    <meta property="og:url" content="https://controlpeso.thiscloud.com.ar/path" />
    <meta property="og:image" content="https://controlpeso.thiscloud.com.ar/images/og-image.png" />
    
    <!-- Twitter Card (public pages only) -->
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="Page Title" />
    <meta name="twitter:description" content="Description" />
    <meta name="twitter:image" content="https://controlpeso.thiscloud.com.ar/images/og-image.png" />
</HeadContent>
```

### Pages Overview

| Page | robots | Priority | Canonical URL | Social Cards |
|------|--------|----------|---------------|--------------|
| Home | index,follow | 1.0 | / | ✅ |
| Login | index,follow | 0.8 | /login | ✅ |
| Dashboard | noindex,nofollow | N/A | /dashboard | ❌ |
| Profile | noindex,nofollow | N/A | /profile | ❌ |
| History | noindex,nofollow | N/A | /history | ❌ |
| Trends | noindex,nofollow | N/A | /trends | ❌ |
| Admin | noindex,nofollow | N/A | /admin | ❌ |
| Error | noindex,nofollow | N/A | N/A | ❌ |

### Rationale

- **Public pages** (Home, Login): Indexable for organic traffic
- **Authenticated pages**: `noindex,nofollow` prevents sensitive user data in search results
- **Canonical URLs**: Hardcoded to production domain (⚠️ update if domain changes)
- **Twitter Cards**: Only on public pages (protected pages not shareable)

## robots.txt

**Location**: `wwwroot/robots.txt`

```
User-agent: *

# Public pages (indexable)
Allow: /
Allow: /login

# Protected pages (not indexable)
Disallow: /dashboard
Disallow: /profile
Disallow: /history
Disallow: /trends
Disallow: /admin
Disallow: /logout
Disallow: /counter
Disallow: /weather

# Sitemap location
Sitemap: https://controlpeso.thiscloud.com.ar/sitemap.xml

# Crawl delay (optional, polite bots)
Crawl-delay: 1
```

## sitemap.xml

**Location**: `wwwroot/sitemap.xml`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
    <url>
        <loc>https://controlpeso.thiscloud.com.ar/</loc>
        <lastmod>2026-02-18</lastmod>
        <changefreq>monthly</changefreq>
        <priority>1.0</priority>
    </url>
    <url>
        <loc>https://controlpeso.thiscloud.com.ar/login</loc>
        <lastmod>2026-02-18</lastmod>
        <changefreq>monthly</changefreq>
        <priority>0.8</priority>
    </url>
</urlset>
```

**Note**: Update `<lastmod>` manually with each release (or automate with CI/CD build date).

## Open Graph Image

**Specification**: `wwwroot/images/README_og-image.md`

- **Dimensions**: 1200x630px (Facebook/LinkedIn/Twitter standard)
- **Format**: PNG, < 300KB
- **Content**: Logo + icon (scale) + tagline "Seguimiento de Peso Corporal Simple y Efectivo" + dark background + brand colors (#1E88E5 blue, #FFC107 amber)

**TODO**:
- [ ] Create `og-image.png` with design tool (Figma/Canva)
- [ ] Place in `wwwroot/images/og-image.png`
- [ ] Test with [Facebook Sharing Debugger](https://developers.facebook.com/tools/debug/)
- [ ] Test with [Twitter Card Validator](https://cards-dev.twitter.com/validator)
- [ ] Test with [LinkedIn Post Inspector](https://www.linkedin.com/post-inspector/)

## Target Keywords

### Primary Keywords (Spanish)
- control peso
- seguimiento peso
- registro peso corporal
- app peso
- aplicación control peso
- peso corporal

### Secondary Keywords (Spanish)
- gráfico evolución peso
- análisis tendencias peso
- IMC calculadora
- seguimiento salud
- peso ideal
- pérdida peso

### Long-tail Keywords (Spanish)
- app gratuita control peso
- seguimiento peso corporal simple
- registro diario peso
- gráfico peso corporal
- análisis peso semanal

### English Keywords (Future)
- weight tracking
- body weight log
- weight control app
- BMI calculator
- weight loss tracker

## SEO Best Practices Applied

### On-Page SEO
- [x] Unique `<PageTitle>` per page
- [x] Descriptive meta descriptions (70-150 chars)
- [x] Relevant keywords in content
- [x] Semantic HTML5 (h1, h2, nav, main, footer)
- [x] Alt text on images (⚠️ verify when og-image created)
- [x] Internal linking (NavMenu links all pages)
- [x] Mobile responsive (MudBlazor responsive by default)
- [x] Fast load times (Blazor Server, CDN MudBlazor)

### Technical SEO
- [x] HTTPS enforcement (`UseHttpsRedirection`)
- [x] Canonical URLs (prevent duplicate content)
- [x] robots.txt (guide crawlers)
- [x] sitemap.xml (help indexing)
- [x] Structured data (⚠️ TODO: JSON-LD for app metadata)
- [x] Clean URLs (no query params, e.g., `/dashboard` not `/dashboard?id=123`)
- [x] 404 error page (`/Error`)

### Off-Page SEO (Future)
- [ ] Backlinks (share on social media, blogs, forums)
- [ ] Social signals (Twitter, Facebook, LinkedIn shares)
- [ ] Google My Business (local SEO, if applicable)
- [ ] App listings (Product Hunt, AlternativeTo, Capterra)

## Local SEO (Argentina Focus)

- **Target Region**: Argentina (`es-AR` locale, ARS currency if e-commerce)
- **Timezone**: `America/Argentina/Buenos_Aires`
- **Language**: Spanish (`es`) primary, English (`en`) secondary
- **Google Search Console**: Submit sitemap, monitor Argentina traffic
- **Bing Webmaster Tools**: Submit sitemap (Bing market share ~3% LATAM)

## Analytics Integration

### Google Analytics 4

**Configuration**: `appsettings.json`
```json
{
  "GoogleAnalytics": {
    "MeasurementId": "G-XXXXXXXXX"
  }
}
```

**Tracking**:
- Page views (automatic)
- Events (manual): `gtag('event', 'add_weight', { ... })`
- Conversions: Sign up (OAuth login), weight log created

### Cloudflare Analytics

**Free Tier**: Privacy-first, no cookies, GDPR compliant

**Setup**: See `docs/CLOUDFLARE_ANALYTICS.md`

## Performance SEO

### Core Web Vitals

| Metric | Target | Current | Notes |
|--------|--------|---------|-------|
| LCP (Largest Contentful Paint) | < 2.5s | ⏳ Test | Optimize images, use CDN |
| FID (First Input Delay) | < 100ms | ⏳ Test | Blazor Server fast |
| CLS (Cumulative Layout Shift) | < 0.1 | ⏳ Test | MudBlazor stable layouts |

**Test Tools**:
- Lighthouse (Chrome DevTools)
- PageSpeed Insights (Google)
- WebPageTest.org

### Optimization Tips

1. **Images**: Compress `og-image.png` (< 300KB), use WebP if supported
2. **Fonts**: Preload Google Fonts (Roboto) in `App.razor`
3. **CSS/JS**: MudBlazor CDN (cached), minified
4. **Caching**: Add `Cache-Control` headers for static assets
5. **CDN**: Consider Azure CDN or Cloudflare CDN for global delivery

## Structured Data (JSON-LD)

**TODO**: Add to `App.razor` or `Home.razor`:

```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "WebApplication",
  "name": "Control Peso Thiscloud",
  "url": "https://controlpeso.thiscloud.com.ar",
  "description": "Aplicación web gratuita para seguimiento de peso corporal con gráficos, análisis de tendencias e IMC",
  "applicationCategory": "HealthApplication",
  "operatingSystem": "Web Browser",
  "offers": {
    "@type": "Offer",
    "price": "0",
    "priceCurrency": "USD"
  }
}
</script>
```

## Google Search Console Setup

1. Verify ownership:
   - HTML file upload, OR
   - DNS TXT record, OR
   - Google Analytics
2. Submit sitemap: `https://controlpeso.thiscloud.com.ar/sitemap.xml`
3. Monitor:
   - Index coverage (errors/warnings)
   - Performance (clicks, impressions, CTR, position)
   - Mobile usability
   - Core Web Vitals

## Bing Webmaster Tools Setup

1. Verify ownership (similar to GSC)
2. Submit sitemap
3. Monitor SEO reports

## SEO Checklist

### Pre-Launch
- [x] Meta tags on all pages
- [x] robots.txt configured
- [x] sitemap.xml generated
- [ ] Open Graph image created (`og-image.png`)
- [ ] Structured data (JSON-LD) added
- [ ] 404 error page tested
- [ ] Mobile responsive verified
- [ ] HTTPS certificate installed

### Post-Launch
- [ ] Submit sitemap to Google Search Console
- [ ] Submit sitemap to Bing Webmaster Tools
- [ ] Test Open Graph with Facebook/Twitter/LinkedIn validators
- [ ] Monitor Google Analytics (traffic, bounce rate, conversions)
- [ ] Check indexing status (site:controlpeso.thiscloud.com.ar in Google)
- [ ] Monitor Core Web Vitals (Lighthouse, PageSpeed Insights)
- [ ] Build backlinks (social media, blog posts, forums)

### Ongoing
- [ ] Update meta descriptions seasonally (e.g., "peso año nuevo")
- [ ] Create content marketing (blog posts about weight loss tips)
- [ ] Monitor keyword rankings (SEM Rush, Ahrefs, or free tools)
- [ ] A/B test meta titles/descriptions
- [ ] Analyze user search queries (Google Search Console → Performance)
- [ ] Respond to reviews (Google My Business, if applicable)

## Troubleshooting

### Page Not Indexed
- Check robots.txt allows crawling (`Allow: /page`)
- Verify sitemap includes URL
- Check `meta robots` not `noindex`
- Submit URL for inspection in Google Search Console

### Low CTR (Click-Through Rate)
- Improve meta title (add numbers, benefits, "gratuito")
- Enhance meta description (call-to-action)
- Test different keywords

### High Bounce Rate
- Ensure content matches user intent
- Improve page load speed
- Add internal links (encourage exploration)
- Clear call-to-action (e.g., "Registra tu peso ahora")

## References

- [Google SEO Starter Guide](https://developers.google.com/search/docs/beginner/seo-starter-guide)
- [Open Graph Protocol](https://ogp.me/)
- [Twitter Card Documentation](https://developer.twitter.com/en/docs/twitter-for-websites/cards/overview/abouts-cards)
- [Schema.org WebApplication](https://schema.org/WebApplication)
- [Core Web Vitals](https://web.dev/vitals/)
