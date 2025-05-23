﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeacherPlatform.DB;
using TeacherPlatform.Models;

namespace TeacherPlatform.Controllers
{
    [Authorize]
    public class TutorController : Controller
    {
        private readonly TutorDbContext _context;

        public TutorController(TutorDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var tutorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var todayUtc = DateTime.UtcNow.Date;
            var nowUtc = DateTime.UtcNow;

            var studentIds = _context.Students
                .Where(s => s.TutorId == tutorId)
                .Select(s => s.StudentId)
                .ToList();

            var model = new DashboardViewModel
            {
                TutorName = _context.Users
                    .Where(u => u.UserId == tutorId)
                    .Select(u => u.FullName)
                    .FirstOrDefault() ?? "Преподаватель",

                StudentCount = studentIds.Count,

                ScheduledLessonsCount = _context.Lessons
                    .Count(l => studentIds.Contains(l.StudentId) && l.Status == "Planned"),

                CompletedLessonsCount = _context.Lessons
                    .Count(l => studentIds.Contains(l.StudentId) && l.Status == "Completed"),

                StudyPlansCount = _context.StudyPlans
                    .Count(p => p.Lessons.Any(l => l.Student.TutorId == tutorId)),

                TodaysLessonsCount = _context.Lessons
                    .Count(l => l.StartTime >= todayUtc &&
                              l.StartTime < todayUtc.AddDays(1) &&
                              studentIds.Contains(l.StudentId)),

                UpcomingLessons = _context.Lessons
                    .Include(l => l.Student)
                    .Where(l => l.StartTime >= nowUtc &&
                              studentIds.Contains(l.StudentId))
                    .OrderBy(l => l.StartTime)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }
    }
}