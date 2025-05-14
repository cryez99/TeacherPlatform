using System.Text;
using TeacherPlatform.DB;
using TeacherPlatform.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace TeacherPlatform.Services
{
    public class AuthService
    {
        private readonly TutorDbContext _db;
        private readonly IHttpContextAccessor _httpContext;

        public AuthService(TutorDbContext db, IHttpContextAccessor httpContext)
        {
            _db = db;
            _httpContext = httpContext;
        }
        public async Task<ProfileViewModel> GetProfile(int userId)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new Exception("Пользователь не найден");

            return new ProfileViewModel
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> UpdateProfile(int userId, string email, string fullName)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            user.Email = email;
            user.FullName = fullName;

            try
            {
                return await _db.SaveChangesAsync() > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("Пользователь не найден");

            // Проверяем текущий пароль
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            // Хешируем новый пароль
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            return true;
        }
    }
}
