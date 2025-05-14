using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class StudentEditModel
    {
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(20)]
        public string? Class { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? AdditionalInfo { get; set; }
    }
}
