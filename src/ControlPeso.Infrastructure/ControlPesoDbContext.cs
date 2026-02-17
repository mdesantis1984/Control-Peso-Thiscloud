using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ControlPeso.Domain.Entities;

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

    public virtual DbSet<UserPreferences> UserPreferences { get; set; }

    public virtual DbSet<Users> Users { get; set; }

    public virtual DbSet<WeightLogs> WeightLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=../../controlpeso.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Action, "IX_AuditLog_Action");

            entity.HasIndex(e => e.CreatedAt, "IX_AuditLog_CreatedAt").IsDescending();

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AuditLog_EntityType_EntityId");

            entity.HasIndex(e => e.UserId, "IX_AuditLog_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLog)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserPreferences_UserId").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_UserPreferences_UserId");

            entity.Property(e => e.DarkMode).HasDefaultValue(1);
            entity.Property(e => e.NotificationsEnabled).HasDefaultValue(1);
            entity.Property(e => e.TimeZone).HasDefaultValue("America/Argentina/Buenos_Aires");

            entity.HasOne(d => d.User).WithOne(p => p.UserPreferences).HasForeignKey<UserPreferences>(d => d.UserId);
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.GoogleId, "IX_Users_GoogleId").IsUnique();

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.GoogleId, "IX_Users_GoogleId");

            entity.HasIndex(e => e.Language, "IX_Users_Language");

            entity.HasIndex(e => e.Role, "IX_Users_Role");

            entity.HasIndex(e => e.Status, "IX_Users_Status");

            entity.Property(e => e.Height).HasDefaultValue(1700.0);
            entity.Property(e => e.Language).HasDefaultValue("es");
        });

        modelBuilder.Entity<WeightLogs>(entity =>
        {
            entity.HasIndex(e => e.Date, "IX_WeightLogs_Date").IsDescending();

            entity.HasIndex(e => e.UserId, "IX_WeightLogs_UserId");

            entity.HasIndex(e => new { e.UserId, e.Date }, "IX_WeightLogs_UserId_Date").IsDescending(false, true);

            entity.Property(e => e.Trend).HasDefaultValue(2);

            entity.HasOne(d => d.User).WithMany(p => p.WeightLogs).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
