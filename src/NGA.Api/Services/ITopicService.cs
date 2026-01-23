using JfYu.Data.Model;
using JfYu.Data.Service;
using NGA.Api.Model.Request;
using NGA.Models;
using NGA.Models.Entity; 

namespace NGA.Api.Services
{
    public interface ITopicService : IService<Topic, DataContext>
    {
        Task<PagedData<Topic>> GetListAsync(QueryRequest query);
    }
}
