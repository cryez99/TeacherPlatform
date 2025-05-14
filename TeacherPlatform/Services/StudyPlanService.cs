using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;

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
                .Include(sp => sp.Students)
                    .ThenInclude(s => s.Tutor)
                .Include(sp => sp.Topics)
                .Include(sp => sp.Lessons)
                .Where(sp => sp.Students.Any(s => s.TutorId == tutorId))
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
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // 1. Получаем студентов и темы до создания плана
                var students = await _db.Students
                    .Where(s => studentIds.Contains(s.StudentId))
                    .ToListAsync();

                var topics = await _db.Topics
                    .Where(t => topicIds.Contains(t.TopicId))
                    .Include(t => t.SubTopics)
                    .ToListAsync();

                // 2. Создаем план
                var plan = new StudyPlan
                {
                    Title = title,
                    LessonsPerWeek = lessonsPerWeek,
                    LessonDurationMinutes = lessonDurationMinutes,
                    StartDate = startDate,
                    CreatedAt = DateTime.UtcNow,
                    Students = students,
                    Topics = topics
                };

                await _db.StudyPlans.AddAsync(plan);
                await _db.SaveChangesAsync();

                // 3. Генерируем уроки после создания плана и привязки студентов/тем
                var lessons = GenerateLessons(plan, selectedDays, lessonStartTime);

                // Добавляем привязку к учебному плану для каждого урока
                foreach (var lesson in lessons)
                {
                    lesson.StudyPlanId = plan.StudyPlanId;
                }

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
        }

        private List<Lesson> GenerateLessons(StudyPlan plan, List<DayOfWeek> selectedDays, TimeSpan startTime)
        {
            var lessons = new List<Lesson>();
            if (plan.Students == null || !plan.Students.Any() || plan.Topics == null || !plan.Topics.Any())
            {
                return lessons;
            }

            var currentDate = plan.StartDate;
            var sortedDays = selectedDays.OrderBy(d => (int)d).ToList();
            int currentDayIndex = 0;

            // Находим первую подходящую дату
            currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

            var allSubTopics = plan.Topics
                .OrderBy(t => t.TopicId)
                .SelectMany(t => t.SubTopics.OrderBy(st => st.Order))
                .ToList();

            foreach (var student in plan.Students)
            {
                foreach (var subTopic in allSubTopics)
                {
                    // Первое занятие по подтеме
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
                        Status = "Planned",
                        StudyPlanId = plan.StudyPlanId
                    });

                    // Переходим к следующему выбранному дню
                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = GetNextLessonDate(currentDate.AddDays(1), sortedDays, ref currentDayIndex);

                    // Второе занятие по подтеме
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
                        Status = "Planned",
                        StudyPlanId = plan.StudyPlanId
                    });

                    // Переходим к следующему выбранному дню для следующей подтемы
                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = GetNextLessonDate(currentDate.AddDays(1), sortedDays, ref currentDayIndex);
                }
            }

            return lessons;
        }

        private DateTime GetNextLessonDate(DateTime currentDate, List<DayOfWeek> lessonDays, ref int currentDayIndex)
        {
            // Находим следующий выбранный день недели
            while (!lessonDays.Contains(currentDate.DayOfWeek))
            {
                currentDate = currentDate.AddDays(1);
            }

            // Обновляем индекс текущего дня
            currentDayIndex = lessonDays.IndexOf(currentDate.DayOfWeek);
            return currentDate;
        }

        public async Task<StudyPlanEditModel> GetStudyPlanForEditAsync(int planId)
        {
            var plan = await _db.StudyPlans
                .Include(sp => sp.Students)
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
                    IsSelected = plan.Students.Any(ps => ps.StudentId == s.StudentId)
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
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var plan = await _db.StudyPlans
                    .Include(sp => sp.Students)
                    .Include(sp => sp.Topics)
                        .ThenInclude(t => t.SubTopics)
                    .Include(sp => sp.Lessons)
                    .FirstOrDefaultAsync(sp => sp.StudyPlanId == model.StudyPlanId);

                if (plan == null) throw new Exception("Учебный план не найден");

                // Сохраняем предыдущие темы для сравнения
                var oldTopicIds = plan.Topics.Select(t => t.TopicId).ToList();
                var newTopicIds = model.AvailableTopics
                    .Where(t => t.IsSelected)
                    .Select(t => t.TopicId)
                    .ToList();

                // Обновление названия
                plan.Title = model.Title;

                // Обновление студентов
                var selectedStudentIds = model.AvailableStudents
                    .Where(s => s.IsSelected)
                    .Select(s => s.StudentId)
                    .ToList();

                var newStudentId = selectedStudentIds.FirstOrDefault();
                var oldStudentId = plan.Students.FirstOrDefault()?.StudentId;

                // Обновляем ученика в плане
                foreach (var student in plan.Students.ToList())
                {
                    student.StudyPlanId = null;
                }

                if (newStudentId != 0)
                {
                    var newStudent = await _db.Students.FindAsync(newStudentId);
                    if (newStudent != null)
                    {
                        newStudent.StudyPlanId = plan.StudyPlanId;
                    }
                }

                // Обновление тем
                foreach (var topic in plan.Topics.ToList())
                {
                    if (!newTopicIds.Contains(topic.TopicId))
                    {
                        topic.StudyPlanId = null;
                    }
                }

                var topicsToAdd = await _db.Topics
                    .Where(t => newTopicIds.Contains(t.TopicId) &&
                               !oldTopicIds.Contains(t.TopicId))
                    .Include(t => t.SubTopics)
                    .ToListAsync();

                foreach (var topic in topicsToAdd)
                {
                    topic.StudyPlanId = plan.StudyPlanId;
                }

                await _db.SaveChangesAsync();

                // Генерируем уроки для новых тем, если они есть
                if (topicsToAdd.Any())
                {
                    // Получаем параметры плана для генерации уроков
                    var selectedDays = plan.Lessons
                        .Select(l => l.StartTime.DayOfWeek)
                        .Distinct()
                        .ToList();

                    var startTime = plan.Lessons.FirstOrDefault()?.StartTime.TimeOfDay ?? new TimeSpan(16, 0, 0);

                    // Генерируем уроки только для новых тем
                    var lessonsForNewTopics = GenerateLessonsForTopics(plan, topicsToAdd, selectedDays, startTime);

                    foreach (var lesson in lessonsForNewTopics)
                    {
                        lesson.StudentId = newStudentId; // Используем текущего ученика
                        lesson.StudyPlanId = plan.StudyPlanId;
                    }

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
        }

        private List<Lesson> GenerateLessonsForTopics(StudyPlan plan, List<Topic> newTopics, List<DayOfWeek> selectedDays, TimeSpan startTime)
        {
            var lessons = new List<Lesson>();
            if (plan.Students == null || !plan.Students.Any() || newTopics == null || !newTopics.Any())
            {
                return lessons;
            }

            // Находим последнюю дату из существующих уроков
            var lastLessonDate = plan.Lessons
                .OrderByDescending(l => l.StartTime)
                .FirstOrDefault()?.StartTime.Date ?? plan.StartDate;

            var currentDate = lastLessonDate.AddDays(1); // Начинаем со следующего дня после последнего урока
            var sortedDays = selectedDays.OrderBy(d => (int)d).ToList();
            int currentDayIndex = 0;

            // Находим первую подходящую дату
            currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

            var allNewSubTopics = newTopics
                .OrderBy(t => t.TopicId)
                .SelectMany(t => t.SubTopics.OrderBy(st => st.Order))
                .ToList();

            foreach (var student in plan.Students)
            {
                foreach (var subTopic in allNewSubTopics)
                {
                    // Первое занятие по подтеме
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
                        Status = "Planned",
                        StudyPlanId = plan.StudyPlanId
                    });

                    // Переходим к следующему дню
                    currentDayIndex = (currentDayIndex + 1) % sortedDays.Count;
                    currentDate = currentDate.AddDays(1);
                    currentDate = GetNextLessonDate(currentDate, sortedDays, ref currentDayIndex);

                    // Второе занятие по подтеме
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
                        Status = "Planned",
                        StudyPlanId = plan.StudyPlanId
                    });

                    // Переходим к следующему дню для следующей подтемы
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
                    // Основная часть документа
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Настройки страницы
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

                    // Заголовок отчета
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

                    // Таблица с планами
                    var table = new Table();

                    // Стили таблицы
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

                    // Заголовки таблицы
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

                    // Данные планов
                    foreach (var plan in studyPlans.OrderBy(p => p.StartDate))
                    {
                        var row = new TableRow();

                        // Название
                        row.AppendChild(CreateTableCell(plan.Title, columnWidths[0]));

                        // Дата начала
                        row.AppendChild(CreateTableCell(plan.StartDate.ToString("d"), columnWidths[1]));

                        // Уроков в неделю
                        row.AppendChild(CreateTableCell(plan.LessonsPerWeek.ToString(), columnWidths[2]));

                        // Длительность
                        row.AppendChild(CreateTableCell($"{plan.LessonDurationMinutes} мин.", columnWidths[3]));

                        // Ученик
                        row.AppendChild(CreateTableCell(
                            plan.Students.Any() ? plan.Students.First().FullName : "Не указан",
                            columnWidths[4]));

                        // Темы (количество)
                        row.AppendChild(CreateTableCell(plan.Topics.Count.ToString(), columnWidths[5]));

                        table.AppendChild(row);
                    }

                    body.AppendChild(table);

                    // Добавим статистику в конце
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