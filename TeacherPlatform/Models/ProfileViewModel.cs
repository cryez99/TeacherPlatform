using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
