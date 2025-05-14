using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;

namespace TeacherPlatform.Services
{
    public class StudentService
    {
        private readonly TutorDbContext _db;

        public StudentService(TutorDbContext db)
        {
            _db = db;
        }

        public async Task<List<Student>> GetStudentsByTutor(int tutorId)
        {
            return await _db.Students
                .Where(s => s.TutorId == tutorId)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

        public async Task<Student?> GetStudent(int id, int tutorId)
        {
            return await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == id && s.TutorId == tutorId);
        }

        public async Task<List<Lesson>> GetLessonsByStudents(List<int> studentIds)
        {
            return await _db.Lessons
                .Where(l => studentIds.Contains(l.StudentId))
                .Include(l => l.Student)
                .OrderBy(l => l.StartTime)
                .ToListAsync();
        }

        public async System.Threading.Tasks.Task CreateStudent(Student student)
        {
            if (student == null || string.IsNullOrEmpty(student.FullName))
                throw new ArgumentException("Неверные данные ученика");

            student.CreatedAt = DateTime.UtcNow;
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task UpdateStudent(int studentId, int tutorId, StudentEditModel model)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.TutorId == tutorId);

            if (student == null)
                throw new Exception("Ученик не найден или нет прав доступа");

            student.FullName = model.FullName;
            student.Class = model.Class;
            student.Email = model.Email;
            student.Phone = model.Phone;
            student.AdditionalInfo = model.AdditionalInfo;

            await _db.SaveChangesAsync();
        }

        public async System.Threading.Tasks.Task DeleteStudent(int id, int tutorId)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.StudentId == id && s.TutorId == tutorId);

            if (student != null)
            {
                _db.Students.Remove(student);
                await _db.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Ученик не найден или нет прав доступа");
            }
        }

        public byte[] GenerateStudentsReport(List<Student> students)
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
                            Top = 1000,  // 1 см сверху
                            Right = 1000, // 1 см справа
                            Bottom = 1000, // 1 см снизу
                            Left = 1000,  // 1 см слева
                            Header = 500,
                            Footer = 500
                        });
                    body.AppendChild(sectionProps);

                    // Заголовок документа
                    var title = new Paragraph(
                        new Run(
                            new Text($"Отчет: Мои ученики ({DateTime.Now:dd.MM.yyyy})")
                        ));
                    title.ParagraphProperties = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new SpacingBetweenLines() { After = "200" },
                        new RunProperties(new Bold(), new FontSize() { Val = "28" })
                    );
                    body.AppendChild(title);

                    // Таблица с данными
                    var table = new Table();

                    // Стили таблицы - занимает 100% ширины
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
                    string[] headers = { "ФИО", "Класс", "Email", "Телефон", "Доп. информация" };

                    // Ширина колонок (в процентах от ширины таблицы)
                    int[] columnWidths = { 30, 15, 20, 15, 20 };

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

                    // Данные учеников
                    foreach (var student in students)
                    {
                        var row = new TableRow();

                        // Ячейки с теми же пропорциями ширины
                        row.AppendChild(CreateTableCell(student.FullName ?? "", columnWidths[0]));
                        row.AppendChild(CreateTableCell(student.Class ?? "", columnWidths[1]));
                        row.AppendChild(CreateTableCell(student.Email ?? "", columnWidths[2]));
                        row.AppendChild(CreateTableCell(student.Phone ?? "", columnWidths[3]));
                        row.AppendChild(CreateTableCell(student.AdditionalInfo ?? "", columnWidths[4]));

                        table.AppendChild(row);
                    }

                    body.AppendChild(table);
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