using SignalRDemo.Server.Hubs;
using SignalRDemo.Server.Services;
using SignalRDemo.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SystemMonitorService>();
builder.Services.AddSingleton<RoomManager>();

// 配置 SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 20;
});

// 配置 CORS (允许 Avalonia 客户端连接)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// 启用静态文件 (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// 映射 SignalR Hub
app.MapHub<ChatHub>(HubConstants.ChatHubPath);

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

// API 端点: 获取服务器信息
app.MapGet("/api/info", () => Results.Ok(new
{
    ServerName = "SignalR Demo Server",
    Version = "1.0.0",
    SignalRHubPath = HubConstants.ChatHubPath,
    Timestamp = DateTime.UtcNow
}))
.WithName("GetServerInfo");

app.Run();
