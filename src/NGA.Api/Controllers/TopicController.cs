using JfYu.Data.Extension;
using JfYu.Data.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NGA.Api.Extensions;
using NGA.Api.Model.Request;
using NGA.Api.Services;
using NGA.Models;
using NGA.Models.Entity; 

namespace NGA.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TopicController : CustomController
    {
        public readonly ITopicService _topicService;
        private readonly IService<Replay, DataContext> _replayService;
        private readonly IService<User, DataContext> _userService;

        public TopicController(ITopicService topicService, IService<Replay, DataContext> replayService, IService<User, DataContext> userService)
        {
            _topicService = topicService;
            _replayService = replayService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTopics([FromQuery] QueryRequest query)
        {
            var data = await _topicService.GetListAsync(query);
            return Ok(data);
        }

        [HttpGet("{tid}")]
        public async Task<IActionResult> Topic(string tid, [FromQuery] QueryTopicRequest query)
        {

            var _topic = await _topicService.GetOneAsync(q => q.Tid == tid);
            if (_topic == null)
                return BadRequest(ErrorCode.TopicNotFound);

            var author = await _replayService.GetOneAsync(q => q.Tid == _topic.Tid && q.Sort == 0);
            var replays = (await _replayService.GetListAsync(
                q => q.Tid.Equals(tid) &&
                (q.Content.Contains(query.SearchKey) || string.IsNullOrEmpty(query.SearchKey)) &&
                (!query.OnlyImage || q.Content.Contains("<img")) &&
                (!query.OnlyAuthor || q.Uid == _topic.Uid || (q.UName == author.UName && q.UName != null)))
                ).OrderBy(q => q.Sort).AsQueryable().ToPaged(query.PageIndex);
            var quoteReplays = await _replayService.GetListAsync(q => replays.Data.Select(q => q.QuotePid).Contains(q.Pid));
            var quoteReplayUsers = (await _userService.GetListAsync(q => quoteReplays.Select(q => q.Uid).Contains(q.Uid))).Distinct().ToDictionary(q => q.Uid, q => q);
            foreach (var item in replays.Data)
            {
                var quoteHtml = "";
                if (item.QuotePid != "-1" && quoteReplayUsers != null && quoteReplayUsers.Count > 0)
                {
                    var Quote = quoteReplays.Where(q => q.Pid == item.QuotePid).FirstOrDefault();
                    if (Quote != null)
                    { 
                        quoteReplayUsers.TryGetValue(Quote.Uid, out User? u);
                        if (Quote != null)
                        {
                            quoteHtml = $"<div class=\"quote\"> by <a href= \"/nuke.php?func=ucp&amp;uid={Quote.Uid}\" " +
                           $"class=\"b\">[{(u == null ? "匿名" : u.UserName)}]</a> <span class=\"xtxt silver\" style=\"font-weight:normal\">({Quote.PostDate})</span><br /> <br />" +
                           $"{Quote.Content.Replace("{replay}", "")}</div>";
                        }
                    }
                }
                item.Content = item.Content.Replace("{replay}", quoteHtml);
            }
            Dictionary<string, User> users = [];
            users = (await _userService.GetListAsync(q => replays.Data.Select(q => q.Uid).Contains(q.Uid))).Distinct().ToDictionary(q => q.Uid, q => q);
            dynamic result = new
            {
                replays,
                users,
                topic = _topic,
            };
            return Ok(result);
        }

    }
}
