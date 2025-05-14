using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TeacherPlatform.Models;

namespace TeacherPlatform.Models
{
    [Table("lessons")]
    public class Lesson
    {
        [Key]
        [Column("lesson_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LessonId { get; set; }

        [Required]
        [Column("title", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Column("description", TypeName = "text")]
        public string? Description { get; set; }

        [Required]
        [Column("start_time")]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("end_time")]
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);

        [Required]
        [Column("status", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = "Запланировано";

        [ForeignKey("Student")]
        [Column("student_id")]
        public int StudentId { get; set; }

        public Student? Student { get; set; }

        // Опциональные связи
        [ForeignKey("StudyPlan")]
        [Column("study_plan_id")]
        public int? StudyPlanId { get; set; }

        public StudyPlan? StudyPlan { get; set; }

        public string GetLocalizedStatus()
        {
            return Status switch
            {
                "Planned" => "Запланирован",
                "Completed" => "Проведён",
                "Cancelled" => "Отменён",
                _ => Status
            };
        }
    }
}