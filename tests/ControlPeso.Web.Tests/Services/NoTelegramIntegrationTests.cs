using ControlPeso.Web.Middleware;
using FluentAssertions;

namespace ControlPeso.Web.Tests.Services;

/// <summary>
/// The Telegram critical-error alert integration was removed; nothing Telegram-related may remain in the Web assembly.
/// </summary>
public sealed class NoTelegramIntegrationTests
{
    [Fact]
    public void WebAssembly_ShouldNotDeclareAnyTelegramType()
    {
        var assembly = typeof(GlobalExceptionMiddleware).Assembly;

        var offending = assembly.GetTypes()
            .Where(t => t.FullName!.Contains("Telegram", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName)
            .ToList();

        offending.Should().BeEmpty();
    }

    [Fact]
    public void GlobalExceptionMiddleware_ShouldNotDependOnAnyNotificationService()
    {
        var invoke = typeof(GlobalExceptionMiddleware).GetMethod(nameof(GlobalExceptionMiddleware.InvokeAsync))!;

        invoke.GetParameters().Select(p => p.ParameterType)
            .Should().BeEquivalentTo([typeof(Microsoft.AspNetCore.Http.HttpContext)]);
    }
}
