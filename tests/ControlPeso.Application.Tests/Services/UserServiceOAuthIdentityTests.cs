using ControlPeso.Application.DTOs;
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
/// S7: OAuth users are matched by provider + external id; an existing account is linked by email
/// only when the provider asserts email_verified=true.
/// </summary>
public sealed class UserServiceOAuthIdentityTests : IDisposable
{
    private readonly DbContextOptions<DbContext> _options =
        new DbContextOptionsBuilder<DbContext>().UseInMemoryDatabase($"OAuthIdentity_{Guid.NewGuid()}").Options;

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly UserService _service;

    public UserServiceOAuthIdentityTests()
    {
        var factory = new Mock<IDbContextFactory<DbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TestDbContext(_options));
        _service = new UserService(factory.Object, _cache, NullLogger<UserService>.Instance);
    }

    public void Dispose()
    {
        using var context = new TestDbContext(_options);
        context.Database.EnsureDeleted();
        _cache.Dispose();
    }

    private async Task<Guid> SeedAsync(string email, string? googleId = null, string? linkedInId = null, string name = "Seeded")
    {
        await using var context = new TestDbContext(_options);
        var user = new Users
        {
            Id = Guid.NewGuid(),
            GoogleId = googleId,
            LinkedInId = linkedInId,
            Name = name,
            Email = email,
            Role = (int)UserRole.Administrator,
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

    private async Task<List<Users>> AllUsersAsync()
    {
        await using var context = new TestDbContext(_options);
        return await context.Set<Users>().AsNoTracking().ToListAsync();
    }

    private static OAuthUserInfo Info(string provider, string externalId, string email, bool verified, string name = "Name") =>
        new()
        {
            Provider = provider,
            ExternalId = externalId,
            Email = email,
            EmailVerified = verified,
            Name = name
        };

    [Fact]
    public async Task ExistingProviderId_WithUnverifiedEmail_KeepsStoredEmailButUpdatesName()
    {
        var id = await SeedAsync("owner@example.com", googleId: "g-1");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-1", "victim@example.com", verified: false, name: "New Name"));

        result.Id.Should().Be(id);
        result.Email.Should().Be("owner@example.com");
        result.Name.Should().Be("New Name");
        (await AllUsersAsync()).Should().ContainSingle().Which.Email.Should().Be("owner@example.com");
    }

    [Fact]
    public async Task ExistingProviderId_WithVerifiedEmail_UpdatesEmail()
    {
        var id = await SeedAsync("old@example.com", googleId: "g-1");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-1", "new@example.com", verified: true));

        result.Id.Should().Be(id);
        result.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task ExistingProviderId_WithVerifiedEmailOwnedByAnotherUser_KeepsStoredEmail()
    {
        var id = await SeedAsync("mine@example.com", googleId: "g-1");
        await SeedAsync("taken@example.com", googleId: "g-2");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-1", "TAKEN@example.com", verified: true));

        result.Id.Should().Be(id);
        result.Email.Should().Be("mine@example.com");
        (await AllUsersAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task UnknownGoogleId_WithVerifiedEmailOfExistingUser_LinksThatUser()
    {
        var id = await SeedAsync("person@example.com");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-new", "Person@Example.com", verified: true));

        result.Id.Should().Be(id);
        result.Role.Should().Be(UserRole.Administrator);
        var users = await AllUsersAsync();
        users.Should().ContainSingle();
        users[0].GoogleId.Should().Be("g-new");
        users[0].LinkedInId.Should().BeNull();
    }

    [Fact]
    public async Task UnknownLinkedInId_WithVerifiedEmailOfExistingUser_LinksThatUser()
    {
        var id = await SeedAsync("person@example.com", googleId: "g-1");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("LinkedIn", "li-new", "person@example.com", verified: true));

        result.Id.Should().Be(id);
        var users = await AllUsersAsync();
        users.Should().ContainSingle();
        users[0].LinkedInId.Should().Be("li-new");
        users[0].GoogleId.Should().Be("g-1");
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("LinkedIn")]
    public async Task UnknownProviderId_WithUnverifiedEmailOfExistingUser_FailsAndLeavesUserUntouched(string provider)
    {
        await SeedAsync("victim@example.com");

        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(Info(provider, "attacker-id", "victim@example.com", verified: false));

        await act.Should().ThrowAsync<OAuthLoginException>();
        var users = await AllUsersAsync();
        users.Should().ContainSingle();
        users[0].GoogleId.Should().BeNull();
        users[0].LinkedInId.Should().BeNull();
    }

    [Fact]
    public async Task UnknownProviderId_WithVerifiedEmailOfUserLinkedToAnotherGoogleAccount_Fails()
    {
        await SeedAsync("person@example.com", googleId: "g-original");

        var act = async () => await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-other", "person@example.com", verified: true));

        await act.Should().ThrowAsync<OAuthLoginException>();
        (await AllUsersAsync()).Should().ContainSingle().Which.GoogleId.Should().Be("g-original");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnknownProviderId_WithUnusedEmail_CreatesNewUser(bool verified)
    {
        await SeedAsync("someone@example.com", googleId: "g-1");

        var result = await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-2", "fresh@example.com", verified));

        result.Email.Should().Be("fresh@example.com");
        result.Role.Should().Be(UserRole.User);
        (await AllUsersAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task LinkingByEmail_EvictsCachedUser()
    {
        var id = await SeedAsync("person@example.com");
        (await _service.GetByEmailAsync("person@example.com"))!.GoogleId.Should().BeEmpty();

        await _service.CreateOrUpdateFromOAuthAsync(Info("Google", "g-new", "person@example.com", verified: true));

        (await _service.GetByEmailAsync("person@example.com"))!.GoogleId.Should().Be("g-new");
        (await _service.GetByIdAsync(id))!.GoogleId.Should().Be("g-new");
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }
}
