namespace WebApi.Model.Request
{
    public class QueryTopicRequest : QueryRequest
    { 
        public bool OnlyAuthor { get; set; }

        public bool OnlyImage { get; set; }
    }
}
