using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;
using TeacherPlatform.Services;

namespace TeacherPlatform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Получаем строку подключения для Render
            var renderDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            string connectionString;
            if (!string.IsNullOrEmpty(renderDbUrl))
            {
                // Парсинг URL формата Render (postgres://user:pass@host:port/db)
                var uri = new Uri(renderDbUrl);
                var userInfo = uri.UserInfo.Split(':');

                connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
                                 $"User Id={userInfo[0]};Password={userInfo[1]};";
            }
            else
            {
                // Для локальной разработки
                connectionString = builder.Configuration.GetConnectionString("TutorDbContext");
            }

            builder.Services.AddDbContext<TutorDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure()));

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "TutorPlatformAuth";
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                });

            builder.Services.AddScoped<AuthService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<StudentService>();
            builder.Services.AddScoped<LessonService>();
            builder.Services.AddScoped<TopicService>();
            builder.Services.AddScoped<StudyPlanService>();

            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Применяем миграции с обработкой ошибок
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<TutorDbContext>();
                    logger.LogInformation("Applying migrations...");
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error applying migrations");
                    throw; // Прерываем запуск при ошибке миграций
                }
            }
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Tutor}/{action=Dashboard}/{id?}");
            });

            app.Run();
        }
    }
}