using jfYu.Core.MongoDB;
using Ngb.Api.IModuleServices;
using Ngb.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NgbApi.ModuleServices
{


    public class UserService : IUserService
    {
        private readonly MongoDBUtil Mongo;      
        public UserService()
        {           
            Mongo = new MongoDBUtil();
        }
        public async Task<User> GetById(string id)
        {
            return await Mongo.QueryOneAsync<User>(q => q.Uid == id);
        }
        public async Task<Dictionary<string, User>> GetListByUids(params string[] id)
        {
            Dictionary<string, User> users = new Dictionary<string, User>();
            var result =new Dictionary<string, User>();// await Mongo.QueryListAsync<User>(q => id.Contains(q.Uid));
            //foreach (var item in result)
            //    users.Add(item.Uid, item);
            return users;

        }
    }


}
