﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Text.RegularExpressions;

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

        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        [Column("email", TypeName = "varchar(100)")]
        [MaxLength(100)]
        [Display(Name = "Email")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email должен содержать @ и домен")]
        public string? Email { get; set; }

        [Column("phone", TypeName = "varchar(20)")]
        [MaxLength(20)]
        [Display(Name = "Телефон")]
        [RegularExpression(@"^[\d\s\+\-\(\)]+$", ErrorMessage = "Телефон может содержать только цифры и символы +-()")]
        public string? Phone { get; set; }

        [Column("class", TypeName = "varchar(20)")]
        [MaxLength(20)]
        [Display(Name = "Класс")]
        [RegularExpression(@"^([1-9]|1[0-1])$", ErrorMessage = "Класс должен быть от 1 до 11")]
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

        public ICollection<Lesson>? Lessons { get; set; }
    }
}