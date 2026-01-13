using Microsoft.AspNetCore.SignalR;
using SignalRDemo.Shared;
using SignalRDemo.Shared.DTOs;
using SignalRDemo.Shared.Hubs;
using SignalRDemo.Server.Services;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SignalRDemo.Server.Hubs;

/// <summary>
/// 聊天 Hub 实现
/// </summary>
public class ChatHub : Hub<IChatHubClient>, IChatHub
{
    // 在线用户字典 (ConnectionId -> UserName)
    private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

    // 文件上传缓存 (FileName -> chunks)
    private static readonly ConcurrentDictionary<string, SortedDictionary<int, byte[]>> _fileChunks = new();

    private readonly ILogger<ChatHub> _logger;
    private readonly SystemMonitorService _monitor;

    public ChatHub(ILogger<ChatHub> logger, SystemMonitorService monitor)
    {
        _logger = logger;
        _monitor = monitor;
    }

    #region 连接生命周期

    public override async Task OnConnectedAsync()
    {
        var userName = Context.GetHttpContext()?.Request.Query["user"].ToString() ?? $"User_{Context.ConnectionId[..8]}";
        _onlineUsers.TryAdd(Context.ConnectionId, userName);

        var connectionStatus = new ConnectionStatus(
            Context.ConnectionId,
            userName,
            true,
            DateTime.UtcNow
        );

        // 通知所有客户端有新用户加入
        await Clients.Others.UserJoined(connectionStatus);

        // 发送系统通知给当前用户
        await Clients.Caller.ReceiveNotification(new SystemNotification(
            "连接成功",
            $"欢迎 {userName}! 您已成功连接到服务器。",
            NotificationType.Success,
            DateTime.UtcNow
        ));

        _logger.LogInformation("用户 {UserName} 已连接, ConnectionId: {ConnectionId}", userName, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_onlineUsers.TryRemove(Context.ConnectionId, out var userName))
        {
            var connectionStatus = new ConnectionStatus(
                Context.ConnectionId,
                userName,
                false,
                DateTime.UtcNow
            );

            await Clients.All.UserLeft(connectionStatus);

            _logger.LogInformation("用户 {UserName} 已断开连接, ConnectionId: {ConnectionId}", userName, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "连接异常断开: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region 消息发送

    public async Task SendMessage(string user, string message)
    {
        var chatMessage = new ChatMessage(user, message, DateTime.UtcNow);
        await Clients.All.ReceiveMessage(chatMessage);

        _logger.LogInformation("消息已发送: {User} -> {Message}", user, message);
    }

    public async Task SendMessageToUser(string targetUser, string message)
    {
        var senderName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");

        // 查找目标用户的 ConnectionId
        var targetConnection = _onlineUsers.FirstOrDefault(x => x.Value == targetUser);

        if (targetConnection.Key != null)
        {
            var chatMessage = new ChatMessage(senderName, message, DateTime.UtcNow);
            // 发送私聊消息给目标用户
            await Clients.Client(targetConnection.Key).ReceivePrivateMessage(senderName, chatMessage);
            // 发送者也收到确认
            await Clients.Caller.ReceivePrivateMessage(targetUser, chatMessage);
        }
        else
        {
            await Clients.Caller.ReceiveNotification(new SystemNotification(
                "发送失败",
                $"用户 {targetUser} 不在线。",
                NotificationType.Warning,
                DateTime.UtcNow
            ));
        }
    }

    /// <summary>
    /// 发送输入状态
    /// </summary>
    public async Task SendTypingStatus(bool isTyping)
    {
        var userName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");
        await Clients.Others.UserTyping(userName, isTyping);
    }

    #endregion

    #region 房间管理

    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        var userName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");
        await Clients.Group(roomName).ReceiveNotification(new SystemNotification(
            "用户加入",
            $"{userName} 加入了房间 {roomName}",
            NotificationType.Info,
            DateTime.UtcNow
        ));

        _logger.LogInformation("用户 {UserName} 加入房间 {RoomName}", userName, roomName);
    }

    public async Task LeaveRoom(string roomName)
    {
        var userName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

        await Clients.Group(roomName).ReceiveNotification(new SystemNotification(
            "用户离开",
            $"{userName} 离开了房间 {roomName}",
            NotificationType.Info,
            DateTime.UtcNow
        ));

        _logger.LogInformation("用户 {UserName} 离开房间 {RoomName}", userName, roomName);
    }

    public async Task SendMessageToRoom(string roomName, string message)
    {
        var userName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");
        var chatMessage = new ChatMessage(userName, message, DateTime.UtcNow);

        await Clients.Group(roomName).ReceiveMessage(chatMessage);

        _logger.LogInformation("房间消息: {UserName} -> {RoomName}: {Message}", userName, roomName, message);
    }

    #endregion

    #region 在线用户

    public Task<List<ConnectionStatus>> GetOnlineUsers()
    {
        var users = _onlineUsers.Select(kv => new ConnectionStatus(
            kv.Key,
            kv.Value,
            true,
            DateTime.UtcNow
        )).ToList();

        return Task.FromResult(users);
    }

    #endregion

    #region 实时监控流

    public async IAsyncEnumerable<MonitoringDataPoint> StreamMonitoringData(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // 使用真实系统数据
            var dataPoint = new MonitoringDataPoint(
                DateTime.UtcNow,
                _monitor.GetCpuUsage(),
                _monitor.GetMemoryUsage(),
                _monitor.GetNetworkBytesReceivedPerSec(),
                _monitor.GetNetworkBytesSentPerSec()
            );

            yield return dataPoint;

            await Task.Delay(HubConstants.MonitoringStreamIntervalMs, cancellationToken);
        }
    }

    #endregion

    #region 文件上传

    public async Task<bool> UploadFileChunk(string fileName, byte[] chunk, int chunkIndex, int totalChunks)
    {
        var userName = _onlineUsers.GetValueOrDefault(Context.ConnectionId, "Unknown");
        var fileKey = $"{Context.ConnectionId}_{fileName}";

        // 获取或创建文件块字典
        var chunks = _fileChunks.GetOrAdd(fileKey, _ => new SortedDictionary<int, byte[]>());

        lock (chunks)
        {
            chunks[chunkIndex] = chunk;
        }

        // 计算进度
        var uploadedChunks = chunks.Count;
        var progress = new FileUploadProgress(
            fileName,
            totalChunks * HubConstants.FileChunkSize,
            uploadedChunks * HubConstants.FileChunkSize,
            (double)uploadedChunks / totalChunks * 100
        );

        await Clients.Caller.FileUploadProgressUpdated(progress);

        _logger.LogInformation("文件块上传: {FileName}, Chunk {ChunkIndex}/{TotalChunks}",
            fileName, chunkIndex + 1, totalChunks);

        // 检查是否所有块都已上传
        if (uploadedChunks == totalChunks)
        {
            // 可以在这里合并文件块并保存
            _fileChunks.TryRemove(fileKey, out _);

            await Clients.Caller.ReceiveNotification(new SystemNotification(
                "上传完成",
                $"文件 {fileName} 上传成功!",
                NotificationType.Success,
                DateTime.UtcNow
            ));

            _logger.LogInformation("文件上传完成: {FileName} by {UserName}", fileName, userName);
            return true;
        }

        return false;
    }

    #endregion
}
