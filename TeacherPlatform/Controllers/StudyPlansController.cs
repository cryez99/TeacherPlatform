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
    public class StudyPlansController : Controller
    {
        private readonly TutorDbContext _context;
        private readonly StudyPlanService _studyPlanService;
        private readonly StudentService _studentService;
        private readonly TopicService _topicService;
        private readonly ILogger<StudyPlansController> _logger;

        public StudyPlansController(
            TutorDbContext db,
            StudyPlanService studyPlanService,
            StudentService studentService,
            TopicService topicService,
            ILogger<StudyPlansController> logger)
        {
            _studyPlanService = studyPlanService;
            _studentService = studentService;
            _topicService = topicService;
            _logger = logger;
            _context = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var plans = await _studyPlanService.GetStudyPlansByTutorAsync(tutorId);
            return View(plans);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            await ReloadViewBagData(tutorId);

            return View(new StudyPlanCreateModel
            {
                StartDate = DateTime.Today,
                LessonsPerWeek = 2,
                LessonDurationMinutes = 60,
                LessonStartTime = new TimeSpan(16, 0, 0) // Добавьте значение по умолчанию
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(StudyPlanCreateModel model)
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, что выбранные студенты принадлежат текущему преподавателю
            var validStudentIds = await _context.Students
                .Where(s => s.TutorId == tutorId)
                .Select(s => s.StudentId)
                .ToListAsync();

            if (model.SelectedStudentIds.Any(id => !validStudentIds.Contains(id)))
            {
                ModelState.AddModelError("", "Выбранные студенты не принадлежат вам");
                await ReloadViewBagData(tutorId);
                return View(model);
            }

            try
            {
                var plan = await _studyPlanService.CreateStudyPlanAsync(
                    model.Title,
                    model.LessonsPerWeek,
                    model.LessonDurationMinutes,
                    model.StartDate,
                    model.LessonStartTime,
                    model.SelectedDays,
                    model.SelectedStudentIds,
                    model.SelectedTopicIds);

                TempData["SuccessMessage"] = "План создан";
                return RedirectToAction("Details", new { id = plan.StudyPlanId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании учебного плана");
                ModelState.AddModelError("", "Произошла ошибка при создании плана");
                await ReloadViewBagData(tutorId);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var plan = await _context.StudyPlans
                .Include(sp => sp.Lessons)
                    .ThenInclude(l => l.Student)
                .Include(sp => sp.Topics)
                    .ThenInclude(t => t.SubTopics)
                .FirstOrDefaultAsync(sp => sp.StudyPlanId == id &&
                                         sp.Lessons.Any(l => l.Student.TutorId == tutorId));

            if (plan == null)
            {
                return NotFound();
            }

            plan.Lessons = plan.Lessons
                .OrderBy(l => l.StartTime)
                .ToList();

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var plan = await _context.StudyPlans
                .Include(sp => sp.Lessons)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(sp => sp.StudyPlanId == id &&
                                         sp.Lessons.Any(l => l.Student.TutorId == tutorId));

            if (plan == null)
            {
                return NotFound();
            }

            try
            {
                // Удаляем все уроки плана
                _context.Lessons.RemoveRange(plan.Lessons);

                // Отвязываем темы
                var topics = await _context.Topics
                    .Where(t => t.StudyPlanId == id)
                    .ToListAsync();

                foreach (var topic in topics)
                {
                    topic.StudyPlanId = null;
                }

                // Удаляем сам план
                _context.StudyPlans.Remove(plan);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "План успешно удалён";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении учебного плана");
                TempData["ErrorMessage"] = "Не удалось удалить учебный план";
            }

            return RedirectToAction("Index");
        }

        private async System.Threading.Tasks.Task ReloadViewBagData(int tutorId)
        {
            ViewBag.Students = await _studentService.GetStudentsByTutor(tutorId)
                ?? new List<Student>();
            ViewBag.Topics = await _topicService.GetAllTopicsWithSubTopics()
                ?? new List<Topic>();
            ViewBag.DaysOfWeek = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var plan = await _context.StudyPlans
                .FirstOrDefaultAsync(sp => sp.StudyPlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            var model = await _studyPlanService.GetStudyPlanForEditAsync(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudyPlanEditModel model)
        {
            if (!ModelState.IsValid)
            {
                // Перезагружаем данные для ViewBag если нужно
                return View(model);
            }

            // Проверяем что выбран ровно один ученик
            var selectedCount = model.AvailableStudents.Count(s => s.IsSelected);
            if (selectedCount != 1)
            {
                ModelState.AddModelError("", "Должен быть выбран ровно один ученик");
                return View(model);
            }

            try
            {
                await _studyPlanService.UpdateStudyPlanAsync(model);
                TempData["SuccessMessage"] = "Учебный план успешно обновлён";
                return RedirectToAction("Details", new { id = model.StudyPlanId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении учебного плана");
                ModelState.AddModelError("", "Произошла ошибка при обновлении плана");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToWord()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Получаем учебные планы через уроки, связанные со студентами текущего преподавателя
            var studyPlans = await _context.StudyPlans
                .Include(sp => sp.Lessons)
                    .ThenInclude(l => l.Student)
                .Include(sp => sp.Topics)
                    .ThenInclude(t => t.SubTopics)
                .Where(sp => sp.Lessons.Any(l => l.Student.TutorId == tutorId))
                .OrderByDescending(sp => sp.CreatedAt)
                .ToListAsync();

            var reportBytes = _studyPlanService.GenerateStudyPlansReport(studyPlans);
            var fileName = $"Учебные_планы_{DateTime.Now:yyyy-MM-dd}.docx";

            return File(reportBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
    }
}