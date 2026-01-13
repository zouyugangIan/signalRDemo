namespace SignalRDemo.Shared;

/// <summary>
/// 共享常量
/// </summary>
public static class HubConstants
{
    /// <summary>
    /// Chat Hub 的路由路径
    /// </summary>
    public const string ChatHubPath = "/hubs/chat";

    /// <summary>
    /// 默认服务器地址
    /// </summary>
    public const string DefaultServerUrl = "http://localhost:5072";

    /// <summary>
    /// 文件上传块大小 (32KB)
    /// </summary>
    public const int FileChunkSize = 32 * 1024;

    /// <summary>
    /// 监控数据流间隔 (毫秒)
    /// </summary>
    public const int MonitoringStreamIntervalMs = 1000;

    /// <summary>
    /// 重连延迟时间数组 (毫秒)
    /// </summary>
    public static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];
}
