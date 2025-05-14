namespace TeacherPlatform.Models
{
    public class DashboardViewModel
    {
        public string TutorName { get; set; }
        public int StudentCount { get; set; }
        public int ScheduledLessonsCount { get; set; }
        public int CompletedLessonsCount { get; set; }
        public int StudyPlansCount { get; set; }
        public int TodaysLessonsCount { get; set; }
        public List<Lesson> UpcomingLessons { get; set; }
    }
}