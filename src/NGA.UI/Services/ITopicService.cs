using JfYu.Data.Model;
using JfYu.Data.Service;
using NGA.Models;
using NGA.Models.Entity;
using System.Threading.Tasks;

namespace NGA.UI.Services
{
    public interface ITopicService : IService<Topic, DataContext>
    {
        Task<PagedData<Topic>> GetLatestTopicsWithReplies(string key, int pageIndex);
    }
}
