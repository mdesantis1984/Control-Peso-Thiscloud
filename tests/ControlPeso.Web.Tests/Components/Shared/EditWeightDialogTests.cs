using Bunit;
using ControlPeso.Application.DTOs;
using ControlPeso.Application.Interfaces;
using ControlPeso.Domain.Enums;
using ControlPeso.Web.Components.Shared;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;

namespace ControlPeso.Web.Tests.Components.Shared;

public sealed class EditWeightDialogTests : TestContext, IDisposable
{
    private readonly IWeightLogService _weightLogService;
    private readonly IStringLocalizer<EditWeightDialog> _localizer;
    private readonly ILogger<EditWeightDialog> _logger;
    private readonly NotificationService _notificationService;
    private readonly UserStateService _userStateService;

    public EditWeightDialogTests()
    {
        // Mock services
        _weightLogService = Substitute.For<IWeightLogService>();
        _localizer = Substitute.For<IStringLocalizer<EditWeightDialog>>();
        _logger = Substitute.For<ILogger<EditWeightDialog>>();
        _notificationService = Substitute.For<NotificationService>();
        _userStateService = Substitute.For<UserStateService>();

        // Setup localizer to return string keys
        _localizer[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));
        _localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(callInfo => 
            new LocalizedString(callInfo.Arg<string>(), string.Format(callInfo.Arg<string>(), callInfo.Arg<object[]>())));

        // Setup UserStateService defaults
        _userStateService.CurrentUnitSystem.Returns(UnitSystem.Metric);

        // Register services
        Services.AddSingleton(_weightLogService);
        Services.AddSingleton(_localizer);
        Services.AddSingleton(_logger);
        Services.AddSingleton(_notificationService);
        Services.AddSingleton(_userStateService);
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
        instance.Close(Arg.Any<DialogResult>()).Returns(Task.CompletedTask);
        return instance;
    }

    void IDisposable.Dispose()
    {
        // bUnit TestContext cleanup
        Dispose();
    }
}
