using jfYu.Core.Common.Pagination;
using jfYu.Core.CPlatform;
using Ngb.Api.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ngb.Api.IModuleServices
{
    public interface IReplayService : IServiceKey
    {
        Task<PaginationInfo<Replay>> GetListByTid(string Tid, PaginationParm parm);
        Task<List<Replay>> GetListByPids(params string[] id);
        Task<Replay> GetByPid(string pid);

    }

}
