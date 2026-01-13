namespace SignalRDemo.Shared.DTOs;

/// <summary>
/// 聊天消息 DTO
/// </summary>
/// <param name="User">发送者</param>
/// <param name="Message">消息内容</param>
/// <param name="Timestamp">发送时间</param>
/// <param name="Scope">消息作用域 (null/empty = Global, 否则为房间名)</param>
public record ChatMessage(
    string User,
    string Message,
    DateTime Timestamp,
    string? Scope = null
);


/// <summary>
/// 系统通知 DTO
/// </summary>
public record SystemNotification(
    string Title,
    string Content,
    NotificationType Type,
    DateTime Timestamp
);

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// 连接状态 DTO
/// </summary>
public record ConnectionStatus(
    string ConnectionId,
    string UserName,
    bool IsConnected,
    DateTime LastSeen
);

/// <summary>
/// 文件上传进度 DTO
/// </summary>
public record FileUploadProgress(
    string FileName,
    long TotalBytes,
    long UploadedBytes,
    double PercentComplete
);

/// <summary>
/// 监控数据点 DTO (用于实时流)
/// </summary>
public record MonitoringDataPoint(
    DateTime Timestamp,
    double CpuUsage,
    double MemoryUsage,
    double NetworkIn,
    double NetworkOut
);
