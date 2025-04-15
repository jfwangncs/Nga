﻿using jfYu.Core.Data.Service;
using jfYu.Core.jfYuRequest;
using jfYu.Core.jfYuRequest.Enum;
using jfYu.Core.RabbitMQ;
using jfYu.Core.Redis.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NGA.Models;
using NGA.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NGA.Base
{
    public abstract class BaseTask : BackgroundService
    {
        protected NGBToken? Token;
        protected CookieCollection authcookies = [];

        public readonly IServiceScopeFactory _scopeFactory;    
         
        protected IRedisService _redisService;
        protected LogFilter _logFilter = new LogFilter() { LoggingFields = JfYuLoggingFields.None, RequestFilter = z => z, ResponseFilter = z => "" };
        private ILogger<JfYuHttpRequest> _logger1;
        protected BaseTask(IServiceScopeFactory scopeFactory, ILogger<JfYuHttpRequest> logger1)
        {
            _scopeFactory = scopeFactory;        
            _logger1 = logger1;
        }

        protected async Task GetRandomDelayAsync()
        {
            string speed = Environment.GetEnvironmentVariable("SPEED") ?? "slowly";
            int workmin = speed == "slowly" ? 5 : 3;
            int workmax = speed == "slowly" ? 30 : 10;
            int restmin = speed == "slowly" ? 30 : 10;
            int restmax = speed == "slowly" ? 120 : 40;
            int delay;
            if (DateTime.Now.Hour > 9 && DateTime.Now.Hour < 24)
                delay = new Random(DateTime.Now.Millisecond).Next(1000 * workmin, 1000 * workmax);
            else
                delay = new Random(DateTime.Now.Millisecond).Next(1000 * restmin, 1000 * restmax);
            await Task.Delay(delay);
        }
        protected async Task WriteLogAsync(Log l)
        {
            using var scope = _scopeFactory.CreateScope();
            var _logService = scope.ServiceProvider.GetRequiredService<IService<Log, DataContext>>();

            var log = await _logService.GetOneAsync(q => q.Info == l.Info && q.Msg == l.Msg && q.Type == l.Type);
            if (log == null)
                await _logService.AddAsync(l);
        }

        protected async Task LoginAsync(CookieCollection cookies)
        {
            using var scope = _scopeFactory.CreateScope();
            _redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();


            if (await _redisService.LockTakeAsync("Login", TimeSpan.FromMinutes(10)))
            {
                var errorCount = 0;
                while (true)
                {
                    try
                    {
                        Random random = new Random();
                        double randomValue = random.NextDouble();
                        string randomString = randomValue.ToString().Substring(2);

                        var _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
                        //登陆
                        _jfYuRequest.Url = "https://bbs.nga.cn/nuke.php";
                        _jfYuRequest.Method = HttpMethod.Post;
                        _jfYuRequest.RequestEncoding = Encoding.GetEncoding("GB18030");
                        _jfYuRequest.Timeout = 30;
                        _jfYuRequest.ContentType = RequestContentType.FormData;
                        _jfYuRequest.RequestHeader.Referer = "https://bbs.nga.cn/nuke/account_copy.html?login";
                        _jfYuRequest.RequestData = $"__lib=login&__output=1&app_id=5004&device=&trackid=&__act=login&__ngaClientChecksum=&name=xiaoyuNcs&type=&password=Ncscd1111&rid=login{randomString}";
                        for (int i = 0; i < cookies.Count; i++)
                            _jfYuRequest.RequestCookies.Add(new Cookie() { Name = cookies[i].Name, Value = cookies[i].Value, Domain = cookies[i].Domain, Path = cookies[i].Path });

                        var rsp = await RefreshCode(cookies, randomString);
                        //var rsp = "131334";
                        _jfYuRequest.RequestData += $"&captcha={rsp}";

                        var loginres = await _jfYuRequest.SendAsync();
                        if (loginres.Contains("登录成功")) //登陆成功
                        {
                            var rg = new Regex(@"""uid"":(?<uid>.*?),");
                            var Uid = rg.Match(loginres).Groups["uid"].Value;
                            var Token = new Regex(@"""token"":""(?<token>.*?)"",").Match(loginres).Groups["token"].Value;
                            authcookies = _jfYuRequest.ResponseCookies;
                            Console.WriteLine("登录成功");
                            await _redisService.AddAsync("Token", new NGBToken() { Uid = Uid, Token = Token });
                            break;
                        }
                        else
                        {
                            errorCount++;
                            if (errorCount > 10)
                                throw new Exception("登录失败超过10次");
                        }
                        await Task.Delay(1 * 60 * 1000);

                    }
                    catch (Exception ex)
                    {
                        await WriteLogAsync(new Log() { Type = "login", Info = "登录失败", Msg = ex.Message });
                    }
                    finally
                    {
                        await _redisService.LockReleaseAsync("Login");
                    }
                }
            }
            else
            {
                await WriteLogAsync(new Log() { Type = "login", Info = "redis锁定中" });
            }

        }

        protected async Task<string> RefreshCode(CookieCollection cookies, string randomString)
        {
            var _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
            _jfYuRequest.Url = $"https://bbs.nga.cn/login_check_code.php?id=login{randomString}&from=login";
            _jfYuRequest.RequestEncoding = Encoding.GetEncoding("GB18030");
            _jfYuRequest.RequestHeader.Referer = "https://bbs.nga.cn/nuke/account_copy.html?login";

            foreach (Cookie item in cookies)
                _jfYuRequest.RequestCookies.Add(new Cookie() { Name = item.Name, Value = item.Value, Domain = item.Domain, Path = item.Path });


            await _jfYuRequest.DownloadFileAsync("code.png");


            _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
            _jfYuRequest.Url = "https://www.ejiaimg.cn/upload";
            var html = await _jfYuRequest.SendAsync();
            string pattern = @"<meta\s+name=""csrf-token""\s+content=""([^""]+)""";

            // 使用 Regex 提取匹配内容
            Match match = Regex.Match(html, pattern);
            string csrfToken = match.Groups[1].Value;
            var uploadcookies = _jfYuRequest.ResponseCookies;
            _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
            _jfYuRequest.Url = "https://www.ejiaimg.cn/upload";
            _jfYuRequest.Method = HttpMethod.Post;
            _jfYuRequest.ContentType = RequestContentType.FormData;
            _jfYuRequest.Files = new Dictionary<string, string> { { "file", "code.png" } };
            _jfYuRequest.RequestHeader.Accept = "application/json";
            _jfYuRequest.RequestCustomHeaders.Add("X-CSRF-TOKEN", csrfToken);
            foreach (Cookie item in uploadcookies)
                _jfYuRequest.RequestCookies.Add(new Cookie() { Name = item.Name, Value = item.Value, Domain = item.Domain, Path = item.Path });

            var imgae = await _jfYuRequest.SendAsync();
            var imgurl = JsonConvert.DeserializeObject<ResponseModel>(imgae).Data.Links.Url;

            _jfYuRequest = new JfYuHttpRequest(_logFilter, _logger1);
            _jfYuRequest.Url = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            _jfYuRequest.Method = HttpMethod.Post;
            _jfYuRequest.ContentType = RequestContentType.Json;
            _jfYuRequest.Authorization = "sk-6c355665886143268de653b89db74cfa";
            _jfYuRequest.RequestData = JsonConvert.SerializeObject(new ApiRequest()
            {
                Model = "qwen-vl-max",
                Messages = new List<ApiRequestMessage>() { new ApiRequestMessage() { Role= "user", Content=new List<Content>() {
                new Content() { Type= "text", Text= "验证码是多少,不要添加前缀 后缀的语言 直接输出" },
                new Content(){ Type="image_url", ImageUrl=new ImageUrl(){ Url=imgurl } }
            } } }
            });

            var codeHtml = await _jfYuRequest.SendAsync();
            return JsonConvert.DeserializeObject<ApiResponse>(codeHtml).Choices.FirstOrDefault().Message.Content;
        }
    }
}
