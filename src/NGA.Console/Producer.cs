using HtmlAgilityPack;
using JfYu.Data.Service;
using JfYu.RabbitMQ;
using JfYu.Redis.Interface;
using JfYu.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NGA.Models;
using NGA.Models.Constant;
using NGA.Models.Entity;
using NGA.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NGA.Console
{
    public class Producer : BackgroundService
    {
        private NGBToken? _token;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Producer> _logger;
        private readonly IJfYuRequest _ngaClient;
        private readonly IRedisService _redisService;
        private readonly string QUEUE_NAME = "ex_topic";
        private List<Black> _blackList = [];

        public Producer(IServiceScopeFactory scopeFactory, ILogger<Producer> logger, IJfYuRequestFactory httpClientFactory, IJfYuRequest request, IRedisService redisService)
        {
            _logger = logger;
            _ngaClient = httpClientFactory.CreateRequest(HttpClientName.NgaClientName); 
            _scopeFactory = scopeFactory;
            _redisService = redisService;
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
                var _topicService = scope.ServiceProvider.GetRequiredService<IService<Topic, DataContext>>();
                var _blackService = scope.ServiceProvider.GetRequiredService<IService<Black, DataContext>>();
                var _rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>(); 
                _token = await _redisService.GetAsync<NGBToken>("Token");
                _blackList = [.. await _blackService.GetListAsync(q => q.Status == 1)];
                int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddSeconds(-30));
                var queueTids = new List<string>();
                foreach (var fid in fids)
                {
                    try
                    {
                        _ngaClient.Url = $"https://bbs.nga.cn/thread.php?fid={fid}&page={startPage}&order_by=lastpostdesc";
                        _ngaClient.RequestEncoding = Encoding.GetEncoding("GB18030");
                        _ngaClient.RequestHeader.AcceptEncoding = "";
                        _ngaClient.Timeout = 10;
                        _ngaClient.RequestCookies.Add(new Cookie() { Name = "guestJs", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                        _ngaClient.RequestCookies.Add(new Cookie() { Name = "lastvisit", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                        _ngaClient.RequestCookies.Add(new Cookie() { Name = "ngaPassportCid", Value = _token?.Token, Domain = ".nga.cn", Path = "/" });
                        _ngaClient.RequestCookies.Add(new Cookie() { Name = "ngaPassportUid", Value = _token?.Uid, Domain = ".nga.cn", Path = "/" });
                        var html = await _ngaClient.SendAsync();

                        if (html.Contains("访客不能直接访问") || html.Contains("未登录"))
                        {
                            _logger.LogInformation("{Guid}:用户登录", guid);
                            var _loginHelper = scope.ServiceProvider.GetRequiredService<ILoginHelper>();
                            await _loginHelper.LoginAsync();
                            continue;
                        }

                        if (string.IsNullOrEmpty(html))
                            continue;

                        if (_ngaClient.StatusCode != HttpStatusCode.OK)
                            throw new Exception($"HttpStats is wrong,status:{_ngaClient.StatusCode},html:{html}");

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
                                _logger.LogInformation("{Guid}:FID:{FID},TID:{TID},{Title}", guid, fid, t.Tid, t.Title);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "获取列表出错");
                        await RandomDelayExtension.GetRandomDelayAsync();
                        continue;
                    }
                    await _rabbitMQService.SendBatchAsync(QUEUE_NAME, queueTids);
                    _logger.LogInformation("{Guid}:共发送{count}条到队列", guid, queueTids.Count);
                    queueTids = [];
                }
                await RandomDelayExtension.GetRandomDelayAsync();
                startPage = startPage > 3 ? 1 : ++startPage;
            } while (true);
        }

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
