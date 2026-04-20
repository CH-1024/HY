using HY.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104_857_600;
});

// 创建 Startup 实例并配置服务
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// 配置 HTTP 请求管道
startup.Configure(app, app.Environment);

app.Run();
