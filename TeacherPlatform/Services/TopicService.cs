using Microsoft.EntityFrameworkCore;
using TeacherPlatform.DB;
using TeacherPlatform.Models;

namespace TeacherPlatform.Services
{
    public class TopicService
    {
        private readonly TutorDbContext _db;

        public TopicService(TutorDbContext db) => _db = db;

        public async Task<List<Topic>> GetAllTopicsWithSubTopics()
        {
            return await _db.Topics
                .Include(t => t.SubTopics.OrderBy(st => st.Order))
                .OrderBy(t => t.Title)
                .ToListAsync();
        }

        public async Task<Topic> CreateTopic(string title)
        {
            var topic = new Topic { Title = title };
            _db.Topics.Add(topic);
            await _db.SaveChangesAsync();
            return topic;
        }

        public async System.Threading.Tasks.Task AddSubTopic(int topicId, string title)
        {
            var order = await _db.SubTopics
                .Where(st => st.TopicId == topicId)
                .CountAsync() + 1;

            _db.SubTopics.Add(new SubTopic
            {
                TopicId = topicId,
                Title = title,
                Order = order
            });

            await _db.SaveChangesAsync();
        }

        public async Task<bool> DeleteTopic(int topicId)
        {
            var topic = await _db.Topics
                .Include(t => t.SubTopics)
                .FirstOrDefaultAsync(t => t.TopicId == topicId);

            if (topic == null) return false;

            _db.SubTopics.RemoveRange(topic.SubTopics);

            _db.Topics.Remove(topic);

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubTopic(int subTopicId)
        {
            var subTopic = await _db.SubTopics.FindAsync(subTopicId);
            if (subTopic == null) return false;

            _db.SubTopics.Remove(subTopic);

            var remainingSubTopics = await _db.SubTopics
                .Where(st => st.TopicId == subTopic.TopicId)
                .OrderBy(st => st.Order)
                .ToListAsync();

            for (int i = 0; i < remainingSubTopics.Count; i++)
            {
                remainingSubTopics[i].Order = i + 1;
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
