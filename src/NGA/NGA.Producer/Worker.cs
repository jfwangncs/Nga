using HtmlAgilityPack;
using jfYu.Core.Data.Service;
using jfYu.Core.jfYuRequest;
using jfYu.Core.RabbitMQ;
using jfYu.Core.Redis.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NGA.Base;
using NGA.Models;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NGA.Producer
{
    public class Worker : BaseTask
    {
        private List<Black> _blackList = [];
        private ILogger<Worker> _logger;
        private ILogger<JfYuHttpRequest>? _logger1;
        public Worker(IServiceScopeFactory scopeFactory, IRabbitMQService rabbitMQService, ILogger<Worker> logger, ILogger<JfYuHttpRequest> logger1) : base(scopeFactory, rabbitMQService, logger1)
        {
            _logger = logger;
            _logger1 = null;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rabbitMQService.QueueBind("topic", "ex_topic", ExchangeType.Direct);

            var fid = Environment.GetEnvironmentVariable("PRODUCER_PID")?.ToString().Trim();
            if (string.IsNullOrEmpty(fid))
                fid = "-7";
            int i = 1; //第一页开始 
            _logger.LogInformation($"fid:{fid}开始");
            do
            {
                using var scope = _scopeFactory.CreateScope();
                _logService = scope.ServiceProvider.GetRequiredService<IService<Log, DataContext>>();
                _topicService = scope.ServiceProvider.GetRequiredService<IService<Topic, DataContext>>();
                _blackService = scope.ServiceProvider.GetRequiredService<IService<Black, DataContext>>();
                _redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();
                Token = await _redisService.GetAsync<NGBToken>("Token");

                _blackList = [.. await _blackService.GetListAsync(q => q.State == 1)];
                int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddSeconds(-30));
                try
                {
                    var _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
                    _jfYuRequest.Url = "https://bbs.nga.cn/thread.php";
                    _jfYuRequest.RequestData = $"fid={fid}&page={i}&order_by=lastpostdesc";
                    _jfYuRequest.RequestEncoding = Encoding.GetEncoding("GB18030");
                    _jfYuRequest.Timeout = 10;
                    _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "guestJs", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                    _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "lastvisit", Value = timeStamp.ToString(), Domain = ".bbs.nga.cn", Path = "/" });
                    _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "ngaPassportCid", Value = Token?.Token, Domain = ".nga.cn", Path = "/" });
                    _jfYuRequest.RequestCookies.Add(new Cookie() { Name = "ngaPassportUid", Value = Token?.Uid, Domain = ".nga.cn", Path = "/" });
                    var html = await _jfYuRequest.SendAsync();

                    if (html.Contains("访客不能直接访问"))
                    { 
                        var _log = new Log
                        {
                            Type = "登录",
                            Info = "需要登录"
                        };
                        await WriteLogAsync(_log);
                        await LoginAsync(_jfYuRequest.ResponseCookies);
                        continue;
                    }

                    if (string.IsNullOrEmpty(html))
                        continue;
                    if (_jfYuRequest.StatusCode != HttpStatusCode.OK)
                    {
                        var _log = new Log
                        {
                            Msg = html,
                            Trace = _jfYuRequest.StatusCode.ToString(),
                            Type = "获取帖子列表出错",
                            Info = "httpStatusCode错误"
                        };
                        await WriteLogAsync(_log);
                        await GetRandomDelayAsync();
                        continue;
                    }
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
                        t.Tid = t.Url.Replace("/read.php?tid=", "").Trim();
                        if (CheckBlackList(t))
                        {
                            var topic = await _topicService.GetOneAsync(q => q.Tid == t.Tid);
                            if (topic == null)
                            {
                                await _topicService.AddAsync(t);
                                _rabbitMQService.Send("ex_topic", t);
                                _logger.LogInformation($"fid:{t.Title}开始");
                            }
                            else
                            {
                                //无新回复/正在采集      
                                if (topic.ReptileNum >= int.Parse(t.Replies))
                                    continue;
                                topic.Replies = t.Replies;
                                topic.LastReplyer = t.LastReplyer;
                                await _topicService.UpdateAsync(topic);
                                _rabbitMQService.Send("ex_topic", t);
                                _logger.LogInformation($"fid:{t.Title}开始");
                            }

                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"fid:{ex.Message}开始");
                    var _log = new Log
                    {
                        Msg = ex.Message,
                        Trace = ex.StackTrace,
                        Type = "获取帖子列表出错",
                        Info = $"页码:{i}"
                    };
                    await WriteLogAsync(_log);
                    await GetRandomDelayAsync();
                    continue;
                }
                await GetRandomDelayAsync();
                i = i > 5 ? 1 : ++i;
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
