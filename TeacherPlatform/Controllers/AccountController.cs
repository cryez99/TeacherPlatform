using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherPlatform.DB;
using TeacherPlatform.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Localization;

namespace TeacherPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly TutorDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(TutorDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (TempData["ErrorMessage"] != null)
            {
                ModelState.AddModelError(string.Empty, TempData["ErrorMessage"].ToString());
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Пожалуйста, исправьте ошибки в форме");
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email не найден");
                    _logger.LogWarning($"Попытка входа с несуществующим email: {model.Email}");
                    return View(model);
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    ModelState.AddModelError(nameof(model.Password), "Неверный пароль");
                    _logger.LogWarning($"Неудачная попытка входа для пользователя {user.Email}");
                    return View(model);
                }

                await Authenticate(user);

                _logger.LogInformation($"Успешный вход пользователя {user.Email}");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Dashboard", "Tutor");
            }
            catch (SaltParseException ex)
            {
                _logger.LogError(ex, "Ошибка проверки пароля");
                ModelState.AddModelError(string.Empty, "Ошибка аутентификации. Пожалуйста, попробуйте снова.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе в систему");
                ModelState.AddModelError(string.Empty, "Произошла непредвиденная ошибка. Пожалуйста, попробуйте позже.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (TempData["ErrorMessage"] != null)
            {
                ModelState.AddModelError(string.Empty, TempData["ErrorMessage"].ToString());
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Пожалуйста, исправьте ошибки в форме");
                return View(model);
            }

            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (userExists)
                {
                    ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email уже зарегистрирован");
                    return View(model);
                }

                if (model.Password.Length < 8)
                {
                    ModelState.AddModelError(nameof(model.Password), "Пароль должен содержать минимум 8 символов");
                    return View(model);
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

                var user = new User
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Зарегистрирован новый пользователь: {user.Email}");

                TempData["SuccessMessage"] = "Регистрация прошла успешно! Теперь вы можете войти в систему.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении пользователя в базу данных");
                ModelState.AddModelError(string.Empty, "Ошибка при регистрации. Пожалуйста, попробуйте позже.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации");
                ModelState.AddModelError(string.Empty, "Произошла непредвиденная ошибка. Пожалуйста, попробуйте позже.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Пожалуйста, исправьте ошибки в форме");
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email не найден");
                    return View(model);
                }

                if (model.NewPassword.Length < 8)
                {
                    ModelState.AddModelError(nameof(model.NewPassword), "Пароль должен содержать минимум 8 символов");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Пароль изменен для пользователя {user.Email}");

                TempData["SuccessMessage"] = "Пароль успешно изменен! Теперь вы можете войти в систему.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сбросе пароля");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при изменении пароля. Пожалуйста, попробуйте позже.");
                return View(model);
            }
        }

        private async Task Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("CreatedAt", user.CreatedAt.ToString("o"))
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30)
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation($"Пользователь вышел из системы");
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе из системы");
                TempData["ErrorMessage"] = "Произошла ошибка при выходе из системы";
                return RedirectToAction("Login", "Account");
            }
        }
    }
}