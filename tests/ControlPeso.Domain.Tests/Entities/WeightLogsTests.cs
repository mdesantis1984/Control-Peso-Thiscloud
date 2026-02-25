using ControlPeso.Domain.Entities;
using FluentAssertions;

namespace ControlPeso.Domain.Tests.Entities;

public class WeightLogsTests
{
    [Fact]
    public void WeightLogs_ShouldSetAndGetId()
    {
        var log = new WeightLogs();
        const string id = "log-123";

        log.Id = id;

        log.Id.Should().Be(id);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetUserId()
    {
        var log = new WeightLogs();
        const string userId = "user-123";

        log.UserId = userId;

        log.UserId.Should().Be(userId);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetDate()
    {
        var log = new WeightLogs();
        const string date = "2024-01-15";

        log.Date = date;

        log.Date.Should().Be(date);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetTime()
    {
        var log = new WeightLogs();
        const string time = "14:30";

        log.Time = time;

        log.Time.Should().Be(time);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetWeight()
    {
        var log = new WeightLogs();
        const double weight = 75.5;

        log.Weight = weight;

        log.Weight.Should().Be(weight);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetDisplayUnit()
    {
        var log = new WeightLogs();
        const int displayUnit = 0;

        log.DisplayUnit = displayUnit;

        log.DisplayUnit.Should().Be(displayUnit);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetNote()
    {
        var log = new WeightLogs();
        const string note = "Feeling good today";

        log.Note = note;

        log.Note.Should().Be(note);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetTrend()
    {
        var log = new WeightLogs();
        const int trend = 1;

        log.Trend = trend;

        log.Trend.Should().Be(trend);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetCreatedAt()
    {
        var log = new WeightLogs();
        const string createdAt = "2024-01-15T14:30:00Z";

        log.CreatedAt = createdAt;

        log.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void WeightLogs_ShouldSetAndGetUser()
    {
        var log = new WeightLogs();
        var user = new Users();

        log.User = user;

        log.User.Should().Be(user);
    }
}
