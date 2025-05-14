using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Diagnostics;

namespace TeacherPlatform.Models
{
    [Table("students")]
    public class Student
    {
        [Key]
        [Column("student_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudentId { get; set; }

        [Column("full_name", TypeName = "varchar(100)")]
        [MaxLength(100)]
        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "ФИО")]
        public string FullName { get; set; }

        [EmailAddress]
        [Column("email", TypeName = "varchar(100)")]
        [MaxLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Column("phone", TypeName = "varchar(20)")]
        [MaxLength(20)]
        [Phone]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [Column("class", TypeName = "varchar(20)")]
        [MaxLength(20)]
        [Display(Name = "Класс")]
        public string Class { get; set; }

        [Column("additional_info", TypeName = "text")]
        [Display(Name = "Доп. информация")]
        public string? AdditionalInfo { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Tutor")]
        [Column("tutor_id")]
        public int TutorId { get; set; }
        
        [Required]
        public User Tutor { get; set; }

        [ForeignKey("StudyPlan")]
        [Column("study_plan_id")]
        [Display(Name = "Учебный план")]
        public int? StudyPlanId { get; set; }

        [Display(Name = "Учебный план")]
        public StudyPlan? StudyPlan { get; set; }

        // Навигационные свойства
        public ICollection<Lesson>? Lessons { get; set; }
    }
}
