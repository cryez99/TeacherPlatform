using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherPlatform.DB;
using TeacherPlatform.Models;
using TeacherPlatform.Services;

namespace TeacherPlatform.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly LessonService _lessonService;
        private readonly StudentService _studentService;
        private readonly TutorDbContext _context;

        public CalendarController(
            LessonService lessonService,
            StudentService studentService, TutorDbContext _db)
        {
            _lessonService = lessonService;
            _studentService = studentService;
            _context = _db;
        }

        public async Task<IActionResult> Index()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var students = await _studentService.GetStudentsByTutor(tutorId);
            var studentIds = students.Select(s => s.StudentId).ToList();

            var lessons = await _studentService.GetLessonsByStudents(studentIds);
            return View(lessons.OrderBy(l => l.StartTime).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
            return View(new Lesson
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lesson lesson)
        {
            if (string.IsNullOrEmpty(lesson.Title))
                ModelState.AddModelError(nameof(Lesson.Title), "Тема урока обязательна");

            if (lesson.StudentId == 0)
                ModelState.AddModelError(nameof(Lesson.StudentId), "Выберите ученика");

            if (lesson.StartTime >= lesson.EndTime)
            {
                ModelState.AddModelError(nameof(Lesson.EndTime), "Время окончания должно быть позже времени начала");
            }

            if (!await _lessonService.IsTimeSlotAvailable(lesson.StudentId, lesson.StartTime, lesson.EndTime))
            {
                ModelState.AddModelError(nameof(Lesson.StartTime), "Это время уже занято другим уроком");
                ModelState.AddModelError(nameof(Lesson.EndTime), ""); 
            }
            if (!ModelState.IsValid)
            {
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
                return View(lesson);
            }

            try
            {
                lesson.StartTime = lesson.StartTime.ToUniversalTime();
                lesson.EndTime = lesson.EndTime.ToUniversalTime();
                lesson.Status = "Planned";
                await _lessonService.CreateLesson(lesson);
                TempData["SuccessMessage"] = "Урок успешно создан!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при создании урока: {ex.Message}");
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
                return View(lesson);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lesson = await _lessonService.GetLesson(id);
            if (lesson == null) return NotFound();

            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lesson lesson)
        {
            if (string.IsNullOrEmpty(lesson.Title))
                ModelState.AddModelError("Title", "Тема урока обязательна");

            if (lesson.StudentId == 0)
                ModelState.AddModelError("StudentId", "Выберите ученика");

            if (!ModelState.IsValid)
            {
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
                return View(lesson);
            }
            if (lesson.StartTime >= lesson.EndTime)
            {
                ModelState.AddModelError(nameof(Lesson.EndTime), "Время окончания должно быть позже времени начала");
            }

            if (!await _lessonService.IsTimeSlotAvailable(lesson.StudentId, lesson.StartTime, lesson.EndTime))
            {
                ModelState.AddModelError(nameof(Lesson.StartTime), "Это время уже занято другим уроком");
                ModelState.AddModelError(nameof(Lesson.EndTime), "");
            }

            try
            {
                await _lessonService.UpdateLesson(id, lesson);
                TempData["SuccessMessage"] = "Урок успешно обновлен!";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex) 
            {
                ModelState.AddModelError("", ex.Message);
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
                return View(lesson);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId);
                return View(lesson);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _lessonService.DeleteLesson(id);
                TempData["SuccessMessage"] = "Урок успешно удален!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportToWord()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var studentIds = await _context.Students
                .Where(s => s.TutorId == tutorId)
                .Select(s => s.StudentId)
                .ToListAsync();

            var lessons = await _context.Lessons
                .Include(l => l.Student)
                .Where(l => studentIds.Contains(l.StudentId))
                .OrderBy(l => l.StartTime)
                .ToListAsync();

            var reportBytes = _lessonService.GenerateCalendarReport(lessons);
            var fileName = $"Календарь_занятий_{DateTime.Now:yyyy-MM-dd}.docx";

            return File(reportBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
    }
}