# SEO Infrastructure Deployment Checklist

## Pre-Deployment Verification

### 1. Code Review
- [ ] `SitemapService.cs` implementado y compilando
- [ ] `SeoController.cs` implementado y compilando
- [ ] Program.cs actualizado con:
  - [ ] `builder.Services.AddScoped<SitemapService>()`
  - [ ] `builder.Services.AddControllers()`
  - [ ] `app.MapControllers()`
- [ ] appsettings.json tiene `App:BaseUrl` configurado
- [ ] appsettings.Production.json tiene `App:BaseUrl` correcto

### 2. Configuration Verification
- [ ] Development BaseUrl: `https://localhost:7143`
- [ ] Production BaseUrl: `https://controlpeso.thiscloud.com.ar`
- [ ] Response caching configurado:
  - [ ] Sitemap: 1 hora (3600s)
  - [ ] Robots: 24 horas (86400s)

### 3. Local Testing
- [ ] Build exitoso (`dotnet build`)
- [ ] App runs locally (`dotnet run`)
- [ ] Test `/sitemap.xml` (browser o curl)
- [ ] Test `/robots.txt` (browser o curl)
- [ ] Run `pwsh test-seo-endpoints.ps1` → ALL TESTS PASSED
- [ ] Verificar logs: "SitemapService initialized with base URL: ..."

### 4. Sitemap Content Verification
- [ ] Incluye `/` (priority 1.0)
- [ ] Incluye `/login` (priority 0.9)
- [ ] Incluye `/privacy` + `/privacidad` (priority 0.7)
- [ ] Incluye `/terms` + `/terminos` (priority 0.7)
- [ ] Incluye `/changelog` + `/historial` (priority 0.6)
- [ ] NO incluye `/dashboard`, `/profile`, `/history`, `/trends`, `/admin`
- [ ] `lastmod` es fecha actual (dinámica)
- [ ] XML válido (sin errores de parseo)

### 5. Robots.txt Content Verification
- [ ] User-agent: * → Allow: / con disallows apropiados
- [ ] Incluye AI crawlers:
  - [ ] GPTBot (OpenAI)
  - [ ] ChatGPT-User (OpenAI browsing)
  - [ ] Claude-Web (Anthropic)
  - [ ] Google-Extended (Bard/Gemini)
  - [ ] FacebookBot (Meta AI)
  - [ ] CCBot (Common Crawl)
  - [ ] PerplexityBot
  - [ ] Applebot-Extended
  - [ ] ia_archiver (Amazon)
  - [ ] cohere-ai
- [ ] Bloqueados crawlers agresivos:
  - [ ] AhrefsBot
  - [ ] SemrushBot
  - [ ] MJ12bot
  - [ ] Scrapy
  - [ ] python-requests
  - [ ] curl/wget
- [ ] Sitemap reference: `Sitemap: https://controlpeso.thiscloud.com.ar/sitemap.xml`
- [ ] Crawl delays configurados (1-5s según bot)

---

## Deployment Steps

### 1. Git Commit
```bash
git add .
git commit -m "feat(seo): implement dynamic sitemap.xml and robots.txt with AI crawler support"
git push origin feature/deploy-production-v4.0.0
```

### 2. Create Pull Request
- [ ] PR creado: `feature/deploy-production-v4.0.0` → `develop`
- [ ] CI checks pasando
- [ ] Code review aprobado
- [ ] Merge a `develop`

### 3. Deploy to Production
- [ ] Build Docker image (si aplica)
- [ ] Deploy a servidor/container
- [ ] Verificar `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Verificar connection string correcto
- [ ] App starts sin errores

---

## Post-Deployment Verification

### 1. Production Endpoint Testing
```bash
# Test sitemap
curl https://controlpeso.thiscloud.com.ar/sitemap.xml

# Test robots
curl https://controlpeso.thiscloud.com.ar/robots.txt

# Run full test suite
pwsh test-seo-endpoints.ps1 production
```

Expected:
- [ ] Status 200 OK
- [ ] Correct Content-Type headers
- [ ] Cache-Control headers present
- [ ] BaseUrl correcto (production URL)

### 2. Search Console Submission

#### Google Search Console
1. [ ] Login: https://search.google.com/search-console
2. [ ] Add/verify property: `https://controlpeso.thiscloud.com.ar`
3. [ ] Go to **Sitemaps** → Submit: `https://controlpeso.thiscloud.com.ar/sitemap.xml`
4. [ ] Wait 24-48h for indexing
5. [ ] Verify **Coverage** report → No errors

