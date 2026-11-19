using ControlPeso.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControlPeso.Infrastructure;

public partial class ControlPesoDbContext : DbContext
{
    public ControlPesoDbContext()
    {
    }

    public ControlPesoDbContext(DbContextOptions<ControlPesoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLog { get; set; }

    public virtual DbSet<UserNotifications> UserNotifications { get; set; }

    public virtual DbSet<UserPreferences> UserPreferences { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    public virtual DbSet<WeightLogs> WeightLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure connection string if not already configured (via DI)
        // This allows scaffold to work standalone while production uses appsettings.json
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=controlpeso;Username=postgres");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLog);
        });

        modelBuilder.Entity<UserNotifications>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DarkMode).HasDefaultValue(true);
            entity.Property(e => e.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.TimeZone).HasDefaultValue("America/Argentina/Buenos_Aires");

            entity.HasOne(d => d.User).WithOne(p => p.UserPreferences);
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Height).HasDefaultValue(170.0m);
            entity.Property(e => e.Language).HasDefaultValue("es");
        });

        modelBuilder.Entity<WeightLogs>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Trend).HasDefaultValue(2);

            entity.HasOne(d => d.User).WithMany(p => p.WeightLogs);
        });

        if (Database.IsNpgsql())
        {
            ConfigureNpgsql(modelBuilder);
        }

        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>
    /// PostgreSQL-specific mapping: server-side defaults, case-insensitive email and
    /// timestamp columns that keep the previous datetime2 semantics (no time zone conversion).
    /// Not applied to other providers (for example SQLite in tests) because they cannot parse these SQL expressions.
    /// </summary>
    private static void ConfigureNpgsql(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (clrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp without time zone");
                    property.SetValueConverter(UtcDateTimeConverter);
                }

                if (property.Name == "Id" && clrType == typeof(Guid))
                {
                    property.SetDefaultValueSql("gen_random_uuid()");
                }
            }
        }

        modelBuilder.Entity<AuditLog>().Property(e => e.CreatedAt).HasDefaultValueSql(UtcNow);
        modelBuilder.Entity<UserNotifications>().Property(e => e.CreatedAt).HasDefaultValueSql(UtcNow);
        modelBuilder.Entity<UserPreferences>().Property(e => e.UpdatedAt).HasDefaultValueSql(UtcNow);
        modelBuilder.Entity<Users>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNow);
            entity.Property(e => e.MemberSince).HasDefaultValueSql(UtcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(UtcNow);
            entity.Property(e => e.Email).HasColumnType("citext");
            entity.HasIndex(e => e.Email).IsUnique();
        });
        modelBuilder.Entity<WeightLogs>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(UtcNow);
            entity.HasIndex(e => new { e.UserId, e.Date, e.Time }).IsUnique();
        });
    }

    /// <summary>
    /// Npgsql refuses to write DateTime values with Kind=Utc into 'timestamp without time zone'.
    /// All timestamps in this application are UTC by convention: write them as Unspecified
    /// (same clock value, no time zone conversion) and read them back as Utc.
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => DateTime.SpecifyKind(
            value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            DateTimeKind.Unspecified),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private const string UtcNow = "(now() at time zone 'utc')";

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
