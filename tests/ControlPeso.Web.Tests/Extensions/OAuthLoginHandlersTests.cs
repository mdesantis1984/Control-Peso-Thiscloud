using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ControlPeso.Web.Tests.Extensions;

/// <summary>S7: OAuth identity mapping and graceful failure handling.</summary>
public sealed class OAuthLoginHandlersTests
{
    private static UserDto StoredUser(string email = "stored@example.com") => new()
    {
        Id = Guid.NewGuid(),
        GoogleId = "sub-1",
        Name = "Stored",
        Email = email,
        Role = UserRole.User,
        MemberSince = DateTime.UtcNow,
        Height = 170m,
        UnitSystem = UnitSystem.Metric,
        Language = "es",
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static OAuthCreatingTicketContext TicketContext(
        Mock<IUserService> userService,
        string userJson,
        params Claim[] claims)
    {
        var services = new ServiceCollection()
            .AddSingleton(userService.Object)
            .AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory, NullLoggerFactory>()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
        var scheme = new AuthenticationScheme("Google", "Google", typeof(GoogleHandler));
        var options = new GoogleOptions { ClientId = "id", ClientSecret = "secret" };
        using var tokens = JsonDocument.Parse("""{"access_token":"t","token_type":"Bearer"}""");
        return new OAuthCreatingTicketContext(
            principal,
            new AuthenticationProperties(),
            httpContext,
            scheme,
            options,
            new HttpClient(),
            OAuthTokenResponse.Success(JsonDocument.Parse(tokens.RootElement.GetRawText())),
            JsonDocument.Parse(userJson).RootElement.Clone());
    }

    private static Mock<IUserService> UserServiceReturning(UserDto user)
    {
        var mock = new Mock<IUserService>();
        mock.Setup(s => s.CreateOrUpdateFromOAuthAsync(It.IsAny<OAuthUserInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return mock;
    }

    [Theory]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":true}""", true)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":"true"}""", true)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":"TRUE"}""", true)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":false}""", false)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":"false"}""", false)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com","email_verified":"yes"}""", false)]
    [InlineData("""{"sub":"sub-1","email":"a@example.com"}""", false)]
    public async Task CreatingTicket_PassesEmailVerifiedOnlyWhenProviderAssertsTrue(string userJson, bool expected)
    {
        var users = UserServiceReturning(StoredUser());
        var context = TicketContext(users, userJson,
            new Claim(ClaimTypes.NameIdentifier, "sub-1"),
            new Claim(ClaimTypes.Email, "a@example.com"),
            new Claim(ClaimTypes.Name, "Ann"));

        await OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google");

        users.Verify(s => s.CreateOrUpdateFromOAuthAsync(
            It.Is<OAuthUserInfo>(i =>
                i.Provider == "Google" && i.ExternalId == "sub-1" && i.Email == "a@example.com" &&
                i.Name == "Ann" && i.EmailVerified == expected),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatingTicket_UsesTheStoredEmailAsTheEmailClaim()
    {
        var users = UserServiceReturning(StoredUser("stored@example.com"));
        var context = TicketContext(users, """{"sub":"sub-1","email":"victim@example.com"}""",
            new Claim(ClaimTypes.NameIdentifier, "sub-1"),
            new Claim(ClaimTypes.Email, "victim@example.com"),
            new Claim("email", "victim@example.com"));

        await OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google");

        context.Principal!.FindAll(ClaimTypes.Email).Select(c => c.Value).Should().Equal("stored@example.com");
        context.Principal.FindAll("email").Should().BeEmpty();
    }

    [Fact]
    public async Task CreatingTicket_FallsBackToEmailWhenNameIsMissing()
    {
        var users = UserServiceReturning(StoredUser());
        var context = TicketContext(users, """{"sub":"sub-1","email":"a@example.com"}""",
            new Claim(ClaimTypes.NameIdentifier, "sub-1"),
            new Claim(ClaimTypes.Email, "a@example.com"));

        await OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google");

        users.Verify(s => s.CreateOrUpdateFromOAuthAsync(
            It.Is<OAuthUserInfo>(i => i.Name == "a@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("email")]
    public async Task CreatingTicket_WithMissingClaim_FailsWithoutTouchingUsers(string missing)
    {
        var users = UserServiceReturning(StoredUser());
        var claims = new List<Claim>();
        if (missing != "subject") claims.Add(new Claim(ClaimTypes.NameIdentifier, "sub-1"));
        if (missing != "email") claims.Add(new Claim(ClaimTypes.Email, "a@example.com"));
        var context = TicketContext(users, "{}", claims.ToArray());

        var act = async () => await OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google");

        await act.Should().ThrowAsync<OAuthLoginException>();
        users.Verify(s => s.CreateOrUpdateFromOAuthAsync(It.IsAny<OAuthUserInfo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatingTicket_WhenUserServiceRejectsTheLogin_PropagatesTheFailure()
    {
        var users = new Mock<IUserService>();
        users.Setup(s => s.CreateOrUpdateFromOAuthAsync(It.IsAny<OAuthUserInfo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OAuthLoginException("email already registered"));
        var context = TicketContext(users, """{"sub":"sub-1","email":"a@example.com"}""",
            new Claim(ClaimTypes.NameIdentifier, "sub-1"),
            new Claim(ClaimTypes.Email, "a@example.com"));

        var act = async () => await OAuthLoginHandlers.OnCreatingTicketAsync(context, "Google");

        await act.Should().ThrowAsync<OAuthLoginException>().WithMessage("email already registered");
    }

    private static RemoteFailureContext FailureContext(string pathBase = "")
    {
        var services = new ServiceCollection()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.PathBase = pathBase;
        return new RemoteFailureContext(
            httpContext,
            new AuthenticationScheme("Google", "Google", typeof(GoogleHandler)),
            new GoogleOptions { ClientId = "id", ClientSecret = "secret" },
            new InvalidOperationException("boom"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/app")]
    public async Task RemoteFailure_RedirectsToLoginWithOAuthFailedAndHandlesTheResponse(string pathBase)
    {
        var context = FailureContext(pathBase);

        await OAuthLoginHandlers.OnRemoteFailureAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be($"{pathBase}/login?error=oauth_failed");
        context.Result.Should().NotBeNull();
        context.Result!.Handled.Should().BeTrue();
    }

    private static IServiceCollection AuthServices(bool withLinkedInConfig)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "google-id",
            ["Authentication:Google:ClientSecret"] = "google-secret"
        };
        if (withLinkedInConfig)
        {
            // Leftover configuration from the removed sign-in must be ignored.
            settings["Authentication:LinkedIn:ClientId"] = "linkedin-id";
            settings["Authentication:LinkedIn:ClientSecret"] = "linkedin-secret";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddOAuthAuthentication(configuration);
        return services;
    }

    [Fact]
    public async Task AddOAuthAuthentication_WiresFailureHandlingForGoogle()
    {
        await using var provider = AuthServices(withLinkedInConfig: false).BuildServiceProvider();

        var google = provider.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get("Google");

        var context = FailureContext();
        await google.Events.OnRemoteFailure(context);

        context.Response.Headers.Location.ToString().Should().Be("/login?error=oauth_failed");
        context.Result!.Handled.Should().BeTrue();
    }

    [Fact]
    public async Task AddOAuthAuthentication_RegistersOnlyCookieAndGoogle_EvenWhenLinkedInIsConfigured()
    {
        await using var provider = AuthServices(withLinkedInConfig: true).BuildServiceProvider();

        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();

        schemes.Select(s => s.Name).Should().BeEquivalentTo(["Cookies", "Google"]);
    }
}
