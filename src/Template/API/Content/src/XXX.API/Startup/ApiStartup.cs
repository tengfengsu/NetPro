﻿using IdGen;
using IdGen.DependencyInjection;

namespace XXX.API
{
    /// <summary>
    /// 自定义启动类
    /// </summary>
    public class ApiStartup : INetProStartup
    {
        /// <summary>
        /// 执行顺序
        /// </summary>
        public double Order { get; set; } = int.MaxValue;

        /// <summary>
        /// 服务注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="typeFinder"></param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration = null, ITypeFinder typeFinder = null)
        {
            //注册Id生成器
            //使用： var uniqueId = new IdGenerator(0).CreateId().ToString();
            services.AddIdGen(0, () => IdGeneratorOptions.Default);

            //批量注入(可正则匹配注入所有子程序集，也可在每个子程序集中独立注入)
            services.BatchInjection("^XXX.", "Service$"); //批量注入以XXX前缀的程序集，Service结尾的类           
        }

        /// <summary>
        /// 请求管道配置
        /// </summary>
        /// <param name="application"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
        {
        }
    }
}
