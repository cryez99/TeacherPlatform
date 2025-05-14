using Microsoft.AspNetCore.Mvc;
using TeacherPlatform.Models;

namespace TeacherPlatform.Controllers
{
    public class SupportController : Controller
    {
        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Contact(SupportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // Здесь будет отправка email (пока просто вывод в консоль)
            Console.WriteLine($"Новое обращение в поддержку:\n" +
                             $"От: {request.Name} <{request.Email}>\n" +
                             $"Тема: {request.Subject}\n" +
                             $"Сообщение: {request.Message}");

            TempData["SuccessMessage"] = "Ваше сообщение отправлено! Мы ответим вам в ближайшее время.";
            return RedirectToAction("Contact");
        }
    }
}
