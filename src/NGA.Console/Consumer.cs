using HtmlAgilityPack;
using JfYu.Data.Service;
using JfYu.RabbitMQ;
using JfYu.Redis.Interface;
using JfYu.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NGA.Models;
using NGA.Models.Constant;
using NGA.Models.Entity;
using NGA.Models.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NGA.Console
{
    class Consumer : BackgroundService
    {

        private NGBToken? _token;
        string ConsumerType = Environment.GetEnvironmentVariable("ConsumerType") ?? "New";//All 爬取所有 New爬取新回复   
        private ILogger<Consumer> _logger;
        private readonly ConsoleOptions _consoleOptions;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly Meter _meter = new Meter(Program.ServiceName);
        private static readonly Counter<long> _consumedItemsCounter = _meter.CreateCounter<long>("nga_consumer_consumed_items_total", "items", "Total number of items consumed by consumer");
        private readonly IRedisService _redisService;
        private static readonly ActivitySource _activitySource = new ActivitySource(Program.ServiceName);
        private CancellationTokenSource _consumerCts;
        private bool _isRunning = false;
        private List<Task> _consumerTasks;

        // 预编译正则表达式以提升性能和减少内存
        private static readonly Regex ReplyRegex = new Regex(@"\[b\]Reply to.*?\[/b\]", RegexOptions.Compiled);
        private static readonly Regex QuoteRegex = new Regex(@"\[quote\].*?\[/quote\]", RegexOptions.Compiled);
        private static readonly Regex PidRegex = new Regex(@"\[pid=(.*?),.*?\]", RegexOptions.Compiled);
        private static readonly Regex UrlRegex = new Regex(@"\[url\].*?\[/url\]", RegexOptions.Compiled);
        private static readonly Regex ImgRegex = new Regex(@"\[img\].*?\[/img\]", RegexOptions.Compiled);
        private static readonly Regex ExternalUrlRegex = new Regex(@"[a-zA-z]+://[^\s]*", RegexOptions.Compiled);

        public Consumer(IServiceScopeFactory scopeFactory, ILogger<Consumer> logger, IOptions<ConsoleOptions> consoleOptions, IRedisService redisService)
        {
            _logger = logger;
            _consoleOptions = consoleOptions.Value;
            _scopeFactory = scopeFactory;
            _redisService = redisService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _token ??= await _redisService.GetAsync<NGBToken>("Token");

            // 启动定时检查任务
            var schedulerTask = Task.Run(() => RunScheduler(stoppingToken), stoppingToken);

            await StartConsumers(stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


        private async Task StartConsumers(CancellationToken stoppingToken)
        {
            if (_isRunning)
            {
                _logger.LogWarning("已在运行中，跳过启动");
                return;
            }

            _logger.LogInformation("启动 Consumer, ServiceName: {ServiceName}, ConsumerCount: {Count}", Program.ServiceName, _consoleOptions.ConsumerCount);

            try
            {
                _consumerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                int consumerCount = _consoleOptions.ConsumerCount;
                _consumerTasks = new List<Task>();

                for (int i = 0; i < consumerCount; i++)
                {
                    var taskId = i + 1;
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();

                            _logger.LogInformation("{TaskId}开始接收消息", taskId);

                            await rabbitMQService.ReceiveAsync<string>(
                                "topic",
                                async q => await HandleTopicAsync(q, taskId), prefetchCount: 10, autoAck: true, cancellationToken: _consumerCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("{TaskId}已被取消", taskId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{TaskId}发生异常", taskId);
                        }
                    }, _consumerCts.Token);

                    _consumerTasks.Add(task);
                }

                _isRunning = true;
                _logger.LogInformation("已启动{Count}个Task", consumerCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动失败");
            }
        }

        private async Task StopConsumers()
        {
            if (!_isRunning)
            {
                _logger.LogWarning("未在运行，跳过停止");
                return;
            }


            _logger.LogInformation("开始停止 Consumer ");

            try
            {
                _consumerCts?.Cancel();

                if (_consumerTasks != null && _consumerTasks.Count > 0)
                {
                    var waitTask = Task.WhenAll(_consumerTasks);
                    var completedInTime = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(30))) == waitTask;

                    if (completedInTime)
                    {
                        _logger.LogInformation("消费者已停止");
                    }
                    else
                    {
                        _logger.LogWarning("部分消费者未在30秒内停止，强制结束");
                    }
                }

                _isRunning = false;
                _logger.LogInformation("已全部停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止时发生异常");
            }
            finally
            {
                _consumerCts?.Dispose();
                _consumerTasks?.Clear();
            }
        }

        private async Task RunScheduler(CancellationToken stoppingToken)
        {
            // 每分钟检查一次
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.Now;

                if (now.Hour == 5 && now.Minute >= 55 && now.Minute < 59 && _isRunning)
                {
                    _logger.LogInformation("到达维护时间 {Time}，停止消费者", now.ToString("HH:mm"));
                    await StopConsumers();
                }
                else if (now.Hour == 6 && now.Minute >= 50 && now.Minute < 59 && !_isRunning)
                {
                    _logger.LogInformation("维护结束 {Time}，启动消费者", now.ToString("HH:mm"));
                    await StartConsumers(stoppingToken);
                }
            }
        }

        /// <summary>
        /// 准备开始
        /// </summary>     
        protected async Task<bool> HandleTopicAsync(string? tid, int taskId)
        {
            using var activity = _activitySource.StartActivity("consumer.run", ActivityKind.Internal);
            if (string.IsNullOrEmpty(tid))
                return true;
            using var scope = _scopeFactory.CreateScope();
            var _topicService = scope.ServiceProvider.GetRequiredService<IService<Topic, DataContext>>();
            var _blackService = scope.ServiceProvider.GetRequiredService<IService<Black, DataContext>>();
            var _replayService = scope.ServiceProvider.GetRequiredService<IService<Replay, DataContext>>();
            var _replayHisService = scope.ServiceProvider.GetRequiredService<IService<ReplayHis, DataContext>>();
            var _userService = scope.ServiceProvider.GetRequiredService<IService<User, DataContext>>();
            var _ngaClient = scope.ServiceProvider.GetRequiredService<IJfYuRequest>();
            _ngaClient.RequestCookies.Add(new Cookie() { Name = "ngaPassportCid", Value = _token?.Token, Domain = ".nga.cn", Path = "/" });
            _ngaClient.RequestCookies.Add(new Cookie() { Name = "ngaPassportUid", Value = _token?.Uid, Domain = ".nga.cn", Path = "/" });
            var topic = await _topicService.GetOneAsync(q => q.Tid == tid);
            if (topic == null)
                return true;
            if (!await _redisService.LockTakeAsync(topic.Tid, TimeSpan.FromHours(1)))
                return true;
            using var childActivity = _activitySource.StartActivity("consumer.process-topic", ActivityKind.Internal);
            childActivity?.SetTag("topic.tid", tid);
            childActivity?.SetTag("topic.title", topic.Title);
            childActivity?.SetTag("topic.startNum", topic.ReptileNum);
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(20));
            var originalNum = 0;
            var reptileNum = 0;
            if (ConsumerType == "New")
                originalNum = reptileNum = topic.ReptileNum;
            int page = reptileNum / 20 + 1;
            try
            {
                do
                {
                    var result = await MainAsync(topic, page, _userService, _replayService, taskId);
                    if (reptileNum != result.Item1 && result.Item1 != -1)
                    {
                        reptileNum = topic.ReptileNum = result.Item1;
                        topic.LastReplyTime = DateTime.Now;
                        await _topicService.UpdateAsync(topic);
                    }
                    if (result.Item2)
                        break;

                    page++;
                    cts.Token.ThrowIfCancellationRequested();
                    await RandomDelayExtension.GetRandomDelayAsync();
                } while (true);
                _consumedItemsCounter.Add(1, new KeyValuePair<string, object?>("fid", topic.Fid), new KeyValuePair<string, object?>("status", "success"));
                return true;
            }
            catch (OperationCanceledException ex)
            {
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogWarning("{TaskId}-{Tid}-{Title},超时发回继续处理", taskId, topic.Tid, topic.Title);
                _consumedItemsCounter.Add(1, new KeyValuePair<string, object?>("fid", topic.Fid), new KeyValuePair<string, object?>("status", "timeout"));
                return false;
            }
            catch (Exception ex)
            {
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "{TaskId}-{Tid}-{Title},发生错误", taskId, topic.Tid, topic.Title);
                _consumedItemsCounter.Add(1, new KeyValuePair<string, object?>("fid", topic.Fid), new KeyValuePair<string, object?>("status", "error"));
                return false;
            }
            finally
            {
                childActivity?.SetTag("topic.endNum", topic.ReptileNum);
                _logger.LogInformation("{TaskId}-{Tid}-{Title},{OriginalNum}-{ReptileNum}结束", taskId, topic.Tid, topic.Title, originalNum, topic.ReptileNum);
                await _redisService.LockReleaseAsync(topic.Tid);
            }

            async Task<Tuple<int, bool>> MainAsync(Topic topic, int page, IService<User, DataContext> _userService, IService<Replay, DataContext> _replayService, int taskId)
            {
                int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddSeconds(-30));
                HtmlDocument htmlDocument = new HtmlDocument();
                _ngaClient.Url = $"https://bbs.nga.cn/read.php?tid={topic.Tid}&page={page}";
                _ngaClient.RequestEncoding = Encoding.GetEncoding("GB18030");
                var html = await _ngaClient.SendAsync();
                if (html.Contains("访客不能直接访问") || html.Contains("未登录"))
                {
                    _logger.LogInformation("用户登录");
                    using var scope = _scopeFactory.CreateScope();
                    var _loginHelper = scope.ServiceProvider.GetRequiredService<ILoginHelper>();
                    await _loginHelper.LoginAsync();
                    return new Tuple<int, bool>(-1, true);
                }

                if (string.IsNullOrEmpty(html) || html.Contains("帖子发布或回复时间超过限制") || html.Contains("302 Found") || html.Contains("帖子被设为隐藏") || html.Contains("查看所需的权限/条件"))
                {
                    _logger.LogInformation($"{taskId}-{topic.Title}被隐藏");
                    return new Tuple<int, bool>(-1, true);
                }

                if (_ngaClient.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("{TaskId}-{Tid}-{Title}-{Html}状态不正确,", taskId, topic.Tid, topic.Title, html);
                    return new Tuple<int, bool>(-1, true);
                }
                htmlDocument.LoadHtml(html);
                HtmlNodeCollection lous = htmlDocument.DocumentNode.SelectNodes("//tr[contains(@id,'post1strow')]");
                if (lous == null)
                    return new Tuple<int, bool>(-1, true);

                var userInfoAll = htmlDocument.DocumentNode.SelectNodes("//script")?.Where(q => q.InnerText.Trim().ToString().Contains($"commonui.userInfo.setAll")).FirstOrDefault()?.InnerHtml;

                if (userInfoAll == null)
                {
                    _logger.LogInformation($"{taskId}-{topic.Title},{page}:找不到用户信息");
                    return new Tuple<int, bool>(-1, true);
                }

                var allUser = await GetAllUser(userInfoAll);
                if (allUser == null || allUser.Count <= 0)
                {
                    _logger.LogInformation($"{taskId}-{topic.Title},{page}:找不到用户信息");
                    return new Tuple<int, bool>(-1, true);
                }
                int maxPage = 0;
                var s = $"var __PAGE = {{0:'/read.php?tid={topic.Tid}',";
                var maxPageHtml = htmlDocument.DocumentNode.SelectNodes("//script").Where(q => q.InnerText.Trim().ToString().Contains(s)).FirstOrDefault()?.InnerHtml;
                if (!string.IsNullOrEmpty(maxPageHtml))
                    maxPage = int.Parse(maxPageHtml.Replace(s, "").Split(",")[0].Split(":")[1]);
                var lastsort = 0;
                for (int i = lous.Count - 1; i >= 0; i--)
                {

                    try
                    {
                        var contentNode = lous[i].SelectSingleNode(".//span[contains(@id,'postcontentandsubject')]");
                        int sort = int.Parse(contentNode.Id.Replace("postcontentandsubject", ""));
                        if (i == lous.Count - 1)
                        {
                            lastsort = sort;
                            if (sort == topic.ReptileNum && lous.Count > 1)
                                return new Tuple<int, bool>(lastsort, maxPage > page ? false : true);
                        }
                        #region 如有引用回复 则保存其引用用户名称                    
                        //回复用户名处理
                        MatchCollection re = ReplyRegex.Matches(contentNode.InnerHtml);
                        foreach (Match m in re)
                        {
                            var rgroup = new Regex(@"\[uid=(.*?)\](.*?)\[/uid\].*?\((.*?)\)").Match(m.Value).Groups;
                            string uid = rgroup[1].Value;
                            string name = rgroup[2].Value;
                            var user = await _userService.GetOneAsync(q => q.Uid == uid);
                            if (user != null)
                            {
                                user.UserName = name;
                                await _userService.UpdateAsync(user);
                            }
                        }

                        //引用回复用户名处理
                        MatchCollection quotergmc = QuoteRegex.Matches(contentNode.InnerHtml);
                        foreach (Match m in quotergmc)
                        {
                            var rgroup = new Regex(@"\[uid=(.*?)\](.*?)\[/uid\].*?\((.*?)\):\[/b\](.*?)\[").Match(m.Value).Groups;
                            string uid = rgroup[1].Value;
                            string name = rgroup[2].Value;
                            var user = await _userService.GetOneAsync(q => q.Uid == uid);
                            if (user != null)
                            {
                                user.UserName = name;
                                await _userService.UpdateAsync(user);
                            }
                        }
                        #endregion

                        var replay = await _replayService.GetOneAsync(x => x.Sort == sort && x.Tid == topic.Tid);
                        if (replay == null)
                            replay = new Replay();
                        replay.Tid = topic.Tid;
                        replay.Sort = sort;
                        //获取replayid
                        if (sort != 0)
                            replay.Pid = lous[i].SelectSingleNode(".//a[contains(@id,'pid')]").Id.Replace("pid", "").Replace("Anchor", "");
                        GetContextData(replay, htmlDocument, contentNode.ChildNodes[4].InnerHtml);
                        GetFloorData(replay, htmlDocument);
                        allUser.TryGetValue(replay.Uid, out UserinfoJson? uij);
                        if (uij != null)
                        {
                            await UpdateUserInfo(uij);
                            if (replay.Uid.StartsWith("-"))
                            {
                                replay.UName = GetName(uij.Username);
                                if (sort == 0)
                                    topic.UserName = replay.UName;
                            }
                            else
                            {
                                replay.UName = uij.Username;
                                if (sort == 0)
                                    topic.UserName = replay.UName;
                            }
                        }
                        if (replay.Id == 0)
                            await _replayService.AddAsync(replay);
                        else
                            await _replayService.UpdateAsync(replay);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{TaskId}-{Tid}-{Title}-{Page}读取楼层出错", taskId, topic.Tid, topic.Title, page);
                    }
                }
                return new Tuple<int, bool>(lastsort, maxPage > page ? false : true);

                async Task UpdateUserInfo(UserinfoJson userinfo)
                {
                    if (userinfo == null || userinfo.Uid <= 0)
                        return;
                    var user = await _userService.GetOneAsync(q => q.Uid == userinfo.Uid.ToString());
                    if (user == null || (DateTime.Now - user.UpdatedTime).Days > 7)
                    {
                        if (user == null)
                            user = new User();
                        var html = "";
                        try
                        {
                            int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddMinutes(-30));
                            _ngaClient.Url = $"https://bbs.nga.cn/nuke.php?__lib=ucp&__act=get&lite=js&uid={userinfo.Uid}";
                            html = await _ngaClient.SendAsync();
                            user.Uid = userinfo.Uid.ToString();
                            if (string.IsNullOrEmpty(html))
                                return;
                            if (html.Contains("参数错误"))
                                return;
                            if (html.Contains("找不到用户"))
                                return;
                            if (html.Contains("无此用户"))
                                return;
                            var rg = Regex.Match(html, "username\":.+?\"");
                            user.UserName = rg.ToString().Replace("\"", "").Split(':').ElementAtOrDefault(1) ?? "";
                            //_user.Name = hn.InnerText;
                            rg = Regex.Match(html, "group\":.+?\"");
                            user.Group = rg.ToString().Replace("\"", "").Split(':').ElementAtOrDefault(1) ?? "";
                            rg = Regex.Match(html, "regdate\":.+?\"");
                            user.Regdate = rg.ToString().Replace("\"", "").Replace(",", "").Split(':').ElementAtOrDefault(1) ?? "";
                            rg = Regex.Match(html, "avatar\":.+?\"");
                            user.Avatar = rg.ToString().Replace("\"", "").Replace("http:", "").Split(':').ElementAtOrDefault(1) ?? "";
                            user.UserName = string.IsNullOrEmpty(user.UserName) ? userinfo.Username : user.UserName;
                            user.Avatar = string.IsNullOrEmpty(user.Avatar) ? userinfo.Avatar : user.Avatar;
                            if (user.Id == 0)
                                await _userService.AddAsync(user);
                            else
                                await _userService.UpdateAsync(user);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("无效的 URI")) //用户头像url无效
                                return;
                            if (ex.Message == "远程服务器返回错误: (404) 未找到。") //用户本身头像url失效，错误日志不保存
                                return;
                            throw;
                        }
                    }
                }
                async Task<Dictionary<string, UserinfoJson>?> GetAllUser(string userInfoAll)
                {
                    try
                    {
                        // 提取 commonui.userInfo.setAll( ... ) 中的JSON对象
                        var startPattern = "commonui.userInfo.setAll(";
                        var startIndex = userInfoAll.IndexOf(startPattern);
                        if (startIndex >= 0)
                        {
                            startIndex += startPattern.Length;

                            // 使用括号平衡算法找到正确的结束位置
                            int braceDepth = 0;
                            int parenDepth = 0;
                            int endIndex = startIndex;
                            bool inString = false;
                            char stringChar = '\0';

                            for (int i = startIndex; i < userInfoAll.Length; i++)
                            {
                                char c = userInfoAll[i];

                                // 处理字符串内的字符
                                if (inString)
                                {
                                    if (c == stringChar && (i == 0 || userInfoAll[i - 1] != '\\'))
                                    {
                                        inString = false;
                                    }
                                    continue;
                                }

                                // 检测字符串开始
                                if (c == '"' || c == '\'')
                                {
                                    inString = true;
                                    stringChar = c;
                                    continue;
                                }

                                // 跟踪括号深度
                                if (c == '{') braceDepth++;
                                else if (c == '}') braceDepth--;
                                else if (c == '(') parenDepth++;
                                else if (c == ')')
                                {
                                    // 当所有括号都匹配且遇到 )，说明找到了 setAll() 的结束括号
                                    if (braceDepth == 0 && parenDepth == 0)
                                    {
                                        endIndex = i;
                                        break;
                                    }
                                    parenDepth--;
                                }
                            }

                            if (endIndex > startIndex)
                            {
                                var jsonContent = userInfoAll.Substring(startIndex, endIndex - startIndex).Trim();
                                return JsonConvert.DeserializeObject<Dictionary<string, UserinfoJson>>(jsonContent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{TaskId}-{Tid}-{Title},{Page}:解析用户信息失败，尝试备用方法", taskId, topic.Tid, topic.Title, page);
                    }
                    return default;
                }
            }
        }
       


        //获取楼层数据
        protected void GetFloorData(Replay floor, HtmlDocument doc)
        {
            //手机、支持反对数据、用户id
            var d = doc.DocumentNode.SelectNodes("//script").Where(q => q.InnerText.Trim().ToString().Contains($"commonui.postArg.proc( {floor.Sort}")).FirstOrDefault();
            if (d != null)
            {
                List<string> ds = SplitArgs(d.InnerText.Trim().Replace("\r\n", ""));
                floor.Moblie = ds[21].ToString();
                floor.Oppose = int.Parse(ds[17].Split(',')[2]);
                floor.Support = int.Parse(ds[17].Split(',')[1]);
                floor.Uid = ds[15].ToString();
                floor.PostDate = ds[16].ToString();
            }
        }

        //处理楼层数据
        protected void GetContextData(Replay floor, HtmlDocument doc, string _context)
        {
            //引用回复id 默认为匿名
            var quotepid = "-1";
            //处理附件
            var d1 = doc.DocumentNode.SelectNodes("//script").Where(q => q.InnerText.Trim().ToString().StartsWith($"ubbcode.attach.load('postattach{floor.Sort}'"));
            if (d1 != null && d1.Count() > 0)
            {
                var m = Regex.Matches(d1.First().InnerText, @"url:'(.+?)',.+?type:'(.+?)'", RegexOptions.IgnoreCase);
                foreach (Match item in m)
                {
                    var type = item.Groups[2].Value;
                    //附件是图片则附加到主楼之中
                    if (type == "img")
                    {
                        var imgurl = item.Groups[1].Value;
                        if (_context.Contains(imgurl)) //如主楼中有此图片则放弃
                            continue;
                        var imghtml = "";
                        if (imgurl.Contains("https://img.nga.178.com/attachments") || imgurl.Contains("http://img.nga.178.com/attachments"))
                            imghtml = $" <img class='topicimg' src='{imgurl}'/>";
                        else
                            imghtml = $" <img class='topicimg' src='https://img.nga.178.com/attachments/{imgurl}'/>";
                        _context += ("<br />" + imghtml);
                    }
                }
            }
            ;
            //url替换
            MatchCollection urls = UrlRegex.Matches(_context);
            foreach (Match m in urls)
            {
                var url = m.Value.Replace("[url]", "").Replace("[/url]", "");
                var ahtml = $"<a target=\"_blank\" href=\"{url}\">{url}</a>";
                _context = _context.Replace(m.Value, ahtml);
            }
            //图片替换
            MatchCollection imgs = ImgRegex.Matches(_context);
            foreach (Match m in imgs)
            {
                var img = m.Value.Replace("[img]", "").Replace("[/img]", "").Replace(".medium.jpg", "").Replace("./", "");
                var imghtml = "";
                if (img.Contains("https://img.nga.178.com/attachments") || img.Contains("http://img.nga.178.com/attachments"))
                    imghtml = $" <img class='topicimg' src='{img}'/>";
                else if (ExternalUrlRegex.IsMatch(img)) //如果是外链 则不添加加头部
                    imghtml = $" <img class='topicimg' src='{img}'/>";
                else
                    imghtml = $" <img class='topicimg' src='https://img.nga.178.com/attachments/{img}'/>";
                _context = _context.Replace(m.Value, imghtml);
            }
            //回复处理
            MatchCollection re = ReplyRegex.Matches(_context);
            foreach (Match m in re)
            {
                //var rgroup = new Regex(@"\[uid=(.*?)\](.*?)\[/uid\].*?\((.*?)\)").Match(m.Value).Groups;
                //string id = rgroup[1].Value;
                //string name = rgroup[2].Value;
                //string time = rgroup[3].Value;
                quotepid = PidRegex.Match(m.Value).Groups[1]?.Value;
                //var rp = Mongo.QueryOne<Replay>(q => q.Pid == pid);
                //string context = "";
                //if (rp != null)
                //{
                //    //去掉[b][/b][quote\][/quote\]类似编码
                //    var h = new Regex(@"\[b\]Reply to.*?\[/b]").Replace(rp.Content, "");
                //    h = new Regex(@"\[quote\].*?\[/quote\]").Replace(h, "");
                //    context = "<br/><br/>" + h;
                //}
                //var rehtml = $"<div class=\"quote\"> by <a href= \"/nuke.php?func=ucp&amp;uid={id}\" " +
                //$"class=\"b\">[{name}]</a> <span class=\"xtxt silver\" style=\"font-weight:normal\">({time})</span>" +
                //$"{context}</div>";
                _context = _context.Replace(m.Value, "{replay}");
            }
            //引用回复处理
            MatchCollection quotergmc = QuoteRegex.Matches(_context);
            foreach (Match m in quotergmc)
            {
                quotepid = PidRegex.Match(m.Value).Groups[1]?.Value;

                //var rgroup = new Regex(@"\[uid=(.*?)\](.*?)\[/uid\].*?\((.*?)\):\[/b\](.*?)\[").Match(m.Value).Groups;
                //string id = rgroup[1].Value;
                //string name = rgroup[2].Value;
                //string time = rgroup[3].Value;
                //string context = rgroup[4].Value;
                //var rehtml = $"<div class=\"quote\"> by <a href= \"/nuke.php?func=ucp&amp;uid={id}\" " +
                //    $"class=\"b\">[{name}]</a> <span class=\"xtxt silver\" style=\"font-weight:normal\">({time})</span>" +
                //    $"{context}</div>";
                _context = _context.Replace(m.Value, "{replay}");
            }
            floor.QuotePid = quotepid;
            floor.Content = _context;
        }


        //分割用户信息
        List<string> SplitArgs(string args)
        {
            //通过字符串分割为参数，先逗号分割然后遇到第一个单引号保存到临时变量，找到第二个单引号凑整参数保存
            List<string> t = new List<string>();
            string[] _as = args.Split(',');
            string ta = "";  //临时保存
            for (int i = 0; i < _as.Length; i++)
            {
                if (ta != "")
                {
                    ta += _as[i] + ",";
                    if (_as[i].ToCharArray().Count(x => x == '\'') == 1)
                    {
                        if (ta.Length > 0)
                        {
                            t.Add(ta.Substring(0, ta.Length - 1).Replace("'", ""));
                        }
                        else
                        {
                            t.Add(ta.Replace("'", ""));
                        }
                        ta = "";
                    }
                }
                else
                {
                    if (_as[i].ToCharArray().Count(x => x == '\'') == 1)
                        ta += _as[i] + ",";
                    else
                        t.Add(_as[i].Replace("'", ""));
                }
            }
            return t;
        }

        string GetName(string aname)
        {
            if (aname.Length < 6)
                return aname;
            var t1 = "甲乙丙丁戊己庚辛壬癸子丑寅卯辰巳午未申酉戌亥";
            var t2 = "王李张刘陈杨黄吴赵周徐孙马朱胡林郭何高罗郑梁谢宋唐许邓冯韩曹曾彭萧蔡潘田董袁于余叶蒋杜苏魏程吕丁沈任姚卢傅钟姜崔谭廖范汪陆金石戴贾韦夏邱方侯邹熊孟秦白江阎薛尹段雷黎史龙陶贺顾毛郝龚邵万钱严赖覃洪武莫孔汤向常温康施文牛樊葛邢安齐易乔伍庞颜倪庄聂章鲁岳翟殷詹申欧耿关兰焦俞左柳甘祝包宁尚符舒阮柯纪梅童凌毕单季裴霍涂成苗谷盛曲翁冉骆蓝路游辛靳管柴蒙鲍华喻祁蒲房滕屈饶解牟艾尤阳时穆农司卓古吉缪简车项连芦麦褚娄窦戚岑景党宫费卜冷晏席卫米柏宗瞿桂全佟应臧闵苟邬边卞姬师和仇栾隋商刁沙荣巫寇桑郎甄丛仲虞敖巩明佘池查麻苑迟邝";
            var i = 6;
            var n = "";
            for (var j = 0; j < 6; j++)
            {
                if (j == 0 || j == 3)
                {
                    if (i + 1 >= aname.Length)
                        return aname;
                    var hexValue = Convert.ToInt32("0x0" + aname.Substring(i + 1, 1), 16);
                    if (hexValue < 0 || hexValue >= t1.Length)
                        return aname;
                    n += t1.Substring(hexValue, 1);
                }
                else if (j < 6)
                {
                    if (i + 1 >= aname.Length)
                        return aname;
                    var hexValue = Convert.ToInt32("0x" + aname.Substring(i, 2), 16);

                    if (hexValue < 0 || hexValue >= t2.Length)
                        return aname;
                    n += t2.Substring(hexValue, 1);
                }
                i += 2;
            }
            return n;
        }
    }
}


