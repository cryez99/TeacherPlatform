using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeacherPlatform.Models;
using TeacherPlatform.Services;

namespace TeacherPlatform.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        public async Task<IActionResult> Index()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var students = await _studentService.GetStudentsByTutor(tutorId);
            return View(students);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Student student)
        {
            if (string.IsNullOrEmpty(student.FullName))
            {
                ModelState.AddModelError("FullName", "Имя обязательно");
                return View(student);
            }

            try
            {
                student.TutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _studentService.CreateStudent(student);

                TempData["SuccessMessage"] = "Ученик успешно добавлен";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
                return View(student);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var student = await _studentService.GetStudent(id, tutorId);

            if (student == null)
            {
                return NotFound();
            }

            return View(new StudentEditModel
            {
                StudentId = student.StudentId,
                FullName = student.FullName,
                Class = student.Class,
                Email = student.Email,
                Phone = student.Phone,
                AdditionalInfo = student.AdditionalInfo
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _studentService.UpdateStudent(id, tutorId, model);

                TempData["SuccessMessage"] = "Данные ученика обновлены";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при обновлении: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _studentService.DeleteStudent(id, tutorId);

                TempData["SuccessMessage"] = "Ученик удален";
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
            var students = await _studentService.GetStudentsByTutor(tutorId);

            var reportBytes = _studentService.GenerateStudentsReport(students);
            var fileName = $"Мои ученики_{DateTime.Now:yyyy-MM-dd}.docx";

            return File(reportBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
    }
}