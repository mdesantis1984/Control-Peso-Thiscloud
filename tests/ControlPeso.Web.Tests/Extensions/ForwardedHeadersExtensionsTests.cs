using System.Net;
using ControlPeso.Web.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ControlPeso.Web.Tests.Extensions;

public sealed class ForwardedHeadersExtensionsTests
{
    private const string ClientIp = "198.51.100.7";

    private static IWebHostEnvironment Production()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");
        return env.Object;
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static ForwardedHeadersOptions Options(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddForwardedHeadersConfiguration(Production(), configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    /// <summary>Runs the forwarded headers middleware for a request coming from <paramref name="remoteIp"/>.</summary>
    private static async Task<HttpContext> SendAsync(IConfiguration configuration, string remoteIp)
    {
        var options = Options(configuration);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = ClientIp;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "controlpeso.example.test";

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Microsoft.Extensions.Options.Options.Create(options));
        await middleware.Invoke(context);
        return context;
    }

    [Fact]
    public async Task UntrustedRemote_WithForwardedFor_DoesNotChangeRemoteIpOrScheme()
    {
        var context = await SendAsync(Config(), "203.0.113.9");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.9"));
        context.Request.Scheme.Should().Be("http");
        context.Request.Host.Value.Should().NotBe("controlpeso.example.test");
    }

    [Theory]
    [InlineData("172.18.0.5")]      // Docker bridge (172.16.0.0/12)
    [InlineData("192.168.178.20")]  // LAN where NPMplus runs (192.168.178.0/24)
    public async Task DefaultTrustedNetwork_WithForwardedFor_RewritesRemoteIpAndScheme(string remoteIp)
    {
        var context = await SendAsync(Config(), remoteIp);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ClientIp));
        context.Request.Scheme.Should().Be("https");
    }

    [Fact]
    public async Task OtherPrivateRange_IsNotTrustedByDefault()
    {
        var context = await SendAsync(Config(), "10.1.2.3");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("10.1.2.3"));
    }

    [Fact]
    public async Task ConfiguredKnownNetworks_ReplaceDefaults()
    {
        var config = Config(("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/8"));

        var trusted = await SendAsync(config, "10.1.2.3");
        var formerDefault = await SendAsync(config, "172.18.0.5");

        trusted.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ClientIp));
        formerDefault.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("172.18.0.5"));
    }

    [Fact]
    public async Task ConfiguredKnownProxies_AreTrusted()
    {
        var config = Config(("ForwardedHeaders:KnownProxies:0", "203.0.113.9"));

        var context = await SendAsync(config, "203.0.113.9");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ClientIp));
    }

    [Fact]
    public async Task CommaSeparatedValue_IsAccepted()
    {
        var config = Config(("ForwardedHeaders:KnownNetworks", "10.0.0.0/8, 192.0.2.0/24"));

        var context = await SendAsync(config, "192.0.2.44");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(ClientIp));
    }

    [Fact]
    public async Task EmptyConfiguredLists_NeverTrustEveryone()
    {
        var config = Config(("ForwardedHeaders:KnownNetworks", ""), ("ForwardedHeaders:KnownProxies", ""));

        var untrusted = await SendAsync(config, "203.0.113.9");
        var options = Options(config);

        untrusted.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.9"));
        (options.KnownIPNetworks.Count + options.KnownProxies.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Options_KeepForwardLimitOfOne()
    {
        Options(Config()).ForwardLimit.Should().Be(1);
    }

    [Theory]
    [InlineData("ForwardedHeaders:KnownNetworks:0", "not-a-cidr")]
    [InlineData("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/99")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-ip")]
    public void InvalidEntry_FailsFast(string key, string value)
    {
        var act = () => Options(Config((key, value)));

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{value}*{key[..key.LastIndexOf(':')]}*");
    }
}