#### Bing Webmaster Tools
1. [ ] Login: https://www.bing.com/webmasters
2. [ ] Add/verify site: `https://controlpeso.thiscloud.com.ar`
3. [ ] Go to **Sitemaps** → Submit: `https://controlpeso.thiscloud.com.ar/sitemap.xml`
4. [ ] Wait 24-48h for indexing

### 3. Robots.txt Validation

#### Google Robots Testing Tool
1. [ ] Go to: https://www.google.com/webmasters/tools/robots-testing-tool
2. [ ] Enter URL: `https://controlpeso.thiscloud.com.ar`
3. [ ] Test user-agents:
   - [ ] Googlebot → Should allow `/`, `/login`, block `/dashboard`
   - [ ] GPTBot → Should allow `/`, `/login`, block `/dashboard`
4. [ ] Verify no errors/warnings

#### Manual Testing
```bash
# Test as Googlebot
curl -A "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)" \
  https://controlpeso.thiscloud.com.ar/robots.txt

# Test as GPTBot
curl -A "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; GPTBot/1.0; +https://openai.com/gptbot)" \
  https://controlpeso.thiscloud.com.ar/robots.txt
```

### 4. Cache Verification
```bash
# First request (cache miss)
curl -I https://controlpeso.thiscloud.com.ar/sitemap.xml

# Second request (cache hit)
curl -I https://controlpeso.thiscloud.com.ar/sitemap.xml

# Verify Cache-Control header:
# Expected: Cache-Control: public, max-age=3600
```

### 5. Monitoring

#### Check Logs
```bash
# Docker logs
docker logs controlpeso-web --tail=100 --follow

# Expected log entries:
# "SitemapService initialized with base URL: https://controlpeso.thiscloud.com.ar"
# "Sitemap.xml requested from {UserAgent}"
# "Robots.txt requested from {UserAgent}"
```

#### Analytics (after 7 days)
- [ ] Google Search Console → Performance
  - [ ] Check impressions increase
  - [ ] Check click-through rate
  - [ ] Check indexed pages count
- [ ] Google Analytics → Acquisition → Traffic Acquisition
  - [ ] Check organic search traffic
  - [ ] Check referral from search engines

---

## Rollback Plan

Si algo falla en producción:

### Option 1: Disable Dynamic Endpoints (Quick Fix)
1. Comment out `app.MapControllers()` en Program.cs
2. Redeploy (uses static wwwroot/sitemap.xml and robots.txt)
3. Investigate issue offline

### Option 2: Hotfix
1. Fix bug en código
2. Test localmente
3. Git commit + push
4. Deploy hotfix branch directamente a production

### Option 3: Full Rollback
```bash
git revert <commit-hash>
git push origin main
# Redeploy previous version
```

---

## Success Criteria

✅ **All checkboxes above completed**

✅ **Production endpoints working**:
- https://controlpeso.thiscloud.com.ar/sitemap.xml → 200 OK
- https://controlpeso.thiscloud.com.ar/robots.txt → 200 OK

✅ **Google Search Console**:
- Sitemap submitted successfully
- No errors in Coverage report

✅ **Logs showing requests**:
- "Sitemap.xml requested from Googlebot"
- "Robots.txt requested from GPTBot"

✅ **SEO improvements visible** (after 30 days):
- Increased organic search traffic
- Increased indexed pages
- Improved search rankings

---

## Notes

- Static files `wwwroot/robots.txt` y `wwwroot/sitemap.xml` ahora tienen deprecation notice
- Endpoints dinámicos tienen **prioridad** sobre archivos estáticos
- Cache headers mejoran performance (menos carga CPU)
- AI crawler support aumenta visibilidad en ChatGPT, Claude, Bard, etc.

---

**Deployment Date**: _____________  
**Deployed By**: _____________  
**Version**: 4.0.0  
**Status**: ⬜ Pending / ⬜ In Progress / ⬜ Completed / ⬜ Failed
