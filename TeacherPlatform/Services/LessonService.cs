using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;

namespace TeacherPlatform.Services
{
    public class LessonService
    {
        private readonly TutorDbContext _db;

        public LessonService(TutorDbContext db)
        {
            _db = db;
        }

        public async Task<List<Lesson>> GetLessonsByStudent(int studentId)
        {
            return await _db.Lessons
                .Where(l => l.StudentId == studentId)
                .Include(l => l.Student)
                .OrderBy(l => l.StartTime)
                .ToListAsync();
        }

        public async Task<Lesson?> GetLesson(int id)
        {
            return await _db.Lessons
                .Include(l => l.Student)
                .FirstOrDefaultAsync(l => l.LessonId == id);
        }

        public async Task CreateLesson(Lesson lesson)
        {
            if (lesson == null) throw new ArgumentNullException(nameof(lesson));

            if (!await IsTimeSlotAvailable(lesson.StudentId, lesson.StartTime, lesson.EndTime))
            {
                throw new InvalidOperationException("Это время уже занято другим уроком");
            }

            lesson.Status ??= "Planned";
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLesson(int lessonId, Lesson model)
        {
            var existingLesson = await _db.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (existingLesson == null)
                throw new Exception("Урок не найден");

            if (!await IsTimeSlotAvailable(model.StudentId, model.StartTime, model.EndTime, lessonId))
            {
                throw new InvalidOperationException("Это время уже занято другим уроком");
            }

            existingLesson.Title = model.Title;
            existingLesson.Description = model.Description;
            existingLesson.StartTime = model.StartTime.ToUniversalTime();
            existingLesson.EndTime = model.EndTime.ToUniversalTime();
            existingLesson.Status = model.Status ?? "Planned";
            existingLesson.StudentId = model.StudentId;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteLesson(int id)
        {
            var lesson = await _db.Lessons.FindAsync(id);

            if (lesson == null)
                throw new Exception("Урок не найден");

            _db.Lessons.Remove(lesson);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> IsTimeSlotAvailable(int studentId, DateTime startTime, DateTime endTime, int? excludeLessonId = null)
        {
            var utcStart = startTime.ToUniversalTime();
            var utcEnd = endTime.ToUniversalTime();

            if (utcStart >= utcEnd)
            {
                return false;
            }

            return !await _db.Lessons.AnyAsync(l =>
                l.LessonId != excludeLessonId &&
                l.StartTime < utcEnd &&
                l.EndTime > utcStart);
        }

        public byte[] GenerateCalendarReport(List<Lesson> lessons)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    // Основная часть документа
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    // Настройки страницы (поля)
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
                            new Text($"Календарь занятий ({DateTime.Now:dd.MM.yyyy})")
                        ));
                    title.ParagraphProperties = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new SpacingBetweenLines() { After = "200" },
                        new RunProperties(new Bold(), new FontSize() { Val = "28" })
                    );
                    body.AppendChild(title);

                    // Группировка по ученикам
                    var groupedLessons = lessons
                        .Where(l => l.Student != null)
                        .GroupBy(l => l.Student)
                        .OrderBy(g => g.Key?.FullName);

                    foreach (var group in groupedLessons)
                    {
                        // Заголовок ученика
                        var studentHeader = new Paragraph(
                            new Run(
                                new Text($"Ученик: {group.Key?.FullName}")
                            ));
                        studentHeader.ParagraphProperties = new ParagraphProperties(
                            new SpacingBetweenLines() { Before = "400", After = "200" },
                            new RunProperties(new Bold(), new FontSize() { Val = "22" })
                        );
                        body.AppendChild(studentHeader);

                        // Таблица занятий
                        var table = new Table();

                        // Стили таблицы - теперь занимает 100% ширины
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
                        string[] headers = { "Дата", "Время", "Тема", "Статус", "Заметки" };

                        // Ширина колонок (в процентах от ширины таблицы)
                        int[] columnWidths = { 15, 15, 30, 15, 25 };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = new TableCell();
                            cell.AppendChild(new TableCellProperties(
                                new TableCellWidth()
                                {
                                    Width = $"{columnWidths[i]}%",
                                    Type = TableWidthUnitValues.Pct
                                }
                            ));
                            cell.AppendChild(new Paragraph(
                                new Run(
                                    new RunProperties(new Bold()),
                                    new Text(headers[i])
                                )));
                            headerRow.AppendChild(cell);
                        }
                        table.AppendChild(headerRow);

                        // Данные занятий
                        foreach (var lesson in group.OrderBy(l => l.StartTime))
                        {
                            var row = new TableRow();

                            // Ячейки с теми же пропорциями ширины
                            row.AppendChild(CreateTableCell(lesson.StartTime.ToString("dd.MM.yyyy"), columnWidths[0]));
                            row.AppendChild(CreateTableCell($"{lesson.StartTime:t} - {lesson.EndTime:t}", columnWidths[1]));
                            row.AppendChild(CreateTableCell(lesson.Title ?? "", columnWidths[2]));
                            row.AppendChild(CreateTableCell(lesson.GetLocalizedStatus() ?? "", columnWidths[3]));
                            row.AppendChild(CreateTableCell(lesson.Description ?? "", columnWidths[4]));

                            table.AppendChild(row);
                        }

                        body.AppendChild(table);
                        body.AppendChild(new Paragraph(new Run(new Break()))); // Пустая строка между учениками
                    }
                }

                return memoryStream.ToArray();
            }
        }

        private TableCell CreateTableCell(string text, int widthPercent)
        {
            var cell = new TableCell();
            cell.AppendChild(new TableCellProperties(
                new TableCellWidth()
                {
                    Width = $"{widthPercent}%",
                    Type = TableWidthUnitValues.Pct
                }
            ));

            // Добавляем перенос слов
            var paragraph = new Paragraph(
                new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Left },
                    new SpacingBetweenLines() { Line = "240", LineRule = LineSpacingRuleValues.Auto }
                ),
                new Run(
                    new RunProperties(),
                    new Text(text)
                ));

            cell.AppendChild(paragraph);
            return cell;
        }
    }
}