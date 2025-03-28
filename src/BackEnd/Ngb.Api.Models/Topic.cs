using jfYu.Core.MongoDB;

namespace Ngb.Api.Models
{
    public class Topic : BaseEntity
    {
        private string url;//连接
        private string tid;//文章ID
        private string uid;//用户id
        private string title;//标题
        private string replies;//回复数
        private string lastReplyer;//最后回复作者     
        private string thread;//类型
        private string postDate;//发布日期
        private int condition = 0;  //0表示主题正常  1正在采集中  -1 错误中断
        private int reptileNum = 0;  //已抓取数
        public string Url { get => url; set => url = value; }
        public string Uid { get => uid; set => uid = value; }
        public string Tid { get => tid; set => tid = value; }
        public string Title { get => title; set => title = value; }
        public string Replies { get => replies; set => replies = value; }
        public string LastReplyer { get => lastReplyer; set => lastReplyer = value; }
        public string PostDate { get => postDate; set => postDate = value; }
        public string Thread { get => thread; set => thread = value; }
        public int Condition { get => condition; set => condition = value; }
        public int ReptileNum { get => reptileNum; set => reptileNum = value; }
    }
}
