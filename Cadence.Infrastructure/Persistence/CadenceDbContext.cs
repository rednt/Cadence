using Cadence.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Infrastructure.Persistence
{
    public sealed class CadenceDbContext : DbContext
    {
        public DbSet<TaskItem> Tasks => Set<TaskItem>();
        public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
        public DbSet<Heartbeat> Heartbeats => Set<Heartbeat>();
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Cycle> Cycles => Set<Cycle>();
        public DbSet<CycleTheme> CycleThemes => Set<CycleTheme>();

        public CadenceDbContext(DbContextOptions<CadenceDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>().ToTable("Tasks");
            modelBuilder.Entity<NotificationLog>().ToTable("NotificationLogs");
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.HasOne(e => e.Area)
                    .WithMany()
                    .HasForeignKey(e => e.AreaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Heartbeat>().ToTable("Heartbeats");
            modelBuilder.Entity<Heartbeat>(entity =>
            {
                entity.HasKey(e => e.WorkerId);
            });

            // CycleTheme configuration
            modelBuilder.Entity<CycleTheme>(entity =>
            {
                entity.ToTable("CycleThemes");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Cycle configuration
            modelBuilder.Entity<Cycle>(entity =>
            {
                entity.ToTable("Cycles");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TargetLength).IsRequired();
                entity.Property(e => e.Phase).IsRequired();
                
                entity.HasOne(c => c.CycleTheme)
                    .WithMany(t => t.Cycles)
                    .HasForeignKey(c => c.CycleThemeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Areas)
                    .WithMany(a => a.Cycles)
                    .UsingEntity(j => j.ToTable("CycleAreas"));
            });

            // Area configuration
            modelBuilder.Entity<Area>(entity =>
            {
                entity.ToTable("Areas");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // CycleTheme configuration (after Area so both exist)
            modelBuilder.Entity<CycleTheme>(entity =>
            {
                entity.ToTable("CycleThemes");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Seed data
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Areas
            modelBuilder.Entity<Area>().HasData(
                new Area { Id = 1, Name = "Cadence", Color = "#007ACC", Description = "Core development work", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 2, Name = "LeetCode", Color = "#FF6B35", Description = "DSA practice", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 3, Name = "Job Hunt", Color = "#2E8B57", Description = "Applications and prep", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 4, Name = "Creative", Color = "#9B59B6", Description = "Creative projects", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 5, Name = "Music", Color = "#E74C3C", Description = "Music practice", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 6, Name = "Art", Color = "#F39C12", Description = "Drawing and art", IsArchived = false, CreatedAt = seedDate },
                new Area { Id = 7, Name = "Other", Color = "#95A5A6", Description = "Miscellaneous tasks", IsArchived = false, CreatedAt = seedDate }
            );

            // CycleThemes
            modelBuilder.Entity<CycleTheme>().HasData(
                new CycleTheme 
                { 
                    Id = 1, 
                    Name = "Deep Work", 
                    Color = "#007ACC", 
                    Icon = "🎯", 
                    Description = "Focused development and learning", 
                    DefaultAreaIds = new List<int> { 1, 2 }, 
                    DefaultLength = TimeSpan.FromHours(4), 
                    IsBuiltIn = true, 
                    CreatedAt = seedDate 
                },
                new CycleTheme 
                { 
                    Id = 2, 
                    Name = "Creative", 
                    Color = "#9B59B6", 
                    Icon = "🎨", 
                    Description = "Creative projects and exploration", 
                    DefaultAreaIds = new List<int> { 4, 5, 6 }, 
                    DefaultLength = TimeSpan.FromHours(3), 
                    IsBuiltIn = true, 
                    CreatedAt = seedDate 
                },
                new CycleTheme 
                { 
                    Id = 3, 
                    Name = "Admin", 
                    Color = "#2E8B57", 
                    Icon = "📋", 
                    Description = "Admin, job hunt, life maintenance", 
                    DefaultAreaIds = new List<int> { 3, 7 }, 
                    DefaultLength = TimeSpan.FromHours(2), 
                    IsBuiltIn = true, 
                    CreatedAt = seedDate 
                }
            );
        }
    }
}