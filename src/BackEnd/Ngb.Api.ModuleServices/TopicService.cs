using jfYu.Core.Common.Pagination;
using jfYu.Core.MongoDB;
using MongoDB.Driver;
using Ngb.Api.IModuleServices;
using Ngb.Api.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NgbApi.ModuleServices
{
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>()
        { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.Or(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.And(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
    public class TopicService : ITopicService
    {

        private readonly MongoDBUtil _mongo;
        private readonly PaginationUtil _paginationutil;
        public TopicService()
        {
            _mongo = new MongoDBUtil();
            _paginationutil = new PaginationUtil();
        }
        public async Task<PaginationInfo<Topic>> GetList(PaginationParm parm)
        {
            var source = _mongo.GetCollection<Topic>().Where(e => parm.Key == "" || e.Title.Contains(parm.Key) || e.Tid.Contains(parm.Key));
            if (parm.Condition.TryGetValue("uid", out string uid))
                source = source.Where(q => q.Uid.Equals(uid));
            return await _paginationutil.PagingAsync(source, parm);
        }

        public Topic GetByTid(string Tid)
        {
            var collection = _mongo.GetCollection<Topic>();
            return collection.First(q => q.Tid == Tid);
        }       
    }
}
