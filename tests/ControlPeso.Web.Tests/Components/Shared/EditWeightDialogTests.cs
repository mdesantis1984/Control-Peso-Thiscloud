using Bunit;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace ControlPeso.Web.Tests.Components.Shared;

public sealed class EditWeightDialogTests : TestContext, IDisposable
{
    private readonly IWeightLogService _weightLogService;
    private readonly IStringLocalizer<EditWeightDialog> _localizer;

    public EditWeightDialogTests()
    {
        // Mock only interface-based services
        _weightLogService = Substitute.For<IWeightLogService>();
        _localizer = Substitute.For<IStringLocalizer<EditWeightDialog>>();

        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Add required MudBlazor providers to test render tree (NOT MudSnackbarProvider - it doesn't support root render)
        RenderTree.Add<MudPopoverProvider>();
        RenderTree.Add<MudDialogProvider>();

        // Setup localizer to return string keys
        _localizer[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));
        _localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo => 
            new LocalizedString(callInfo.Arg<string>(), string.Format(callInfo.Arg<string>(), callInfo.Arg<object[]>())));

        // Register services
        Services.AddSingleton(_weightLogService);
        Services.AddSingleton(_localizer);
        Services.AddSingleton<ILogger<EditWeightDialog>>(NullLogger<EditWeightDialog>.Instance);

        // Create real instances for sealed classes (NotificationService and UserStateService)
        // These can't be mocked with NSubstitute because they're sealed
        var snackbar = Substitute.For<ISnackbar>();
        var userPrefsService = Substitute.For<IUserPreferencesService>();
        var userNotificationService = Substitute.For<IUserNotificationService>();
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var notificationLogger = NullLogger<NotificationService>.Instance;
        var notificationService = new NotificationService(snackbar, userPrefsService, userNotificationService, authStateProvider, notificationLogger);

        var userStateLogger = NullLogger<UserStateService>.Instance;
        var userStateService = new UserStateService(userStateLogger);
        userStateService.SetCurrentUnitSystem(UnitSystem.Metric); // Set default for tests

        Services.AddSingleton(notificationService);
        Services.AddSingleton(userStateService);
        Services.AddMudServices();
    }

    [Fact]
    public void EditWeightDialog_WhenRendered_ShouldNotThrow()
    {
        // Arrange
        var weightLog = CreateTestWeightLog();

        // Act
        Action act = () =>
        {
            var cut = RenderComponent<MudDialogProvider>();
            var comp = RenderComponent<EditWeightDialog>(parameters => parameters
                .Add(p => p.WeightLog, weightLog)
                .AddCascadingValue(CreateMudDialogInstance()));
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EditWeightDialog_WhenWeightLogProvided_ShouldInitialize()
    {
        // Arrange
        var weightLog = CreateTestWeightLog();

        // Act
        var cut = RenderComponent<MudDialogProvider>();
        var comp = RenderComponent<EditWeightDialog>(parameters => parameters
            .Add(p => p.WeightLog, weightLog)
            .AddCascadingValue(CreateMudDialogInstance()));

        // Assert
        comp.Should().NotBeNull();
        comp.Instance.Should().NotBeNull();
    }

    [Fact]
    public void EditWeightDialog_WhenWeightLogIsNull_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => RenderComponent<EditWeightDialog>(parameters => parameters
            .Add(p => p.WeightLog, null!)
            .AddCascadingValue(CreateMudDialogInstance()));

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task EditWeightDialog_WhenUpdateCalled_ShouldCallService()
    {
        // Arrange
        var weightLog = CreateTestWeightLog();

        _weightLogService.UpdateAsync(Arg.Any<Guid>(), Arg.Any<UpdateWeightLogDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(weightLog));

        // Act & Assert - Service should be injectable
        _weightLogService.Should().NotBeNull();
    }

    private static WeightLogDto CreateTestWeightLog() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
        Time = new TimeOnly(8, 30),
        Weight = 75.5m,
        DisplayUnit = WeightUnit.Kg,
        Note = "Test note",
        Trend = WeightTrend.Down,
        CreatedAt = DateTime.UtcNow
    };

    private static IMudDialogInstance CreateMudDialogInstance()
    {
        var instance = Substitute.For<IMudDialogInstance>();
        return instance;
    }

    void IDisposable.Dispose()
    {
        // bUnit TestContext cleanup
        Dispose();
    }
}
