using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherPlatform.Models
{
    [Table("sub_topics")]
    public class SubTopic
    {
        [Key]
        [Column("sub_topic_id")]
        public int SubTopicId { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; }

        [ForeignKey("Topic")]
        [Column("topic_id")]
        public int TopicId { get; set; }
        public Topic Topic { get; set; }

        [Column("order")]
        public int Order { get; set; }
    }
}
