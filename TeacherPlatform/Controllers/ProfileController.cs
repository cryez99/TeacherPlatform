using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeacherPlatform.Models;
using TeacherPlatform.Services;

namespace TeacherPlatform.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AuthService _auth;

        public ProfileController(AuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var model = await _auth.GetProfile(userId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Update()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return View(new { UserId = userId });
        }

        [HttpPost]
        public async Task<IActionResult> Update(string email, string fullName)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            try
            {
                var result = await _auth.UpdateProfile(userId, email, fullName);

                if (!result)
                {
                    ViewBag.ErrorMessage = "Не удалось обновить профиль";
                    var user = await _auth.GetProfile(userId);
                    return View("Index", user);
                }

                // Обновляем аутентификационные куки
                await HttpContext.SignOutAsync();
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Email, email),
                    new(ClaimTypes.Name, fullName)
                };
                await HttpContext.SignInAsync(
                    new ClaimsPrincipal(new ClaimsIdentity(claims,
                    CookieAuthenticationDefaults.AuthenticationScheme)));

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                var user = await _auth.GetProfile(userId);
                return View("Index", user);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Проверяем, что новый пароль и подтверждение совпадают
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Пароли не совпадают");
                    return View(model);
                }

                // Проверяем, что новый пароль отличается от текущего
                if (model.CurrentPassword == model.NewPassword)
                {
                    ModelState.AddModelError("NewPassword", "Новый пароль должен отличаться от текущего");
                    return View(model);
                }

                var result = await _auth.ChangePassword(userId, model.CurrentPassword, model.NewPassword);

                if (!result)
                {
                    ModelState.AddModelError("CurrentPassword", "Текущий пароль неверен");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Пароль успешно изменен";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        private async System.Threading.Tasks.Task UpdateAuthenticationCookie(int userId, string email, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim("CreatedAt", DateTime.UtcNow.ToString("o"))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}