using jfYu.Core.MongoDB;

namespace Ngb.Api.Models
{
    public class User : BaseEntity
    {
        private string uid;//id
        private string username;//用户名
        private string group;//分组
        private string regdate;//注册时间
        private string avatar;//头像

        public string Uid { get => uid; set => uid = value; }
        public string UserName { get => username; set => username = value; }
        public string Group { get => group; set => group = value; }
        public string Regdate { get => regdate; set => regdate = value; }
        public string Avatar { get => avatar; set => avatar = value; }
    }  
}
