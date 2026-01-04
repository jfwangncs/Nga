using JfYu.Data.Context;
using JfYu.Data.Extension;
using JfYu.Data.Model;
using JfYu.Data.Service;
using NGA.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NGA.UI.Services
{
    public class TopicService : Service<Topic, DataContext>, ITopicService
    {
        public TopicService(DataContext context, ReadonlyDBContext<DataContext> readonlyDBContext) : base(context, readonlyDBContext)
        {
        }

        public async Task<PagedData<Topic>> GetLatestTopicsWithReplies(string key, int pageIndex)
        {
            var latestReplays = ReadonlyContext.Replays
                .GroupBy(r => r.Tid)
                .Select(g => new
                {
                    Tid = g.Key,
                    LatestUpdateTime = g.Max(r => r.UpdatedTime) // 获取每个 Tid 的最新更新时间
                })
                .OrderByDescending(x => x.LatestUpdateTime);

            var latestTopics = ReadonlyContext.Topics.Where(q => q.Title.Contains(key) || string.IsNullOrEmpty(key))
                       .Join(
                           latestReplays, // 关联最新的回复记录
                           topic => topic.Tid, // Topics 表的 Tid
                           replay => replay.Tid, // 最新回复记录的 Tid
                           (topic, replay) => new { Topic = topic, Replay = replay } // 投影结果
                       )
                       .OrderByDescending(x => x.Replay.LatestUpdateTime) // 按最新更新时间排序
                       .Select(x => x.Topic); // 选择 Topics 表的记录

            return await latestTopics.ToPagedAsync(pageIndex);
        }
    }
}
