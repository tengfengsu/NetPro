﻿using FreeSql.Internal.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XXX.Plugin.Web.Manager
{
    public interface IFreeSQLDemoService
    {
        Task<int> DeleteAsync(uint id);
        Task<string> GenerateSqlByLinq(DynamicFilterInfo dyfilter, SearchPageBase searchPageBase);
        Task<PagedList<User>> GraphQLAsync(DynamicFilterInfo dyfilter, SearchPageBase searchPageBase);
        Task<int> InsertAsync(UserInsertAo userInsertAo);
        Task<string> MultiFreeSqlAsync(string dbKey);
        Task<PagedList<User>> SearchJoinAsync(UserSearchAo search);
        bool Transaction();
        Task<int> UpdateAsync(UserUpdateAo user);
        Task<int> UpdatePatchAsync(uint id, uint age);
    }

    /// <summary>
    /// Freesql示例服务
    /// reference： https://github.com/dotnetcore/FreeSql/wiki/入门
    /// </summary>
    public class FreeSQLDemoService : IFreeSQLDemoService
    {
        private readonly ILogger<FreeSQLDemoService> _logger;
        private readonly IFreeSql _fsql;
        private readonly IMapper _mapper;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="fsql"></param>
        /// <param name="mapper"></param>
        public FreeSQLDemoService(ILogger<FreeSQLDemoService> logger
            , IFreeSql fsql
            , IMapper mapper)
        {
            _logger = logger;
            _fsql = fsql;
            _mapper = mapper;
        }

        /// <summary>
        /// 多库操作示例(切换数据库)
        /// </summary>
        /// <returns></returns>
        public async Task<string> MultiFreeSqlAsync(string dbKey)
        {
            var sqliteInstance = await _fsql.Select<User>().ToListAsync();
            _logger.LogInformation($"当前默认数据库实例连接字符串 {_fsql.Ado.ConnectionString}");
            //将当前库sqlite切换到mysql实例上，本方法后续操作都是基于"mysql"实例的操作            
            //_fsql.Change("mysql");
            if (!string.IsNullOrWhiteSpace(dbKey))
            {
                _fsql.Change(dbKey);
            }

            _logger.LogInformation($"执行切库后的数据库实例连接字符串 {_fsql.Ado.ConnectionString}");
            var mysqlInstance = await _fsql.Select<User>().ToListAsync();
            return _fsql.Ado.ConnectionString;
        }

        /// <summary>
        /// 新增示例
        /// </summary>
        /// <returns></returns>
        public async Task<int> InsertAsync(UserInsertAo userInsertAo)
        {
            //AO实体隐射为数据库DO实体
            var userEntity = _mapper.Map<UserInsertAo, User>(userInsertAo);

            // Insert方法多个重载，支持单对象、集合对象 
            var affrows = await _fsql.Insert(userEntity).ExecuteAffrowsAsync();
            _logger.LogInformation("新增成功");
            return affrows;
        }

        /// <summary>
        /// 根据id删除示例
        /// </summary>
        /// <returns></returns>
        public async Task<int> DeleteAsync(uint id)
        {
            var affrows = await _fsql.Delete<User>(id)
                .Where(x => x.Id == id)
                .ExecuteAffrowsAsync();
            _logger.LogError("删除成功 ");
            return affrows;
        }

        /// <summary>
        /// 更新单个值示例
        /// </summary>
        /// <param name="id"></param>
        /// <param name="age"></param>
        /// <returns></returns>
        public async Task<int> UpdatePatchAsync(uint id, uint age)
        {
            //UpdateDiy方法只能通过GetRepository获取到Repository对象才可使用
            var affrows = await _fsql.GetRepository<User>().UpdateDiy
                //.Set(s => new User { Age = age }) //将Age值覆盖
                .Set(s => s.Age + age)              //在数据库执行age数据的累加，原子操作
                .Where(x => x.Id == id)
                .ExecuteAffrowsAsync();

            _logger.LogError("更新单个值成功");
            return affrows;
        }

        /// <summary>
        /// 更新整个对象示例
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(UserUpdateAo user)
        {
            var userEntity = _mapper.Map<UserUpdateAo, User>(user);
            var affrows = await _fsql.GetRepository<User>()
                .UpdateAsync(userEntity);

            _logger.LogError("更新单个值成功");
            return affrows;
        }

        /// <summary>
        ///  多表关联查询查询示例
        ///  多表关联查询方式提供多种，具体以官方文档
        ///  reference：https://github.com/dotnetcore/FreeSql/wiki/Query-from-Multi-Tables
        ///  reference：https://github.com/dotnetcore/FreeSql/wiki/查询
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public async Task<PagedList<User>> SearchJoinAsync(UserSearchAo search)
        {
            //方式一：通过Repository间接操作
            var list = await _fsql.GetRepository<User>().Select.From<UserRelation, Company>((user, userRe, comp) => user
            //方式二：直接操作
            //var list = await _fsql.Select<User>().From<UserRelation, Company>((user, userRe, comp) => user
            .InnerJoin(u => u.Id == userRe.UserId).InnerJoin(a => userRe.CompanyId == comp.Id))
            .WhereIf(search.Age > 0, (a, s, d) => a.Age == search.Age)//查询条件
            .OrderByDescending((a, s, d) => a.Id)                   //排序：以User.Id 降序排序
            .Page(search.PageIndex, search.PageSize)            //分页
            .Count(out long totalCount)                     //返回结果总数
            .ToListAsync((u, r, c) => new User
            {
                Id = r.UserId,
                Age = u.Age,
                NickName = u.NickName
            });
            _logger.LogError("查询列表成功");
            var result = new PagedList<User>(list, search.PageIndex, search.PageSize, totalCount);
            return result;
        }

        /// <summary>
        ///  动态条件查询示例，类似graphql协议
        ///  reference：https://github.com/dotnetcore/FreeSql/wiki/查询
        /// </summary>
        /// <param name="dyfilter "></param>
        /// <param name="searchPageBase "></param>
        /// <returns></returns>
        public async Task<PagedList<User>> GraphQLAsync(DynamicFilterInfo dyfilter, SearchPageBase searchPageBase)
        {
            //前端构造查询条件的格式如下：
            dyfilter = JsonConvert
                .DeserializeObject<DynamicFilterInfo>(@"
                {
                  ""Logic"" : ""Or"",
                  ""Filters"" :
                  [
                    {
                      ""Field"" : ""Code"", ""Operator"" : ""NotContains"", ""Value"" : ""val1"", 
                      ""Filters"" : [{ ""Field"" : ""Name"", ""Operator"" : ""NotStartsWith"", ""Value"" : ""val2"" }]
                    },
                    {
                      ""Field"" : ""Parent.Code"", ""Operator"" : ""Equals"", ""Value"" : ""val11"",
                      ""Filters"" : [{ ""Field"" : ""Parent.Name"", ""Operator"" : ""Contains"", ""Value"" : ""val22"" }]
                    }
                  ]
                }");

            var list = _fsql.Select<User>().WhereDynamicFilter(dyfilter)
                 .Page(searchPageBase.PageIndex, searchPageBase.PageSize)//分页
                 .Count(out long totalCount)
                 .ToList();

            var result = new PagedList<User>(list, searchPageBase.PageIndex, searchPageBase.PageSize, totalCount);
            return result;
        }

        /// <summary>
        /// 通过linq生成执行的sql字符串示例
        /// 用途：根据生成sql检查问题优化程序
        /// </summary>
        /// <param name="dyfilter"></param>
        /// <param name="searchPageBase"></param>
        /// <returns></returns>
        public async Task<string> GenerateSqlByLinq(DynamicFilterInfo dyfilter, SearchPageBase searchPageBase)
        {
            //前端构造查询条件的格式如下：
            dyfilter = JsonConvert
                .DeserializeObject<DynamicFilterInfo>(@"
                {
                  ""Logic"" : ""Or"",
                  ""Filters"" :
                  [
                    {
                      ""Field"" : ""Code"", ""Operator"" : ""NotContains"", ""Value"" : ""val1"", 
                      ""Filters"" : [{ ""Field"" : ""Name"", ""Operator"" : ""NotStartsWith"", ""Value"" : ""val2"" }]
                    },
                    {
                      ""Field"" : ""Parent.Code"", ""Operator"" : ""Equals"", ""Value"" : ""val11"",
                      ""Filters"" : [{ ""Field"" : ""Parent.Name"", ""Operator"" : ""Contains"", ""Value"" : ""val22"" }]
                    }
                  ]
                }");

            var sqlString = _fsql.Select<User>().WhereDynamicFilter(dyfilter)
                  .Page(searchPageBase.PageIndex, searchPageBase.PageSize)//分页
                  .Count(out long totalCount)
                  .ToSql();//生成SQL

            _logger.LogInformation($"生成sql:{sqlString }");
            return sqlString;
        }

        /// <summary>
        /// 事务示例
        /// 另类事务 reference：https://github.com/dotnetcore/FreeSql/issues/322;
        /// 常规事务方式 reference：https://github.com/dotnetcore/FreeSql/wiki/事务;
        /// </summary>
        /// <returns></returns>
        public bool Transaction()
        {
            try
            {
                //此种事务只能使用同步方法，其他事务用法参考:https://github.com/dotnetcore/FreeSql/wiki/事务
                _fsql.Transaction(() =>
                {
                    //_fsql.Ado.TransactionCurrentThread //获得当前事务对象

                    #region 1、插入企业数据生成主键
                    var company = new Company
                    {
                        Name = "公司1",
                        Region = "SZ"
                    };
                    var affrows = _fsql.Insert<Company>(company).ExecuteAffrows();

                    if (affrows < 1)
                        throw new Exception("企业数据插入失败，回滚事务，事务退出");
                    #endregion

                    #region  2、插入用户数据生成主键

                    var user = new User
                    {
                        Name = "Leon",
                        Age = 28,
                        CompanyId = company.Id,
                        NickName = "xiaoliangge",
                        CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        Pwd = "密码就是不断提高"
                    };
                    affrows = _fsql.Insert<User>(user).ExecuteAffrows();

                    if (affrows < 1)
                        throw new Exception("用户数据插入失败，回滚事务，事务退出");
                    #endregion

                    #region 3、插入用户企业关系数据生成主键
                    var userRelation = new UserRelation
                    {
                        CompanyId = company.Id,
                        UserId = company.Id,
                    };
                    affrows = _fsql.Insert<UserRelation>(userRelation).ExecuteAffrows();

                    if (affrows < 1)
                        throw new Exception("用户企业关系表插入失败，回滚事务，事务退出");
                    #endregion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"事务执行失败");
                return false;
            }
            return true;
        }
    }
}
