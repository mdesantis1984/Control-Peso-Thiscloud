using ControlPeso.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ControlPeso;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC07E8B49735");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLog).HasConstraintName("FK__AuditLog__UserId__73BA3083");
        });

        modelBuilder.Entity<UserNotifications>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserNoti__3214EC076607A559");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications).HasConstraintName("FK_UserNotifications_Users");
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserPref__3214EC07225CFCD0");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DarkMode).HasDefaultValue(true);
            entity.Property(e => e.NotificationsEnabled).HasDefaultValue(true);
            entity.Property(e => e.TimeZone).HasDefaultValue("America/Argentina/Buenos_Aires");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithOne(p => p.UserPreferences).HasConstraintName("FK__UserPrefe__UserI__6C190EBB");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0706812F7C");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Height).HasDefaultValue(170.0m);
            entity.Property(e => e.Language).HasDefaultValue("es");
            entity.Property(e => e.MemberSince).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<WeightLogs>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WeightLo__3214EC0734D73176");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Trend).HasDefaultValue(2);

            entity.HasOne(d => d.User).WithMany(p => p.WeightLogs).HasConstraintName("FK__WeightLog__UserI__619B8048");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
