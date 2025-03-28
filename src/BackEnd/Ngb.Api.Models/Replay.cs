using jfYu.Core.MongoDB;
using System.Collections.Generic;

namespace Ngb.Api.Models
{
    public class Replay:BaseEntity
    {
        private string tid;//文章ID
        private string uid;//用户id
        private string pid; //回复id
        private int sort;//几楼
        private int support;//赞
        private int oppose;//踩
        private string moblie;//手机
        private string postDate;//发布日期
        private string content; //内容
        private string quotePid;//引用回复楼层ID
        private List<string> isDownload = new List<string>();//存储已下载附件


        public string Tid { get => tid; set => tid = value; }
        public string Uid { get => uid; set => uid = value; }
        public string Pid { get => pid; set => pid = value; }
        public int Sort { get => sort; set => sort = value; }
        public int Support { get => support; set => support = value; }
        public int Oppose { get => oppose; set => oppose = value; }
        public string Moblie { get => moblie; set => moblie = value; }
        public string PostDate { get => postDate; set => postDate = value; }
        public string Content { get => content; set => content = value; }
        public string QuotePid { get => quotePid; set => quotePid = value; }

        public List<string> IsDownload { get => isDownload; set => isDownload = value; }
    }

}
