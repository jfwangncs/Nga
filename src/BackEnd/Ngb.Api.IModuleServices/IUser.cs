using jfYu.Core.CPlatform;
using Ngb.Api.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ngb.Api.IModuleServices
{

    public interface IUserService : IServiceKey
    {

        Task<User> GetById(string id);

        Task<Dictionary<string, User>> GetListByUids(params string[] id);
    }
}
