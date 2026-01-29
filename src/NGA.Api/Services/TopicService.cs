using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using Mapster;
using Microsoft.EntityFrameworkCore;
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
            var topicsQuery = ReadonlyContext.Topics.Where(x => string.IsNullOrEmpty(query.SearchKey) || x.Title.Contains(query.SearchKey))
                .OrderByDescending(x => x.LastReplyTime ?? DateTime.MinValue);
            var topic = await topicsQuery.ToPagedAsync(query.PageIndex, query.PageSize);
            var avatars = await ReadonlyContext.Users.Where(u => topic.Data.Select(t => t.Uid).Contains(u.Uid))
                .Select(u => new { u.Uid, u.Avatar })
                .ToDictionaryAsync(u => u.Uid, u => u.Avatar ?? "");
             
            var responses = topic.Data.Select(t =>
            {
                var response = t.Adapt<TopicResponse>();
                response.Avatar = avatars.TryGetValue(t.Uid, out var avatar) ? avatar : "";
                return response;
            }).ToList();

            return new PagedData<TopicResponse>()
            {
                Data = responses,
                TotalCount = topic.TotalCount,
                TotalPages = topic.TotalPages,
            };
        }
    }
}
