using Microsoft.EntityFrameworkCore;
using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<TimeTrackingAPI.Models.Task> Tasks { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ ДОБАВЛЯЕМ НАСТРОЙКУ ДЛЯ DECIMAL
            modelBuilder.Entity<TimeEntry>()
                .Property(t => t.Hours)
                .HasPrecision(5, 2);  // 5 цифр всего, 2 после запятой

            modelBuilder.Entity<TimeTrackingAPI.Models.Task>()
                .HasIndex(t => t.ProjectId);

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(t => t.EntryDate);

            modelBuilder.Entity<TimeEntry>()
                .HasIndex(t => t.TaskId);

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Code)
                .IsUnique();

            modelBuilder.Entity<TimeTrackingAPI.Models.Task>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimeEntry>()
                .HasOne(t => t.Task)
                .WithMany(t => t.TimeEntries)
                .HasForeignKey(t => t.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>().HasData(
                new Project { Id = 1, Name = "Разработка веб-платформы", Code = "WEB-01", IsActive = true },
                new Project { Id = 2, Name = "Мобильное приложение", Code = "MOB-01", IsActive = true },
                new Project { Id = 3, Name = "Техническая поддержка", Code = "SUP-01", IsActive = false }
            );

            modelBuilder.Entity<TimeTrackingAPI.Models.Task>().HasData(
                new TimeTrackingAPI.Models.Task { Id = 1, Name = "Разработка API", ProjectId = 1, IsActive = true },
                new TimeTrackingAPI.Models.Task { Id = 2, Name = "Дизайн интерфейса", ProjectId = 1, IsActive = true },
                new TimeTrackingAPI.Models.Task { Id = 3, Name = "Тестирование", ProjectId = 1, IsActive = true },
                new TimeTrackingAPI.Models.Task { Id = 4, Name = "Разработка iOS", ProjectId = 2, IsActive = true },
                new TimeTrackingAPI.Models.Task { Id = 5, Name = "Разработка Android", ProjectId = 2, IsActive = false },
                new TimeTrackingAPI.Models.Task { Id = 6, Name = "Консультации клиентов", ProjectId = 3, IsActive = false }
            );
        }
    }
}