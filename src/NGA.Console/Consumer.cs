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

            int consumerCount = _consoleOptions.ConsumerCount;
            var tasks = new List<Task>();
            for (int i = 0; i < consumerCount; i++)
            {
                var taskId = i + 1;
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var _rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
                    var channel = await _rabbitMQService.ReceiveAsync<string>("topic", async q => await HandleTopicAsync(q, taskId));
                }, stoppingToken));
            }
            await Task.WhenAll(tasks);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// 准备开始
        /// </summary>     
        protected async Task<bool> HandleTopicAsync(string? tid, int taskId)
        {
            using var activity = new ActivitySource(Program.ServiceName).StartActivity("consumer.run", ActivityKind.Internal);
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
            using var childActivity = new ActivitySource(Program.ServiceName).StartActivity("process", ActivityKind.Internal);
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
                        await _topicService.UpdateAsync(topic);
                    }
                    if (result.Item2)
                        break;

                    page++;
                    await RandomDelayExtension.GetRandomDelayAsync();
                    cts.Token.ThrowIfCancellationRequested();
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
                var anonymousReg = Regex.Matches(userInfoAll, @"""-1"":{.*?username{1}.*?}", RegexOptions.IgnoreCase);
                var anonymous = new Dictionary<string, UserinfoJson>();
                foreach (Match item in anonymousReg)
                {
                    var value = $"{{{item.Value}}}}}}}";
                    var data = JsonConvert.DeserializeObject<Dictionary<string, UserinfoJson>>(value);
                    if (data != null)
                        anonymous.Add(data.FirstOrDefault().Key, data.FirstOrDefault().Value);
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
                        Regex rprg = new Regex(@"\[b\]Reply to.*?\[/b]");
                        MatchCollection re = rprg.Matches(contentNode.InnerHtml);
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
                        Regex quoterg = new Regex(@"\[quote\].*?\[/quote\]");
                        MatchCollection quotergmc = quoterg.Matches(contentNode.InnerHtml);
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
                        await GetUserInfo(replay.Uid);
                        if (replay.Uid.StartsWith("-1"))
                        {
                            UserinfoJson? uij;
                            var a = anonymous.TryGetValue(replay.Uid, out uij);
                            if (uij != null)
                            {
                                replay.UName = GetName(uij.Username);
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
                // 获取用户信息     
                async Task GetUserInfo(string uid)
                {
                    if (uid == null || uid.StartsWith("-") || uid == "0")
                        return;
                    var user = await _userService.GetOneAsync(q => q.Uid == uid);
                    if (user == null || (DateTime.Now - user.UpdatedTime).Days > 7)
                    {
                        if (user == null)
                            user = new User();
                        var html = "";
                        try
                        {
                            int timeStamp = UnixTime.GetUnixTime(DateTime.Now.AddMinutes(-30));
                            _ngaClient.Url = $"https://bbs.nga.cn/nuke.php?__lib=ucp&__act=get&lite=js&uid={uid}";
                            html = await _ngaClient.SendAsync();
                            user.Uid = uid;
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
            Regex qariRegex = new Regex(@"\[url\].*?\[/url\]");
            MatchCollection urls = qariRegex.Matches(_context);
            foreach (Match m in urls)
            {
                var url = m.Value.Replace("[url]", "").Replace("[/url]", "");
                var ahtml = $"<a target=\"_blank\" href=\"{url}\">{url}</a>";
                _context = _context.Replace(m.Value, ahtml);
            }
            //图片替换
            Regex imgrg = new Regex(@"\[img\].*?\[/img\]");
            MatchCollection imgs = imgrg.Matches(_context);
            foreach (Match m in imgs)
            {
                var img = m.Value.Replace("[img]", "").Replace("[/img]", "").Replace(".medium.jpg", "").Replace("./", "");
                var imghtml = "";
                if (img.Contains("https://img.nga.178.com/attachments") || img.Contains("http://img.nga.178.com/attachments"))
                    imghtml = $" <img class='topicimg' src='{img}'/>";
                else if (new Regex(@"[a-zA-z]+://[^\s]*").IsMatch(img)) //如果是外链 则不添加加头部
                    imghtml = $" <img class='topicimg' src='{img}'/>";
                else
                    imghtml = $" <img class='topicimg' src='https://img.nga.178.com/attachments/{img}'/>";
                _context = _context.Replace(m.Value, imghtml);
            }
            //回复处理
            Regex rprg = new Regex(@"\[b\]Reply to.*?\[/b]");
            MatchCollection re = rprg.Matches(_context);
            foreach (Match m in re)
            {
                //var rgroup = new Regex(@"\[uid=(.*?)\](.*?)\[/uid\].*?\((.*?)\)").Match(m.Value).Groups;
                //string id = rgroup[1].Value;
                //string name = rgroup[2].Value;
                //string time = rgroup[3].Value;
                quotepid = new Regex(@"\[pid=(.*?),.*?\]").Match(m.Value).Groups[1]?.Value;
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
            Regex quoterg = new Regex(@"\[quote\].*?\[/quote\]");
            MatchCollection quotergmc = quoterg.Matches(_context);
            foreach (Match m in quotergmc)
            {
                quotepid = new Regex(@"\[pid=(.*?),.*?\]").Match(m.Value).Groups[1]?.Value;

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


