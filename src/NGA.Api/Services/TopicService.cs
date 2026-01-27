using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using NGA.Api.Model.Request;
using NGA.Models;
using NGA.Models.Entity;

namespace NGA.Api.Services
{
    public class TopicService(DataContext context, ReadonlyDBContext<DataContext> readonlyDBContext) : Service<Topic, DataContext>(context, readonlyDBContext), ITopicService
    {
        public async Task<PagedData<Topic>> GetListAsync(QueryRequest query)
        {
            var latestTopics = ReadonlyContext.Topics.Where(q => q.Title.Contains(query.SearchKey) || string.IsNullOrEmpty(query.SearchKey)).OrderByDescending(x => x.LastReplyTime ?? DateTime.MinValue);
            return await latestTopics.ToPagedAsync(query.PageIndex, query.PageSize);
        }
    }
}
