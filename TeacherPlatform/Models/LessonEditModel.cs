using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class LessonEditModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Максимум 100 символов")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Время начала обязательно")]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Время окончания обязательно")]
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);

        public string? Status { get; set; }

        [Required(ErrorMessage = "Ученик обязателен")]
        public int StudentId { get; set; }
    }
}
