using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TeacherPlatform.Services
{
    public class StudyPlanService
    {
        private readonly TutorDbContext _db;
        private readonly ILogger<StudyPlanService> _logger;

        public StudyPlanService(TutorDbContext db, ILogger<StudyPlanService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<StudyPlan>> GetStudyPlansByTutorAsync(int tutorId)
        {
            return await _db.StudyPlans
                .Include(sp => sp.Lessons)
                    .ThenInclude(l => l.Student)
                        .ThenInclude(s => s.Tutor)
                .Include(sp => sp.Topics)
                    .ThenInclude(t => t.SubTopics)
                .Where(sp => sp.Lessons.Any(l => l.Student.TutorId == tutorId))
                .OrderByDescending(sp => sp.CreatedAt)
                .ToListAsync();
        }

        public async Task<StudyPlan> CreateStudyPlanAsync(
         string title,
         int lessonsPerWeek,
         int lessonDurationMinutes,
         DateTime startDate,
         TimeSpan lessonStartTime,
         List<DayOfWeek> selectedDays,
         List<int> studentIds,
         List<int> topicIds)
        {
            var executionStrategy = _db.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    var students = await _db.Students
                        .Where(s => studentIds.Contains(s.StudentId))
                        .ToListAsync();

                    var topics = await _db.Topics
                        .Where(t => topicIds.Contains(t.TopicId))
                        .Include(t => t.SubTopics)
                        .ToListAsync();

                    var plan = new StudyPlan
                    {
                        Title = title,
                        LessonsPerWeek = lessonsPerWeek,
                        LessonDurationMinutes = lessonDurationMinutes,
                        StartDate = startDate,
                        CreatedAt = DateTime.UtcNow,
                        Topics = topics
                    };

                    await _db.StudyPlans.AddAsync(plan);
                    await _db.SaveChangesAsync();

                    var lessons = GenerateLessons(plan, selectedDays, lessonStartTime, students);

                    await _db.Lessons.AddRangeAsync(lessons);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return plan;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ошибка при создании плана");
                    throw;
                }
            });
        }
        private List<Lesson> GenerateLessons(StudyPlan plan, List<DayOfWeek> selectedDays, TimeSpan startTime, List<Student> students)
        {
            var lessons = new List<Lesson>();
            if (!students.Any() || plan.Topics == null || !plan.Topics.Any())
            {
                return lessons;
            }

            var currentDate = plan.StartDate;
            var sortedDays = selectedDays.OrderBy(d => (int)d).ToList();
            int currentDayIndex = 0;

            currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

            var allSubTopics = plan.Topics
                .OrderBy(t => t.TopicId)
                .SelectMany(t => t.SubTopics.OrderBy(st => st.Order))
                .ToList();

            foreach (var student in students)
            {
                foreach (var subTopic in allSubTopics)
                {
                    var startDateTime = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        startTime.Hours,
                        startTime.Minutes,
                        0,
                        DateTimeKind.Local);

                    lessons.Add(new Lesson
                    {
                        Title = $"{subTopic.Topic.Title}: {subTopic.Title} (Занятие 1)",
                        StartTime = startDateTime,
                        EndTime = startDateTime.AddMinutes(plan.LessonDurationMinutes),
                        StudentId = student.StudentId,
                        StudyPlanId = plan.StudyPlanId,
                        Status = "Planned"
                    });

                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = GetNextLessonDate(currentDate.AddDays(1), sortedDays, ref currentDayIndex);

                    startDateTime = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        startTime.Hours,
                        startTime.Minutes,
                        0,
                        DateTimeKind.Local);

                    lessons.Add(new Lesson
                    {
                        Title = $"{subTopic.Topic.Title}: {subTopic.Title} (Занятие 2)",
                        StartTime = startDateTime,
                        EndTime = startDateTime.AddMinutes(plan.LessonDurationMinutes),
                        StudentId = student.StudentId,
                        StudyPlanId = plan.StudyPlanId,
                        Status = "Planned"
                    });

                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = GetNextLessonDate(currentDate.AddDays(1), sortedDays, ref currentDayIndex);
                }
            }

            return lessons;
        }

        private DateTime GetNextLessonDate(DateTime currentDate, List<DayOfWeek> lessonDays, ref int currentDayIndex)
        {
            while (!lessonDays.Contains(currentDate.DayOfWeek))
            {
                currentDate = currentDate.AddDays(1);
            }

            currentDayIndex = lessonDays.IndexOf(currentDate.DayOfWeek);
            return currentDate;
        }

        public async Task<StudyPlanEditModel> GetStudyPlanForEditAsync(int planId)
        {
            var plan = await _db.StudyPlans
                .Include(sp => sp.Lessons)
                    .ThenInclude(l => l.Student)
                .Include(sp => sp.Topics)
                    .ThenInclude(t => t.SubTopics)
                .FirstOrDefaultAsync(sp => sp.StudyPlanId == planId);

            if (plan == null) return null;

            var allStudents = await _db.Students.ToListAsync();
            var allTopics = await _db.Topics.Include(t => t.SubTopics).ToListAsync();

            var model = new StudyPlanEditModel
            {
                StudyPlanId = plan.StudyPlanId,
                Title = plan.Title,
                AvailableStudents = allStudents.Select(s => new StudentSelection
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    IsSelected = plan.Lessons.Any(l => l.StudentId == s.StudentId)
                }).ToList(),
                AvailableTopics = allTopics.Select(t => new TopicSelection
                {
                    TopicId = t.TopicId,
                    Title = t.Title,
                    IsSelected = plan.Topics.Any(pt => pt.TopicId == t.TopicId),
                    SubTopics = t.SubTopics.Select(st => new SubTopicSelection
                    {
                        SubTopicId = st.SubTopicId,
                        Title = st.Title
                    }).ToList()
                }).ToList()
            };

            return model;
        }

        public async Task UpdateStudyPlanAsync(StudyPlanEditModel model)
        {
            var executionStrategy = _db.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    var plan = await _db.StudyPlans
                        .Include(sp => sp.Lessons)
                            .ThenInclude(l => l.Student)
                        .Include(sp => sp.Topics)
                            .ThenInclude(t => t.SubTopics)
                        .FirstOrDefaultAsync(sp => sp.StudyPlanId == model.StudyPlanId);

                    if (plan == null) throw new Exception("Учебный план не найден");

                    // Обновление названия
                    plan.Title = model.Title;

                    // Обновление студентов
                    var selectedStudentIds = model.AvailableStudents
                        .Where(s => s.IsSelected)
                        .Select(s => s.StudentId)
                        .ToList();

                    // Удаляем уроки для студентов, которые больше не выбраны
                    var lessonsToRemove = plan.Lessons
                        .Where(l => !selectedStudentIds.Contains(l.StudentId))
                        .ToList();

                    _db.Lessons.RemoveRange(lessonsToRemove);

                    // Обновление тем
                    var oldTopicIds = plan.Topics.Select(t => t.TopicId).ToList();
                    var newTopicIds = model.AvailableTopics
                        .Where(t => t.IsSelected)
                        .Select(t => t.TopicId)
                        .ToList();

                    // Удаляем невыбранные темы
                    foreach (var topic in plan.Topics.ToList())
                    {
                        if (!newTopicIds.Contains(topic.TopicId))
                        {
                            topic.StudyPlanId = null;
                        }
                    }

                    // Добавляем новые темы
                    var topicsToAdd = await _db.Topics
                        .Where(t => newTopicIds.Contains(t.TopicId) && !oldTopicIds.Contains(t.TopicId))
                        .Include(t => t.SubTopics)
                        .ToListAsync();

                    foreach (var topic in topicsToAdd)
                    {
                        topic.StudyPlanId = plan.StudyPlanId;
                    }

                    await _db.SaveChangesAsync();

                    // Генерируем уроки для новых тем и студентов
                    if (topicsToAdd.Any() || selectedStudentIds.Any())
                    {
                        var selectedDays = plan.Lessons
                            .Select(l => l.StartTime.DayOfWeek)
                            .Distinct()
                            .ToList();

                        var startTime = plan.Lessons.FirstOrDefault()?.StartTime.TimeOfDay ?? new TimeSpan(16, 0, 0);

                        var students = await _db.Students
                            .Where(s => selectedStudentIds.Contains(s.StudentId))
                            .ToListAsync();

                        var lessonsForNewTopics = GenerateLessonsForTopics(plan, topicsToAdd, selectedDays, startTime, students);

                        await _db.Lessons.AddRangeAsync(lessonsForNewTopics);
                        await _db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ошибка при обновлении учебного плана");
                    throw;
                }
            });
        }

        private List<Lesson> GenerateLessonsForTopics(StudyPlan plan, List<Topic> newTopics, List<DayOfWeek> selectedDays, TimeSpan startTime, List<Student> students)
        {
            var lessons = new List<Lesson>();
            if (!students.Any() || newTopics == null || !newTopics.Any())
            {
                return lessons;
            }

            var lastLessonDate = plan.Lessons
                .OrderByDescending(l => l.StartTime)
                .FirstOrDefault()?.StartTime.Date ?? plan.StartDate;

            var currentDate = lastLessonDate.AddDays(1);
            var sortedDays = selectedDays.OrderBy(d => (int)d).ToList();
            int currentDayIndex = 0;

            currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

            var allNewSubTopics = newTopics
                .OrderBy(t => t.TopicId)
                .SelectMany(t => t.SubTopics.OrderBy(st => st.Order))
                .ToList();

            foreach (var student in students)
            {
                foreach (var subTopic in allNewSubTopics)
                {
                    var startDateTime = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        startTime.Hours,
                        startTime.Minutes,
                        0,
                        DateTimeKind.Local);

                    lessons.Add(new Lesson
                    {
                        Title = $"{subTopic.Topic.Title}: {subTopic.Title} (Занятие 1)",
                        StartTime = startDateTime,
                        EndTime = startDateTime.AddMinutes(plan.LessonDurationMinutes),
                        StudentId = student.StudentId,
                        StudyPlanId = plan.StudyPlanId,
                        Status = "Planned"
                    });

                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = currentDate.AddDays(1);
                    currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

                    startDateTime = new DateTime(
                        currentDate.Year,
                        currentDate.Month,
                        currentDate.Day,
                        startTime.Hours,
                        startTime.Minutes,
                        0,
                        DateTimeKind.Local);

                    lessons.Add(new Lesson
                    {
                        Title = $"{subTopic.Topic.Title}: {subTopic.Title} (Занятие 2)",
                        StartTime = startDateTime,
                        EndTime = startDateTime.AddMinutes(plan.LessonDurationMinutes),
                        StudentId = student.StudentId,
                        StudyPlanId = plan.StudyPlanId,
                        Status = "Planned"
                    });

                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = currentDate.AddDays(1);
                }
            }

            return lessons;
        }

        public byte[] GenerateStudyPlansReport(List<StudyPlan> studyPlans)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    var sectionProps = new SectionProperties(
                        new PageMargin()
                        {
                            Top = 1000,
                            Right = 1000,
                            Bottom = 1000,
                            Left = 1000,
                            Header = 500,
                            Footer = 500
                        });
                    body.AppendChild(sectionProps);

                    var title = new Paragraph(
                        new Run(
                            new Text($"Отчет по учебным планам ({DateTime.Now:dd.MM.yyyy})")
                        ));
                    title.ParagraphProperties = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new SpacingBetweenLines() { After = "200" },
                        new RunProperties(new Bold(), new FontSize() { Val = "28" })
                    );
                    body.AppendChild(title);

                    var table = new Table();
                    var tableProperties = new TableProperties(
                        new TableWidth() { Width = "100%", Type = TableWidthUnitValues.Pct },
                        new TableBorders(
                            new TopBorder() { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder() { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder() { Val = BorderValues.Single, Size = 4 },
                            new RightBorder() { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                        ),
                        new TableLayout() { Type = TableLayoutValues.Fixed }
                    );
                    table.AppendChild(tableProperties);

                    var headerRow = new TableRow();
                    string[] headers = { "Название", "Дата начала", "Уроков/неделю", "Длительность", "Ученик", "Темы" };
                    int[] columnWidths = { 25, 15, 15, 15, 20, 10 };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = new TableCell();
                        cell.AppendChild(new TableCellProperties(
                            new TableCellWidth() { Width = $"{columnWidths[i]}%", Type = TableWidthUnitValues.Pct }
                        ));
                        cell.AppendChild(new Paragraph(
                            new Run(
                                new RunProperties(new Bold()),
                                new Text(headers[i])
                            )));
                        headerRow.AppendChild(cell);
                    }
                    table.AppendChild(headerRow);

                    foreach (var plan in studyPlans.OrderBy(p => p.StartDate))
                    {
                        var row = new TableRow();

                        row.AppendChild(CreateTableCell(plan.Title, columnWidths[0]));
                        row.AppendChild(CreateTableCell(plan.StartDate.ToString("d"), columnWidths[1]));
                        row.AppendChild(CreateTableCell(plan.LessonsPerWeek.ToString(), columnWidths[2]));
                        row.AppendChild(CreateTableCell($"{plan.LessonDurationMinutes} мин.", columnWidths[3]));

                        var studentNames = plan.Lessons
                            .Select(l => l.Student?.FullName)
                            .Distinct()
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();

                        row.AppendChild(CreateTableCell(
                            studentNames.Any() ? string.Join(", ", studentNames) : "Не указан",
                            columnWidths[4]));

                        row.AppendChild(CreateTableCell(plan.Topics.Count.ToString(), columnWidths[5]));

                        table.AppendChild(row);
                    }

                    body.AppendChild(table);

                    var stats = new Paragraph(
                        new Run(
                            new Text($"Всего планов: {studyPlans.Count}, " +
                                    $"Всего уроков: {studyPlans.Sum(p => p.Lessons.Count)}, " +
                                    $"Всего тем: {studyPlans.Sum(p => p.Topics.Count)}")
                        ));
                    stats.ParagraphProperties = new ParagraphProperties(
                        new SpacingBetweenLines() { Before = "400" },
                        new RunProperties(new Bold())
                    );
                    body.AppendChild(stats);
                }

                return memoryStream.ToArray();
            }
        }

        private TableCell CreateTableCell(string text, int widthPercent)
        {
            var cell = new TableCell();
            cell.AppendChild(new TableCellProperties(
                new TableCellWidth() { Width = $"{widthPercent}%", Type = TableWidthUnitValues.Pct }
            ));

            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Left },
                    new SpacingBetweenLines() { Line = "240", LineRule = LineSpacingRuleValues.Auto }
                ),
                new Run(new Text(text)));

            cell.AppendChild(paragraph);
            return cell;
        }
    }
}