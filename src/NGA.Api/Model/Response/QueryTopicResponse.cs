using JfYu.Data.Model;
using NGA.Models.Entity;

namespace NGA.Api.Model.Response
{
    public class QueryTopicResponse
    {
        public PagedData<Replay> Replay { get; set; }
        public Dictionary<string,User> User { get; set; }
        public Topic Topic { get; set; }
    }
}
