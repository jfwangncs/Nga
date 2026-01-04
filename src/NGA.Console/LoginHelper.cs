using JfYu.Redis.Interface;
using JfYu.Request;
using JfYu.Request.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NGA.Models.Constant;
using NGA.Models.Models;
using NGA.Models.Models.RefreshCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NGA.Console
{
    public class LoginHelper : ILoginHelper
    {
        private readonly IRedisService _redisService;
        private readonly IJfYuRequest _ngaClient;
        private readonly IJfYuRequest _qianWenClient;
        private readonly ILogger<LoginHelper> _logger;
        private readonly ConsoleOptions _consoleOptions;
        public LoginHelper(IRedisService redisService, IJfYuRequestFactory httpClientFactory, ILogger<LoginHelper> logger, IOptions<ConsoleOptions> image)
        {
            _redisService = redisService;
            _logger = logger;
            _consoleOptions = image.Value;
            _ngaClient = httpClientFactory.CreateRequest(HttpClientName.NgaClientName);
            _qianWenClient = httpClientFactory.CreateRequest(HttpClientName.QianWenClientName);
        }

        public async Task LoginAsync()
        {
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
                        var rsp = await RefreshCode(randomString);
                        //登陆
                        _ngaClient.Url = "https://bbs.nga.cn/nuke.php";
                        _ngaClient.Method = HttpMethod.Post;
                        _ngaClient.RequestEncoding = Encoding.GetEncoding("GB18030");
                        _ngaClient.Timeout = 30;
                        _ngaClient.RequestHeader.AcceptEncoding = "";
                        _ngaClient.ContentType = RequestContentType.FormData;
                        _ngaClient.RequestHeader.Referer = "https://bbs.nga.cn/nuke/account_copy.html?login";
                        _ngaClient.RequestData = $"__lib=login&__output=1&app_id=5004&device=&trackid=&__act=login&__ngaClientChecksum=&name=xiaoyuNcs&type=&password=Ncscd1111&rid=login{randomString}";
                        _ngaClient.RequestData += $"&captcha={rsp}";
                        var loginres = await _ngaClient.SendAsync();
                        if (loginres.Contains("登录成功"))
                        {
                            var rg = new Regex(@"""uid"":(?<uid>.*?),");
                            var Uid = rg.Match(loginres).Groups["uid"].Value;
                            var Token = new Regex(@"""token"":""(?<token>.*?)"",").Match(loginres).Groups["token"].Value;
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
                        _logger.LogError(ex, "登录异常");
                    }
                }
            }
            else
            {
                _logger.LogWarning("登录被锁定，等待2分钟");
                await Task.Delay(2 * 60 * 1000);
            }

        }

        protected async Task<string?> RefreshCode(string randomString)
        {
            _ngaClient.Url = $"https://bbs.nga.cn/login_check_code.php?id=login{randomString}&from=login";
            _ngaClient.RequestEncoding = Encoding.GetEncoding("GB18030");
            _ngaClient.RequestHeader.Referer = "https://bbs.nga.cn/nuke/account_copy.html?login";
            var stream = await _ngaClient.DownloadFileAsync();
            var base64String = Convert.ToBase64String(stream?.ToArray() ?? []);

            _qianWenClient.Url = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            _qianWenClient.Method = HttpMethod.Post;
            _qianWenClient.ContentType = RequestContentType.Json;
            _qianWenClient.Authorization = _consoleOptions.QianWenToken;
            _qianWenClient.RequestHeader.AcceptEncoding = "";
            _qianWenClient.RequestEncoding = Encoding.UTF8;
            _qianWenClient.RequestData = JsonConvert.SerializeObject(new QianWenRequest()
            {
                Model = "qwen-vl-max",
                Messages = new List<QianWenRequestMessage>() { new QianWenRequestMessage() { Role= "user", Content=new List<Content>() {
                new Content() { Type= "text", Text= "验证码是多少,不要添加前缀 后缀的语言 直接输出" },
                new Content(){ Type="image_url", ImageUrl=new ImageUrl(){ Url=$"data:image/png;base64,{base64String}" } }
            } } }
            });

            var codeHtml = await _qianWenClient.SendAsync();
            return JsonConvert.DeserializeObject<QianWenResponse>(codeHtml)?.Choices.FirstOrDefault()?.Message.Content;

        }
    }
}
