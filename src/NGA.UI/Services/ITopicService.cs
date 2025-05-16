using jfYu.Core.Data.Model;
using jfYu.Core.Data.Service;
using NGA.Models;
using System.Threading.Tasks;

namespace NGA.UI.Services
{
    public interface ITopicService : IService<Topic, DataContext>
    {
        Task<PagedData<Topic>> GetLatestTopicsWithReplies(string key, int pageIndex);
    }
}
