using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeacherPlatform.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [Column("password_hash", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [Column("email", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [Column("full_name", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public ICollection<Student> Students { get; set; }
    }
}
