using jfYu.Core.Common.Pagination;
using jfYu.Core.CPlatform;
using Ngb.Api.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ngb.Api.IModuleServices
{
    public interface ITopicService : IServiceKey
    {
        Task<PaginationInfo<Topic>> GetList(PaginationParm parm);
        Topic GetByTid(string Tid);

    }
}
