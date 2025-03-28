using jfYu.Core.Common.Pagination;
using jfYu.Core.MongoDB;
using Ngb.Api.IModuleServices;
using Ngb.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NgbApi.ModuleServices
{
    public class ReplayService : IReplayService
    {
        private readonly MongoDBUtil Mongo;   
        public ReplayService()
        {          
            Mongo = new MongoDBUtil();
        }
        public async Task<PaginationInfo<Replay>> GetListByTid(string Tid, PaginationParm parm)
        {


            //bool onlyImage = false;
            //bool onlyAuthor = false;
            //String uid = "-1";
            //switch (parm.Condition["TopicType"])
            //{
            //    case "Image":
            //        onlyImage = true;
            //        break;
            //    case "Author":
            //        uid = Mongo.QueryOne<Topic>(q => q.Tid == Tid).Uid;
            //        onlyAuthor = true;

            //        break;
            //}
            //var result = await Mongo.GetPageAscAsync<Replay>(q => q.Tid == Tid && (!onlyImage || q.Content.Contains("<img")) && (!onlyAuthor || q.Uid == uid),
            //e => e.Sort, parm);
            return null;

        }
        public async Task<List<Replay>> GetListByPids(params string[] id)
        {
            //List<Replay> items = new List<Replay>();
            //var result = await Mongo.QueryListAsync<Replay>(q => id.Contains(q.Pid));
            //foreach (var item in result)
            //    items.Add(item);
            return null;
        }
        /// <summary>
        /// 动态linq组合
        /// </summary>

        public async Task<Replay> GetByPid(string pid)
        {
            return await Mongo.QueryOneAsync<Replay>(q => q.Pid == pid);

        }



    }
}
