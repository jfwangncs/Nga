using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using Mapster;
using NGA.Api.Model.Request;
using NGA.Api.Model.Response;
using NGA.Models;
using NGA.Models.Entity;

namespace NGA.Api.Services
{
    public class TopicService(DataContext context, ReadonlyDBContext<DataContext> readonlyDBContext) : Service<Topic, DataContext>(context, readonlyDBContext), ITopicService
    {
        public async Task<PagedData<TopicResponse>> GetListAsync(QueryRequest query)
        {
            var topicsQuery = ReadonlyContext.Topics
                .GroupJoin(
                    ReadonlyContext.Users,
                    topic => topic.Uid,
                    user => user.Uid,
                    (topic, userGroup) => new { topic, userGroup })
                .SelectMany(
                    x => x.userGroup.DefaultIfEmpty(),
                    (x, user) => new
                    {
                        Topic = x.topic,
                        Avatar = user != null ? user.Avatar : null
                    })
                .Where(x => string.IsNullOrEmpty(query.SearchKey) || x.Topic.Title.Contains(query.SearchKey))
                .OrderByDescending(x => x.Topic.LastReplyTime ?? DateTime.MinValue);

            return await topicsQuery.ToPagedAsync(q => { return q.Select(x => { var d = x.Topic.Adapt<TopicResponse>(); d.Avatar = x.Avatar ?? ""; return d; }); }, query.PageIndex, query.PageSize);
        }
    }
}
