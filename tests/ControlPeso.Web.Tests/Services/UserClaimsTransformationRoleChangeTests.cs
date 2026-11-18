using System.Security.Claims;
using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using ControlPeso.Infrastructure;
using ControlPeso.Web.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ControlPeso.Web.Tests.Services;

/// <summary>S14: after an admin changes role or status, the next request's claims reflect it immediately.</summary>
public sealed class UserClaimsTransformationRoleChangeTests : IAsyncLifetime
{
    private const string Email = "person@example.com";
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private UserClaimsTransformation _transformation = null!;
    private AdminService _admin = null!;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ControlPesoDbContext>().UseSqlite(_connection).Options;
        await using (var context = new ControlPesoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var user = new Users
            {
                Id = Guid.NewGuid(),
                GoogleId = "g-1",
                Name = "Person",
                Email = Email,
                Role = (int)UserRole.User,
                Language = "es",
                Status = (int)UserStatus.Active,
                Height = 170m,
                MemberSince = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _userId = user.Id;
        }

        var factory = new Mock<IDbContextFactory<DbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ControlPesoDbContext(options));
        var users = new UserService(factory.Object, _cache, NullLogger<UserService>.Instance);
        _admin = new AdminService(factory.Object, NullLogger<AdminService>.Instance, users);
        _transformation = new UserClaimsTransformation(users, NullLogger<UserClaimsTransformation>.Instance);
    }

    public async Task DisposeAsync()
    {
        _cache.Dispose();
        await _connection.DisposeAsync();
    }

    /// <summary>Each request carries a fresh principal built from the auth cookie.</summary>
    private async Task<ClaimsPrincipal> RequestAsync() =>
        await _transformation.TransformAsync(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "g-1"), new Claim(ClaimTypes.Email, Email)], "Cookies")));

    [Fact]
    public async Task RoleChange_IsVisibleOnTheNextRequest()
    {
        (await RequestAsync()).FindFirst(ClaimTypes.Role)!.Value.Should().Be("User");

        await _admin.UpdateUserRoleAsync(_userId, UserRole.Administrator);

        var principal = await RequestAsync();
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Administrator");
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(_userId.ToString());
    }

    [Fact]
    public async Task StatusChange_IsVisibleOnTheNextRequest()
    {
        (await RequestAsync()).FindFirst("UserStatus")!.Value.Should().Be("Active");

        await _admin.UpdateUserStatusAsync(_userId, UserStatus.Inactive);

        (await RequestAsync()).FindFirst("UserStatus")!.Value.Should().Be("Inactive");
    }
}
