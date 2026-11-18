using ControlPeso.Application.Services;
using ControlPeso.Domain.Entities;
using ControlPeso.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ControlPeso.Application.Tests.Services;

/// <summary>
/// S14: role and status changes made by an admin must be visible on the next read of the cached user,
/// by id and by email (UserClaimsTransformation reads by email on every request).
/// </summary>
public sealed class AdminServiceCacheInvalidationTests : IDisposable
{
    private readonly DbContextOptions<DbContext> _options =
        new DbContextOptionsBuilder<DbContext>().UseInMemoryDatabase($"AdminCache_{Guid.NewGuid()}").Options;

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly UserService _users;
    private readonly AdminService _admin;

    public AdminServiceCacheInvalidationTests()
    {
        var factory = new Mock<IDbContextFactory<DbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TestDbContext(_options));
        _users = new UserService(factory.Object, _cache, NullLogger<UserService>.Instance);
        _admin = new AdminService(factory.Object, NullLogger<AdminService>.Instance, _users);
    }

    public void Dispose()
    {
        using var context = new TestDbContext(_options);
        context.Database.EnsureDeleted();
        _cache.Dispose();
    }

    private async Task<Guid> SeedUserAsync(string email = "Person@Example.com")
    {
        await using var context = new TestDbContext(_options);
        var user = new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = "g-1",
            Name = "Person",
            Email = email,
            Role = (int)UserRole.User,
            Language = "es",
            Status = (int)UserStatus.Active,
            Height = 170m,
            MemberSince = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Set<Users>().Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task UpdateUserRole_EvictsCachedUserByIdAndByEmail()
    {
        var id = await SeedUserAsync();
        (await _users.GetByEmailAsync("Person@Example.com"))!.Role.Should().Be(UserRole.User);   // fills the email cache
        (await _users.GetByIdAsync(id))!.Role.Should().Be(UserRole.User);                          // fills the id cache

        await _admin.UpdateUserRoleAsync(id, UserRole.Administrator);

        (await _users.GetByEmailAsync("Person@Example.com"))!.Role.Should().Be(UserRole.Administrator);
        (await _users.GetByIdAsync(id))!.Role.Should().Be(UserRole.Administrator);
    }

    [Fact]
    public async Task UpdateUserStatus_EvictsCachedUserByIdAndByEmail()
    {
        var id = await SeedUserAsync();
        await _users.GetByEmailAsync("Person@Example.com");
        await _users.GetByIdAsync(id);

        await _admin.UpdateUserStatusAsync(id, UserStatus.Inactive);

        (await _users.GetByEmailAsync("Person@Example.com"))!.Status.Should().Be(UserStatus.Inactive);
        (await _users.GetByIdAsync(id))!.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public async Task UpdateUserRole_ToTheSameRole_LeavesTheCacheUntouched()
    {
        var id = await SeedUserAsync();
        var cached = await _users.GetByIdAsync(id);

        await _admin.UpdateUserRoleAsync(id, UserRole.User);

        (await _users.GetByIdAsync(id)).Should().BeSameAs(cached);
    }

    [Fact]
    public async Task UpdateUserRole_ForUnknownUser_ThrowsAndEvictsNothing()
    {
        var id = await SeedUserAsync();
        var cached = await _users.GetByIdAsync(id);

        var act = async () => await _admin.UpdateUserRoleAsync(Guid.NewGuid(), UserRole.Administrator);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await _users.GetByIdAsync(id)).Should().BeSameAs(cached);
    }

    [Fact]
    public async Task UpdateUserRole_AskUserServiceToInvalidateByIdAndStoredEmail()
    {
        var id = await SeedUserAsync("Stored@Example.com");
        var users = new Mock<ControlPeso.Application.Interfaces.IUserService>();
        var factory = new Mock<IDbContextFactory<DbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TestDbContext(_options));
        var admin = new AdminService(factory.Object, NullLogger<AdminService>.Instance, users.Object);

        await admin.UpdateUserRoleAsync(id, UserRole.Administrator);
        await admin.UpdateUserStatusAsync(id, UserStatus.Pending);

        users.Verify(u => u.InvalidateCache(id, "Stored@Example.com"), Times.Exactly(2));
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>().HasKey(e => e.Id);
            modelBuilder.Entity<AuditLog>().HasKey(e => e.Id);
        }
    }
}
