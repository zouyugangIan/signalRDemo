namespace SignalRDemo.Shared.DTOs;

/// <summary>
/// 聊天消息 DTO
/// </summary>
public record ChatMessage(
    string User,
    string Message,
    DateTime Timestamp
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
