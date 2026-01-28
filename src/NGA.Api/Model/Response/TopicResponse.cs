namespace NGA.Api.Model.Response
{
    public class TopicResponse
    { 
        public string Url { get; set; } 
        public string Fid { get; set; }         
        public string Uid { get; set; } 
        public string Tid { get; set; } 
        public string Title { get; set; }        
        public string Replies { get; set; } 
        public string UserName { get; set; }         
        public string PostDate { get; set; }
        public DateTime? LastReplyTime { get; set; }
        public string Avatar { get; set; }
    }
}
