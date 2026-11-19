using ControlPeso.Web.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ControlPeso.Web.Tests.Middleware;

/// <summary>
/// Unhandled exceptions are logged and redirected to the friendly error page; no external alerts are sent.
/// </summary>
public sealed class GlobalExceptionMiddlewareTests
{
    private sealed class ListLogger : ILogger<GlobalExceptionMiddleware>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, exception));
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldLogErrorAndRedirectToErrorPageWithTraceId()
    {
        var logger = new ListLogger();
        var boom = new InvalidOperationException("boom");
        var middleware = new GlobalExceptionMiddleware(
            _ => throw boom,
            logger,
            Substitute.For<IHostEnvironment>());
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status302Found);
        context.Response.Headers.Location.ToString().Should().Be("/error?traceId=trace-123");
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && ReferenceEquals(e.Exception, boom));
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldPassThroughWithoutRedirect()
    {
        var logger = new ListLogger();
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.CompletedTask,
            logger,
            Substitute.For<IHostEnvironment>());
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Headers.Location.ToString().Should().BeEmpty();
        logger.Entries.Should().BeEmpty();
    }
}
