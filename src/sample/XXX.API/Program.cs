var host = Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup("NetPro.Startup"));//环境变量方式叠加生效 ASPNETCORE_HOSTINGSTARTUPASSEMBLIES


host.Build().Run();
