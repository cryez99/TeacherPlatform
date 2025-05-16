using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeacherPlatform.Models;


namespace TeacherPlatform.DB
{
    public class TutorDbContext : DbContext
    {
        public TutorDbContext(DbContextOptions<TutorDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<SubTopic> SubTopics { get; set; }
        public DbSet<StudyPlan> StudyPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Topic>()
                .HasMany(t => t.SubTopics)
                .WithOne(st => st.Topic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.StudyPlan)
                .WithMany(sp => sp.Lessons)
                .HasForeignKey(l => l.StudyPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudyPlan>()
                .HasMany(sp => sp.Topics)
                .WithOne(t => t.StudyPlan)
                .HasForeignKey(t => t.StudyPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StudyPlan>()
                .HasMany(sp => sp.Lessons)
                .WithOne(l => l.StudyPlan)
                .HasForeignKey(l => l.StudyPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => v.Kind == DateTimeKind.Unspecified
                                    ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                                    : v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Local)));
                    }
                }
            }

            // Опционально: можно добавить индексы для улучшения производительности

            modelBuilder.Entity<Lesson>()
                .HasIndex(l => l.StudentId);

            modelBuilder.Entity<Lesson>()
                .HasIndex(l => l.StartTime);
        }
    }
}
