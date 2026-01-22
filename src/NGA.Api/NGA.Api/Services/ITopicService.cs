using JfYu.Data.Model;
using JfYu.Data.Service;
using NGA.Models;
using NGA.Models.Entity;
using WebApi.Model.Request;

namespace NGA.Api.Services
{
    public interface ITopicService : IService<Topic, DataContext>
    {
        Task<PagedData<Topic>> GetListAsync(QueryRequest query);
    }
}
