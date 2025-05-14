using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    [Table("progress")]
    public class Progress
    {
        [Key]
        [Column("progress_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProgressId { get; set; }

        [ForeignKey("Student")]
        [Column("student_id")]
        public int? StudentId { get; set; }
        public Student Student { get; set; }

        [ForeignKey("Topic")]
        [Column("topic_id")]
        public int? TopicId { get; set; }
        public Topic Topic { get; set; }

        [Column("status", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string Status { get; set; } = "NotStarted";

        [Column("completion_date")]
        public DateTime? CompletionDate { get; set; }

        [Column("score")]
        public int? Score { get; set; }

        [Column("notes", TypeName = "text")]
        public string Notes { get; set; }
    }
}
