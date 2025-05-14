using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class StudyPlanEditModel
    {
        public int StudyPlanId { get; set; }

        [Required(ErrorMessage = "Введите название плана")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "От 3 до 100 символов")]
        [Display(Name = "Название плана")]
        public string Title { get; set; }

        [Display(Name = "Ученики")]
        public List<StudentSelection> AvailableStudents { get; set; } = new();

        [Display(Name = "Темы")]
        public List<TopicSelection> AvailableTopics { get; set; } = new();
    }

    public class StudentSelection
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class TopicSelection
    {
        public int TopicId { get; set; }
        public string Title { get; set; }
        public bool IsSelected { get; set; }
        public List<SubTopicSelection> SubTopics { get; set; } = new();
    }

    public class SubTopicSelection
    {
        public int SubTopicId { get; set; }
        public string Title { get; set; }
    }
}
