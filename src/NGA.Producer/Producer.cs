using HtmlAgilityPack;
using JfYu.Data.Service;
using JfYu.RabbitMQ;
using JfYu.Redis.Interface;
using JfYu.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NGA.Models;
using NGA.Models.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NGA.Console
{
    public class Producer : BaseTask
    {
        private List<Black> _blackList = [];
        private ILogger<Producer> _logger;
        private IJfYuRequest _jfYuRequest;
        private readonly string QUEUE_NAME = "ex_topic";
        public Producer(IServiceScopeFactory scopeFactory, IRabbitMQService rabbitMQService, ILogger<Producer> logger, IOptions<Ejiaimg> ejiaimg, IJfYuRequest request) : base(scopeFactory, ejiaimg)
        {
            _logger = logger;
            _jfYuRequest = request;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fids = JsonConvert.DeserializeObject<List<string>>(Environment.GetEnvironmentVariable("FID") ?? "");
            fids ??= new List<string>() { "-7", "472" };
            int startPage = 1;
            do
            {
                var guid = Guid.NewGuid().ToString("n");
                using var scope = _scopeFactory.CreateScope();
                var _logService = scope.ServiceProvider.GetRequiredService<IService<Log, DataContext>>();
                var _topicService = scope.ServiceProvider.GetRequiredService<IService<Topic, DataContext>>();
                var _blackService = scope.ServiceProvider.GetRequiredService<IService<Black, DataContext>>();
                var _rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
                var _redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();
                Token = await _redisService.GetAsync<NGBToken>("Token");
                _blackList = [.. await _blackService.GetListAsync(q => q.Status == 1)];
                int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddSeconds(-30));
                var queueTids = new List<string>();
                foreach (var fid in fids)
                {
                    try
                    {
                        _jfYuRequest.Url = $"https://bbs.nga.cn/thread.php?fid={fid}&page={startPage}&order_by=lastpostdesc";
                        _jfYuRequest.RequestEncoding = Encoding.GetEncoding("GB18030");
                        _jfYuRequest.Timeout = 10;
                        _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "guestJs", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                        _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "lastvisit", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                        _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "ngaPassportCid", Value = Token?.Token, Domain = ".nga.cn", Path = "/" });
                        _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "ngaPassportUid", Value = Token?.Uid, Domain = ".nga.cn", Path = "/" });
                        var html = await _jfYuRequest.SendAsync();

                        if (html.Contains("访客不能直接访问") || html.Contains("未登录"))
                        {
                            _logger.LogInformation("{Guid}:用户登录", guid);
                            await LoginAsync(_jfYuRequest.ResponseCookies);
                            continue;
                        }

                        if (string.IsNullOrEmpty(html))
                            continue;

                        if (_jfYuRequest.StatusCode != HttpStatusCode.OK)
                            throw new Exception($"HttpStats is wrong,status:{_jfYuRequest.StatusCode},html:{html}");

                        var htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(html);
                        var nodes = htmlDocument.DocumentNode.SelectNodes("//*[@class='row1 topicrow']");
                        if (nodes == null || nodes.Count <= 0)
                            continue;
                        var allnodes = nodes.Union(htmlDocument.DocumentNode.SelectNodes("//*[@class='row2 topicrow']"));
                        if (allnodes == null || !allnodes.Any())
                            continue;
                        string _thread = htmlDocument.DocumentNode.SelectSingleNode("//*[@class='nav_link']").InnerText;
                        foreach (var item in allnodes)
                        {
                            var t = new Topic
                            {
                                Replies = item.ChildNodes[1].InnerText.Trim(),
                                Uid = Regex.Match(item.ChildNodes[5].InnerHtml, "uid=.+?'").ToString().Replace("'", "").Split('=')[1],
                                LastReplyer = item.ChildNodes[7].ChildNodes[2].InnerText.Trim(),
                                PostDate = item.ChildNodes[5].ChildNodes[2].InnerText.Trim(),
                                Url = item.ChildNodes[3].ChildNodes[1].Attributes["href"].Value.Trim(),
                                Title = item.ChildNodes[3].InnerText.Trim().Replace("\n", "").Replace("\t", ""),
                                Thread = _thread,
                                Fid = fid,
                            };
                            if (t.Title == "帖子发布或回复时间超过限制")
                                continue;
                            t.Tid = t.Url.Replace("/read.php?tid=", "").Trim();
                            if (CheckBlackList(t))
                            {
                                var topic = await _topicService.GetOneAsync(q => q.Tid == t.Tid);
                                if (topic == null)
                                {
                                    await _topicService.AddAsync(t);
                                    queueTids.Add(t.Tid);
                                }
                                else
                                {
                                    //无新回复/正在采集      
                                    if (topic.ReptileNum >= int.Parse(t.Replies))
                                        continue;
                                    topic.Replies = t.Replies;
                                    topic.LastReplyer = t.LastReplyer;
                                    await _topicService.UpdateAsync(topic);
                                    queueTids.Add(t.Tid);
                                }
                                _logger.LogInformation("{Guid}:FID:{FID},TID:{TID},{Title}", guid, fid,t.Tid, t.Title);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                        var _log = new Log
                        {
                            Msg = "获取帖子列表出错",
                            Trace = ex.Message + ex.StackTrace,
                        };
                        await WriteLogAsync(_log);
                        await GetRandomDelayAsync();
                        continue;
                    }
                    await _rabbitMQService.SendBatchAsync(QUEUE_NAME, queueTids);
                    _logger.LogInformation("{Guid}:共发送{count}条到队列", guid, queueTids.Count);
                    queueTids = [];
                }
                await GetRandomDelayAsync();
                startPage = startPage > 3 ? 1 : ++startPage;
            } while (true);
        }

        // 处理黑名单        
        protected bool CheckBlackList(Topic t)
        {
            foreach (var item in _blackList)
            {
                string[] titles = item.Title.Split(',');
                foreach (var title in titles)
                {
                    if (t.Title.Contains(title))
                        return false;
                }
            }
            return true;
        }
    }
}
