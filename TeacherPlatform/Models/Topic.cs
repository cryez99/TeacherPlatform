using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace TeacherPlatform.Models
{
    [Table("topics")]
    public class Topic
    {
        [Key]
        [Column("topic_id")]
        public int TopicId { get; set; }

        [Required]
        [Column("title", TypeName = "varchar(100)")]
        public string Title { get; set; }

        [ForeignKey("StudyPlan")]
        [Column("study_plan_id")]
        [Display(Name = "Учебный план")]
        public int? StudyPlanId { get; set; }

        [Display(Name = "Учебный план")]
        public StudyPlan? StudyPlan { get; set; }

        public ICollection<SubTopic> SubTopics { get; set; } = new List<SubTopic>();
    }
}
