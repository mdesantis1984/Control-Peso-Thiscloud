# SEO Infrastructure - Control Peso Thiscloud

> **Verified against the code**: 2026-11-19 (version 9.0.0)

`/robots.txt` and `/sitemap.xml` are generated dynamically; there are no static files.

| Piece | Location |
|-------|----------|
| Endpoints (Minimal API) | `src/ControlPeso.Web/Extensions/SeoEndpointsExtensions.cs` (`app.MapSeoEndpoints()` in `Program.cs`) |
| Generator | `src/ControlPeso.Web/Services/SitemapService.cs` (scoped service) |
| Base URL | `App:BaseUrl` (default `https://controlpeso.thiscloud.com.ar`) |

There is no `SeoController`; the endpoints are Minimal API routes.

## Endpoints

| Route | Content-Type | Cache-Control |
|-------|--------------|---------------|
| `GET /sitemap.xml` | `application/xml; charset=utf-8` | `public, max-age=3600` (1 h) |
| `GET /robots.txt` | `text/plain; charset=utf-8` | `public, max-age=86400` (24 h) |

Both log an `Information` entry with the requesting `User-Agent` and return `500` if generation fails (the exception is logged).

The `Dockerfile` deletes `wwwroot/robots.txt*` and `wwwroot/sitemap.xml*` from the published output so a static file can never shadow the dynamic route.

## Sitemap

Only public pages are listed, with `lastmod` set to the current UTC date:

| URL | Priority | Change frequency |
|-----|----------|------------------|
| `/` | 1.0 | daily |
| `/login` | 0.9 | weekly |
| `/privacy`, `/privacidad` | 0.7 | monthly |
| `/terms`, `/terminos` | 0.7 | monthly |
| `/changelog`, `/historial` | 0.6 | weekly |

Authenticated pages (`/dashboard`, `/profile`, `/history`, `/trends`, `/admin`, `/logout`) are not listed.

## robots.txt

- `User-agent: *`: allows `/`, disallows the authenticated pages above and the technical paths `/api/`, `/_blazor/`, `/_framework/`, `/diagnostics/` (and the legacy `/testflags`), explicitly allows the public pages, `Crawl-delay: 1`.
- **AI and link-preview crawlers** (about 35 providers, 50+ user-agents in 8 groups: LLM providers, big tech, AI search, training datasets, international, social, messaging link previews, enterprise): same allow/disallow rules as `*`, with a per-provider `Crawl-delay` of 2-5 s. `Bytespider` is fully disallowed.
- **Blocked completely** (`Disallow: /`): aggressive SEO tools (AhrefsBot, SemrushBot, MJ12bot, ...), scrapers and site copiers (Scrapy, python-requests, curl, wget, HTTrack, ...), email harvesters and similar bots, and `archive.org_bot`.
- The file ends with `Sitemap: {App:BaseUrl}/sitemap.xml`.

The full list of user-agents lives in `SitemapService.GenerateRobotsTxt()`; that method is the single source of truth, so edit it there when adding or removing a bot.

## Configuration

```json
{ "App": { "BaseUrl": "https://controlpeso.thiscloud.com.ar" } }
```

Override per environment (for example `https://localhost:7143` in development). If `App:BaseUrl` is missing the production URL above is used, so a wrong base URL in the sitemap means the override is not being applied.

## Verification

```bash
curl -s https://controlpeso.thiscloud.com.ar/sitemap.xml | head
curl -s https://controlpeso.thiscloud.com.ar/robots.txt | head -20
curl -sI https://controlpeso.thiscloud.com.ar/robots.txt | grep -i cache-control
```

Production sits behind Cloudflare, which also honours the `Cache-Control` headers. Submit the sitemap in Google Search Console once per property.

## Troubleshooting

| Symptom | Check |
|---------|-------|
| 404 on `/sitemap.xml` or `/robots.txt` | `app.MapSeoEndpoints()` is called in `Program.cs` and `SitemapService` is registered in DI. |
| Sitemap shows `localhost` URLs | `App__BaseUrl` is set in the production environment (compose file or `appsettings.Production.json`). |
| Old robots.txt content | Cloudflare/browser cache (24 h); purge it, and confirm no static file exists in `wwwroot`. |

Related: [ARCHITECTURE.md](./ARCHITECTURE.md), [DEPLOYMENT.md](./DEPLOYMENT.md).
