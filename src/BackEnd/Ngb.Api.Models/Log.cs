using jfYu.Core.MongoDB;

namespace Ngb.Api.Models
{
    public class Log : BaseEntity
    {
        private string msg;
        private string trace;
        private string info;
        private string type;

        public string Msg { get => msg; set => msg = value; }
        public string Trace { get => trace; set => trace = value; }
        public string Info { get => info; set => info = value; }
        public string Type { get => type; set => type = value; }
    }
}
