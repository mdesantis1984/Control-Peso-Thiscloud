using System.Globalization;
using System.Text;
using System.Xml;

namespace ControlPeso.Web.Services;

/// <summary>
/// Service for generating dynamic sitemap.xml and robots.txt for SEO optimization with comprehensive AI crawler support
/// </summary>
public sealed class SitemapService
{
    private readonly ILogger<SitemapService> _logger;
    private readonly string _baseUrl;

    // Public URLs accessible to crawlers
    private static readonly string[] PublicUrls = 
    [
        "/",
        "/login",
        "/privacy",
        "/privacidad",
        "/terms",
        "/terminos",
        "/changelog",
        "/historial"
    ];

    // Protected URLs requiring authentication
    private static readonly string[] ProtectedUrls = 
    [
        "/dashboard",
        "/profile",
        "/history",
        "/trends",
        "/admin",
        "/logout"
    ];

    // Technical/internal URLs to block
    private static readonly string[] InternalUrls = 
    [
        "/api/",
        "/_blazor/",
        "/_framework/",
        "/diagnostics/",
        "/counter",
        "/weather",
        "/testflags"
    ];

    public SitemapService(ILogger<SitemapService> logger, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);

        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://controlpeso.thiscloud.com.ar";

        _logger.LogInformation("SitemapService initialized with base URL: {BaseUrl}", _baseUrl);
    }
    
    /// <summary>
    /// Generates XML sitemap with all public URLs
    /// </summary>
    public string GenerateSitemap()
    {
        _logger.LogInformation("Generating dynamic sitemap.xml");
        
        try
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using var writer = XmlWriter.Create(sb, settings);
            
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, 
                "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");

            var now = DateTime.UtcNow;
            var lastMod = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            
            // Define public pages with SEO metadata
            var publicPages = new[]
            {
                // Home - Maximum priority
                new SitemapUrl("/", lastMod, "daily", 1.0),
                
                // Login - High priority for conversion
                new SitemapUrl("/login", lastMod, "weekly", 0.9),
                
                // Legal pages - Medium-high priority (important for trust/compliance)
                new SitemapUrl("/privacy", lastMod, "monthly", 0.7),
                new SitemapUrl("/privacidad", lastMod, "monthly", 0.7),
                new SitemapUrl("/terms", lastMod, "monthly", 0.7),
                new SitemapUrl("/terminos", lastMod, "monthly", 0.7),
                
                // Changelog - Medium priority (useful for users and search engines)
                new SitemapUrl("/changelog", lastMod, "weekly", 0.6),
                new SitemapUrl("/historial", lastMod, "weekly", 0.6),
            };

            foreach (var page in publicPages)
            {
                WriteUrlElement(writer, page);
            }

            writer.WriteEndElement(); // urlset
            writer.WriteEndDocument();
            writer.Flush();

            var sitemap = sb.ToString();
            
            _logger.LogInformation(
                "Sitemap generated successfully with {PageCount} URLs", 
                publicPages.Length);
            
            return sitemap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sitemap");
            throw;
        }
    }

    private void WriteUrlElement(XmlWriter writer, SitemapUrl url)
    {
        writer.WriteStartElement("url");
        
        writer.WriteElementString("loc", $"{_baseUrl}{url.Path}");
        writer.WriteElementString("lastmod", url.LastModified);
        writer.WriteElementString("changefreq", url.ChangeFrequency);
        writer.WriteElementString("priority", url.Priority.ToString("F1", CultureInfo.InvariantCulture));
        
        writer.WriteEndElement(); // url
    }

    /// <summary>
    /// Generates robots.txt with comprehensive AI crawler support (40+ AI agents)
    /// Updated: 2026-02-28 with latest AI crawlers
    /// </summary>
    public string GenerateRobotsTxt()
    {
        _logger.LogInformation("Generating dynamic robots.txt with comprehensive AI crawler support");

        var sb = new StringBuilder();

        // ============================================
        // HEADER
        // ============================================
        sb.AppendLine("# robots.txt for Control Peso Thiscloud");
        sb.AppendLine($"# {_baseUrl}/robots.txt");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"# AI Crawlers Supported: {GetAiCrawlerCount()}+ agents");
        sb.AppendLine();

        // ============================================
        // STANDARD WEB CRAWLERS (Google, Bing, etc.)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# STANDARD WEB SEARCH ENGINES");
        sb.AppendLine("# ============================================");
        sb.AppendLine();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine();

        // Protected pages
        sb.AppendLine("# Authenticated pages - require login");
        foreach (var url in ProtectedUrls)
        {
            sb.AppendLine($"Disallow: {url}");
        }
        sb.AppendLine();

        // Internal/technical URLs
        sb.AppendLine("# Internal/technical endpoints");
        foreach (var url in InternalUrls)
        {
            sb.AppendLine($"Disallow: {url}");
        }
        sb.AppendLine();

        // Public pages explicit allow
        sb.AppendLine("# Public pages - explicitly allowed");
        foreach (var url in PublicUrls)
        {
            sb.AppendLine($"Allow: {url}");
        }
        sb.AppendLine();
        sb.AppendLine("Crawl-delay: 1");
        sb.AppendLine();

        // ============================================
        // AI CRAWLERS - TIER 1 (Major LLM Providers)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 1: MAJOR LLM PROVIDERS");
        sb.AppendLine("# OpenAI, Anthropic, Google, Microsoft");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // OpenAI (GPT-4, GPT-4o, o1, o3)
        AddAiCrawlerRules(sb, "OpenAI GPT (ChatGPT, GPT-4, o1, o3)", 
            ["GPTBot", "ChatGPT-User", "OAI-SearchBot"], 2);

        // Anthropic (Claude 3, Claude 3.5 Sonnet, Claude 4)
        AddAiCrawlerRules(sb, "Anthropic Claude (Claude 3/3.5/4, Sonnet, Opus)", 
            ["Claude-Web", "ClaudeBot", "anthropic-ai"], 2);

        // Google (Gemini, Bard, PaLM 2)
        AddAiCrawlerRules(sb, "Google Gemini/Bard (Gemini 1.5/2.0, PaLM 2)", 
            ["Google-Extended", "Googlebot-Image", "Googlebot-Video", "Googlebot-News"], 2);

        // Microsoft (Copilot, Bing Chat, GPT-4 integration)
        AddAiCrawlerRules(sb, "Microsoft Copilot (Bing Chat, GitHub Copilot)", 
            ["Bingbot", "BingPreview", "msnbot", "MSNBot-Media"], 2);

        // ============================================
        // AI CRAWLERS - TIER 2 (Meta, Amazon, Apple)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 2: BIG TECH AI");
        sb.AppendLine("# Meta, Amazon, Apple, X/Twitter");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Meta AI (LLaMA 2, LLaMA 3, LLaMA 3.1, Meta AI)
        AddAiCrawlerRules(sb, "Meta AI (LLaMA 2/3/3.1, Meta AI Assistant)", 
            ["FacebookBot", "Meta-ExternalAgent", "facebookexternalhit", "facebookcatalog"], 3);

        // Amazon (Alexa, Amazon Q, Bedrock)
        AddAiCrawlerRules(sb, "Amazon AI (Alexa, Amazon Q, Bedrock)", 
            ["ia_archiver", "Amazonbot", "alexa site audit"], 3);

        // Apple (Apple Intelligence, Siri, Spotlight)
        AddAiCrawlerRules(sb, "Apple Intelligence (Siri, Spotlight, Apple Intelligence)", 
            ["Applebot", "Applebot-Extended", "AppleNewsBot"], 2);

        // X/Twitter (Grok AI by xAI)
        AddAiCrawlerRules(sb, "X/Twitter Grok AI (xAI Grok, formerly Twitter)", 
            ["TwitterBot", "Twitterbot"], 3);

        // ============================================
        // AI CRAWLERS - TIER 3 (Specialized AI Search)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 3: SPECIALIZED AI SEARCH");
        sb.AppendLine("# Perplexity, You.com, Brave, DuckDuckGo");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Perplexity AI
        AddAiCrawlerRules(sb, "Perplexity AI (AI-powered search engine)", 
            ["PerplexityBot", "Perplexity"], 3);

        // You.com (YouBot AI search)
        AddAiCrawlerRules(sb, "You.com AI (AI search engine)", 
            ["YouBot"], 3);

        // Brave Search AI
        AddAiCrawlerRules(sb, "Brave Search AI (Privacy-focused AI search)", 
            ["Brave-Search-Bot", "BraveBot"], 3);

        // DuckDuckGo AI
        AddAiCrawlerRules(sb, "DuckDuckGo AI (Privacy-focused search with AI)", 
            ["DuckDuckBot", "DuckDuckGo-Favicons-Bot"], 3);

        // ============================================
        // AI CRAWLERS - TIER 4 (AI Training & Datasets)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 4: AI TRAINING & DATASETS");
        sb.AppendLine("# Common Crawl, Internet Archive, Research Orgs");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Common Crawl (major AI training dataset source)
        AddAiCrawlerRules(sb, "Common Crawl (Web archive for AI training datasets)", 
            ["CCBot"], 5);

        // Cohere AI
        AddAiCrawlerRules(sb, "Cohere AI (Enterprise LLM provider)", 
            ["cohere-ai"], 3);

        // AI2 Bot (Allen Institute for AI - Semantic Scholar, OLMo)
        AddAiCrawlerRules(sb, "AI2 Bot (Allen Institute - Semantic Scholar, OLMo)", 
            ["ai2bot"], 4);

        // Diffbot (AI-powered web data extraction)
        AddAiCrawlerRules(sb, "Diffbot (AI-powered knowledge graph)", 
            ["Diffbot"], 4);

        // ImagesiftBot (AI image search and analysis)
        AddAiCrawlerRules(sb, "ImagesiftBot (AI image search)", 
            ["ImagesiftBot"], 4);

        // ============================================
        // AI CRAWLERS - TIER 5 (International AI)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 5: INTERNATIONAL AI");
        sb.AppendLine("# Baidu, ByteDance, Naver, Yandex");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Baidu AI (ERNIE Bot, Baidu AI)
        AddAiCrawlerRules(sb, "Baidu AI (ERNIE Bot, China's leading AI)", 
            ["Baiduspider", "Baiduspider-render", "Baiduspider-image"], 4);

        // ByteDance AI (Doubao, TikTok AI)
        AddAiCrawlerRules(sb, "ByteDance AI (Doubao, TikTok AI) - LIMITED ACCESS", 
            ["Bytespider"], 5, allowPublicOnly: false); // Note: Limited due to aggressive crawling

        // Naver AI (HyperCLOVA X, Korea's leading AI)
        AddAiCrawlerRules(sb, "Naver AI (HyperCLOVA X, Korea)", 
            ["NaverBot", "Yeti"], 4);

        // Yandex AI (YaLM, YandexGPT)
        AddAiCrawlerRules(sb, "Yandex AI (YaLM, YandexGPT, Russia)", 
            ["YandexBot", "YandexImages", "YandexVideo"], 4);

        // Sogou AI (China)
        AddAiCrawlerRules(sb, "Sogou AI (China search engine with AI)", 
            ["Sogou web spider", "Sogou inst spider"], 4);

        // ============================================
        // AI CRAWLERS - TIER 6 (Social Media AI)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 6: SOCIAL MEDIA AI");
        sb.AppendLine("# LinkedIn, Pinterest, Instagram, Reddit");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // LinkedIn AI (LinkedIn Learning, Recruiter AI)
        AddAiCrawlerRules(sb, "LinkedIn AI (LinkedIn Learning, Recruiter AI)", 
            ["LinkedInBot"], 3);

        // Pinterest AI (Visual search, recommendations)
        AddAiCrawlerRules(sb, "Pinterest AI (Visual search, product recommendations)", 
            ["Pinterestbot", "Pinterest"], 3);

        // Instagram AI (Meta AI integration)
        AddAiCrawlerRules(sb, "Instagram AI (Meta AI integration)", 
            ["Instagram"], 4);

        // Reddit AI (Conversation analysis, recommendations)
        AddAiCrawlerRules(sb, "Reddit AI (Conversation analysis)", 
            ["redditbot"], 4);

        // Quora AI (Answer generation, recommendations)
        AddAiCrawlerRules(sb, "Quora AI (Answer generation)", 
            ["Quora-LinkPreview"], 4);

        // ============================================
        // AI CRAWLERS - TIER 7 (Messaging Platforms AI)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 7: MESSAGING PLATFORMS");
        sb.AppendLine("# Telegram, WhatsApp, Slack, Discord");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Telegram (Link preview bot)
        AddAiCrawlerRules(sb, "Telegram (Link preview bot)", 
            ["TelegramBot"], 3);

        // WhatsApp (Link preview)
        AddAiCrawlerRules(sb, "WhatsApp (Link preview)", 
            ["WhatsApp"], 3);

        // Slack (Link unfurling bot)
        AddAiCrawlerRules(sb, "Slack (Link unfurling bot)", 
            ["Slackbot", "Slack-ImgProxy"], 3);

        // Discord (Link embed bot)
        AddAiCrawlerRules(sb, "Discord (Link embed bot)", 
            ["Discordbot"], 3);

        // ============================================
        // AI CRAWLERS - TIER 8 (Enterprise & Niche AI)
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AI CRAWLERS - TIER 8: ENTERPRISE & NICHE AI");
        sb.AppendLine("# Salesforce, HubSpot, Zendesk, etc.");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        // Salesforce Einstein AI
        AddAiCrawlerRules(sb, "Salesforce Einstein AI", 
            ["SalesforceBot"], 4);

        // Anthropic (additional variants)
        AddAiCrawlerRules(sb, "Anthropic Research Crawler", 
            ["anthropic-research"], 4);

        // Hugging Face (AI model testing)
        AddAiCrawlerRules(sb, "Hugging Face (AI model testing)", 
            ["HuggingFaceBot"], 4);

        // OpenAI Research (additional research crawler)
        AddAiCrawlerRules(sb, "OpenAI Research Crawler", 
            ["OpenAI-Research"], 4);

        // ============================================
        // AGGRESSIVE/UNWANTED BOTS - BLOCK COMPLETELY
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# AGGRESSIVE OR UNWANTED BOTS - BLOCKED");
        sb.AppendLine("# SEO crawlers, scrapers, unauthorized data miners");
        sb.AppendLine("# ============================================");
        sb.AppendLine();

        var blockedBots = new[]
        {
            // SEO Tools (aggressive crawling)
            "AhrefsBot",
            "SemrushBot",
            "MJ12bot",
            "DotBot",
            "BLEXBot",
            "DataForSeoBot",
            "PetalBot",
            "SeznamBot",
            "LinkpadBot",
            "Screaming Frog SEO Spider",

            // Scrapers & Data Miners
            "Scrapy",
            "python-requests",
            "curl",
            "wget",
            "HTTrack",
            "WebCopier",
            "WebZIP",
            "WebReaper",
            "SiteSnagger",
            "WebStripper",

            // Aggressive/Malicious
            "EmailCollector",
            "EmailSiphon",
            "EmailWolf",
            "ExtractorPro",
            "CherryPicker",
            "WebBandit",
            "Teleport",
            "TeleportPro",
            "WebAutomatic",
            "Webster",

            // Ad/Spam Bots
            "MegaIndex",
            "ZumBot",
            "DomainCrawler",
            "archive.org_bot", // Block if privacy concern
        };

        foreach (var bot in blockedBots)
        {
            sb.AppendLine($"User-agent: {bot}");
            sb.AppendLine("Disallow: /");
            sb.AppendLine();
        }

        // ============================================
        // SITEMAP & CRAWL SETTINGS
        // ============================================
        sb.AppendLine("# ============================================");
        sb.AppendLine("# SITEMAP LOCATION");
        sb.AppendLine("# ============================================");
        sb.AppendLine($"Sitemap: {_baseUrl}/sitemap.xml");
        sb.AppendLine();

        _logger.LogInformation("Robots.txt generated successfully with {CrawlerCount}+ AI crawlers", GetAiCrawlerCount());

        return sb.ToString();
    }

    /// <summary>
    /// Helper method to add AI crawler rules with consistent formatting
    /// </summary>
    private void AddAiCrawlerRules(
        StringBuilder sb, 
        string description, 
        string[] userAgents, 
        int crawlDelay,
        bool allowPublicOnly = true)
    {
        sb.AppendLine($"# {description}");

        foreach (var agent in userAgents)
        {
            sb.AppendLine($"User-agent: {agent}");
        }

        if (allowPublicOnly)
        {
            // Allow public pages only
            foreach (var url in PublicUrls)
            {
                sb.AppendLine($"Allow: {url}");
            }

            // Block protected pages
            foreach (var url in ProtectedUrls)
            {
                sb.AppendLine($"Disallow: {url}");
            }

            // Block internal URLs
            foreach (var url in InternalUrls)
            {
                sb.AppendLine($"Disallow: {url}");
            }
        }
        else
        {
            // Block everything for aggressive crawlers
            sb.AppendLine("Disallow: /");
        }

        sb.AppendLine($"Crawl-delay: {crawlDelay}");
        sb.AppendLine();
    }

    /// <summary>
    /// Returns the total count of AI crawlers supported
    /// </summary>
    private static int GetAiCrawlerCount()
    {
        // Count based on tiers:
        // Tier 1: 4 providers (OpenAI, Anthropic, Google, Microsoft)
        // Tier 2: 4 providers (Meta, Amazon, Apple, X/Twitter)
        // Tier 3: 4 providers (Perplexity, You.com, Brave, DuckDuckGo)
        // Tier 4: 5 providers (Common Crawl, Cohere, AI2, Diffbot, ImagesiftBot)
        // Tier 5: 5 providers (Baidu, ByteDance, Naver, Yandex, Sogou)
        // Tier 6: 5 providers (LinkedIn, Pinterest, Instagram, Reddit, Quora)
        // Tier 7: 4 providers (Telegram, WhatsApp, Slack, Discord)
        // Tier 8: 4 providers (Salesforce, Anthropic Research, Hugging Face, OpenAI Research)
        // Total: 35 AI providers with 50+ user-agent variants
        return 50;
    }
}

/// <summary>
/// Sitemap URL entry with SEO metadata
/// </summary>
internal record SitemapUrl(
    string Path,
    string LastModified,
    string ChangeFrequency,
    double Priority
);
