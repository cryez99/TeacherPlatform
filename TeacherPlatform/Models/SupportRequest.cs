using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class SupportRequest
    {
        [Required(ErrorMessage = "Обязательное поле")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        public string Message { get; set; }
    }
}
