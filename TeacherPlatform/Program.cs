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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? throw new ArgumentNullException("POSTGRES_HOST");
            var connectionString = $"Server={dbHost};Port=5432;Database={Environment.GetEnvironmentVariable("POSTGRES_DATABASE")};" +
                                  $"Username={Environment.GetEnvironmentVariable("POSTGRES_USERNAME")};" +
                                  $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")};";

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

            //// Инициализация базы данных
            //using (var scope = app.Services.CreateScope())
            //{
            //    var db = scope.ServiceProvider.GetRequiredService<TutorDbContext>();
            //    db.Database.EnsureCreated();
            //}

            // Middleware pipeline
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