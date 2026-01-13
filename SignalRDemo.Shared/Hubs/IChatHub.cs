namespace SignalRDemo.Shared.Hubs;

using SignalRDemo.Shared.DTOs;

/// <summary>
/// Chat Hub 服务端接口 - 定义客户端可调用的服务端方法
/// </summary>
public interface IChatHub
{
    /// <summary>
    /// 发送消息到所有客户端
    /// </summary>
    Task SendMessage(string user, string message);

    /// <summary>
    /// 发送消息到指定用户
    /// </summary>
    Task SendMessageToUser(string targetUser, string message);

    /// <summary>
    /// 获取即时房间列表
    /// </summary>
    Task<List<RoomInfo>> GetRooms();

    /// <summary>
    /// 创建房间
    /// </summary>
    Task<RoomInfo> CreateRoom(string roomName);

    /// <summary>
    /// 加入聊天房间
    /// </summary>
    Task<RoomInfo> JoinRoom(string roomName);

    /// <summary>
    /// 离开聊天房间
    /// </summary>
    Task LeaveRoom(string roomName);

    /// <summary>
    /// 发送消息到指定房间
    /// </summary>
    Task SendMessageToRoom(string roomName, string message);

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    Task<List<ConnectionStatus>> GetOnlineUsers();

    /// <summary>
    /// 开始监控数据流
    /// </summary>
    IAsyncEnumerable<MonitoringDataPoint> StreamMonitoringData(CancellationToken cancellationToken);

    /// <summary>
    /// 上传文件块
    /// </summary>
    Task<bool> UploadFileChunk(string fileName, byte[] chunk, int chunkIndex, int totalChunks);
}

/// <summary>
/// Chat Hub 客户端接口 - 定义服务端可调用的客户端方法
/// </summary>
public interface IChatHubClient
{
    /// <summary>
    /// 接收聊天消息
    /// </summary>
    Task ReceiveMessage(ChatMessage message);

    /// <summary>
    /// 接收系统通知
    /// </summary>
    Task ReceiveNotification(SystemNotification notification);

    /// <summary>
    /// 用户加入通知
    /// </summary>
    Task UserJoined(ConnectionStatus user);

    /// <summary>
    /// 用户离开通知
    /// </summary>
    Task UserLeft(ConnectionStatus user);

    /// <summary>
    /// 文件上传进度更新
    /// </summary>
    Task FileUploadProgressUpdated(FileUploadProgress progress);

    /// <summary>
    /// 接收监控数据点
    /// </summary>
    Task ReceiveMonitoringData(MonitoringDataPoint dataPoint);

    /// <summary>
    /// 接收私聊消息
    /// </summary>
    Task ReceivePrivateMessage(string fromUser, ChatMessage message);

    /// <summary>
    /// 用户输入状态
    /// </summary>
    Task UserTyping(string userName, bool isTyping);
}
