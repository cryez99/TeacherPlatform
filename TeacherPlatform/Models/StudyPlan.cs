using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace TeacherPlatform.Models
{
    [Table("study_plans")]
    public class StudyPlan
    {
        [Key]
        [Column("study_plan_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudyPlanId { get; set; }

        [Column("title", TypeName = "varchar(100)")]
        [MaxLength(100)]
        [Required(ErrorMessage = "Название плана обязательно")]
        [Display(Name = "Название плана")]
        public string Title { get; set; }

        [Column("lessons_per_week")]
        [Required(ErrorMessage = "Количество занятий обязательно")]
        [Range(1, 7, ErrorMessage = "Должно быть от 1 до 7 занятий в неделю")]
        [Display(Name = "Занятий в неделю")]
        public int LessonsPerWeek { get; set; } = 2;

        [Column("lesson_duration_minutes")]
        [Required(ErrorMessage = "Длительность занятия обязательна")]
        [Range(30, 180, ErrorMessage = "Длительность должна быть от 30 до 180 минут")]
        [Display(Name = "Длительность урока (мин)")]
        public int LessonDurationMinutes { get; set; } = 60;

        [Column("start_date")]
        [Required(ErrorMessage = "Дата начала обязательна")]
        [Display(Name = "Дата начала")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Column("created_at")]
        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ученики")]
        public ICollection<Student> Students { get; set; } = new List<Student>();

        [Display(Name = "Темы")]
        public ICollection<Topic> Topics { get; set; } = new List<Topic>();

        [Display(Name = "Занятия")]
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

        public override string ToString()
        {
            return $"{Title} (с {StartDate:dd.MM.yyyy})";
        }
    }
}