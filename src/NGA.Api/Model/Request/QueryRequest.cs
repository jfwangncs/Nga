namespace NGA.Api.Model.Request
{
    public class QueryRequest
    {
        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public string SearchKey { get; set; } = "";
    }
}
