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

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TutorDbContext")
                ?? builder.Configuration.GetConnectionString("TutorDbContext");

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

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TutorDbContext>();
                await db.Database.MigrateAsync();
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