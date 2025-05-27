using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherPlatform.Services;

namespace TeacherPlatform.Controllers
{
    public class TopicsController : Controller
    {
        private readonly TopicService _topicService;

        public TopicsController(TopicService topicService)
        {
            _topicService = topicService;
        }

        public async Task<IActionResult> Index()
        {
            var topics = await _topicService.GetAllTopicsWithSubTopics();
            return View(topics);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string title)
        {
            await _topicService.CreateTopic(title);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddSubTopic(int topicId, string title)
        {
            await _topicService.AddSubTopic(topicId, title);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTopic(int topicId)
        {
            var result = await _topicService.DeleteTopic(topicId);
            if (!result) return NotFound();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubTopic(int subTopicId)
        {
            var result = await _topicService.DeleteSubTopic(subTopicId);
            if (!result) return NotFound();
            return RedirectToAction("Index");
        }
    }
}
