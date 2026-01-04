using JfYu.Data.Extension;
using JfYu.Data.Service;
using Microsoft.AspNetCore.Mvc;
using NGA.Models;
using NGA.Models.Entity;
using NGA.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NGA.UI.Controllers
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ITopicService _topicService;

        private readonly IService<Replay, DataContext> _replayService;

        private readonly IService<User, DataContext> _userService;

        public HomeController(ITopicService topicService, IService<Replay, DataContext> replayService, IService<User, DataContext> userService)
        {
            _topicService = topicService;
            _replayService = replayService;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Route("Topic/{tid}")]
        public async Task<IActionResult> Topic(string tid)
        {
            var _topic = await _topicService.GetOneAsync(q => q.Tid == tid);
            if (_topic == null)
            {
                ViewData["Title"] = "主题被删除";
                return View("Del");
            }
            ViewData["Title"] = _topic.Title;
            ViewData["Tid"] = _topic.Tid;
            return View();
        }

        [HttpGet("api/Topic/{tid}")]
        public async Task<JsonResult> GetTopic(string tid, int pageIndex = 1, string key = "", bool onlyAuthor = false, bool onlyImage = false)
        {

            var _topic = await _topicService.GetOneAsync(q => q.Tid == tid);
            if (_topic == null)
            {
                return Json("");
            }
            var author = await _replayService.GetOneAsync(q => q.Tid == _topic.Tid && q.Sort == 0);
            var replays = (await _replayService.GetListAsync(
                q => q.Tid.Equals(tid) &&
                (q.Content.Contains(key) || string.IsNullOrEmpty(key)) &&
                (!onlyImage || q.Content.Contains("<img")) &&
                (!onlyAuthor || q.Uid == _topic.Uid || (q.UName == author.UName && q.UName != null)))
                ).OrderBy(q => q.Sort).AsQueryable().ToPaged(pageIndex);
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
                        User u;
                        quoteReplayUsers.TryGetValue(Quote.Uid, out u);
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
            Dictionary<string, User> users = new Dictionary<string, User>();
            users = (await _userService.GetListAsync(q => replays.Data.Select(q => q.Uid).Contains(q.Uid))).Distinct().ToDictionary(q => q.Uid, q => q);
            dynamic result = new
            {
                replays,
                users,
                topic = _topic,
            };
            return Json(result);
        }

        [HttpGet("api/Topic/List")]
        public async Task<JsonResult> GetList(string key, int pageIndex = 1)
        {

            var result = await _topicService.GetLatestTopicsWithReplies(key, pageIndex);
            foreach (var item in result.Data)
            {
                item.Title = item.Title.Replace("新闻", "XW").Replace("讨论", "TL").Replace("转帖", "ZT");
            }
            return Json(result);

        }
    }
}
