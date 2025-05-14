using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeacherPlatform.Models
{
    public class StudyPlanCreateModel
    {
        [Required(ErrorMessage = "Введите название плана")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "От 3 до 100 символов")]
        [Display(Name = "Название плана")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Укажите количество занятий")]
        [Range(1, 7, ErrorMessage = "От 1 до 7 занятий")]
        [Display(Name = "Занятий в неделю")]
        public int LessonsPerWeek { get; set; } = 2;

        [Required(ErrorMessage = "Укажите длительность занятия")]
        [Range(30, 180, ErrorMessage = "От 30 до 180 минут")]
        [Display(Name = "Длительность урока (мин)")]
        public int LessonDurationMinutes { get; set; } = 60;

        [Required(ErrorMessage = "Укажите дату начала")]
        [Display(Name = "Дата начала")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Выберите дни занятий")]
        [Display(Name = "Дни занятий")]
        public List<DayOfWeek> SelectedDays { get; set; } = new();

        [Required(ErrorMessage = "Укажите время начала занятий")]
        [Display(Name = "Время начала занятий")]
        [DataType(DataType.Time)]
        public TimeSpan LessonStartTime { get; set; } = new TimeSpan(16, 0, 0); // По умолчанию 16:00

        [Required(ErrorMessage = "Выберите хотя бы одного ученика")]
        [Display(Name = "Ученики")]
        public List<int> SelectedStudentIds { get; set; } = new();

        [Required(ErrorMessage = "Выберите хотя бы одну тему")]
        [Display(Name = "Темы для изучения")]
        public List<int> SelectedTopicIds { get; set; } = new();
    }
}