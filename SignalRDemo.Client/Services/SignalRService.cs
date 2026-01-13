using Microsoft.AspNetCore.SignalR.Client;
using SignalRDemo.Shared;
using SignalRDemo.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRDemo.Client.Services;

/// <summary>
/// SignalR 服务接口
/// </summary>
public interface ISignalRService : IAsyncDisposable
{
    /// <summary>
    /// 连接状态
    /// </summary>
    HubConnectionState ConnectionState { get; }

    /// <summary>
    /// 当前连接ID
    /// </summary>
    string? ConnectionId { get; }

    // 事件
    event Action<ChatMessage>? MessageReceived;
    event Action<SystemNotification>? NotificationReceived;
    event Action<ConnectionStatus>? UserJoined;
    event Action<ConnectionStatus>? UserLeft;
    event Action<FileUploadProgress>? FileUploadProgressUpdated;
    event Action<MonitoringDataPoint>? MonitoringDataReceived;
    event Action<HubConnectionState>? ConnectionStateChanged;
    event Action<Exception?>? Reconnecting;
    event Action<string?>? Reconnected;
    event Action<Exception?>? Closed;

    // 连接管理
    Task<bool> ConnectAsync(string serverUrl, string userName, CancellationToken cancellationToken = default);
    Task DisconnectAsync();

    // 消息发送
    Task SendMessageAsync(string message);
    Task SendMessageToUserAsync(string targetUser, string message);

    // 房间管理
    Task JoinRoomAsync(string roomName);
    Task LeaveRoomAsync(string roomName);
    Task SendMessageToRoomAsync(string roomName, string message);

    // 在线用户
    Task<List<ConnectionStatus>> GetOnlineUsersAsync();

    // 监控流
    IAsyncEnumerable<MonitoringDataPoint> StreamMonitoringDataAsync(CancellationToken cancellationToken);

    // 文件上传
    Task<bool> UploadFileAsync(string filePath, IProgress<FileUploadProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// SignalR 服务实现
/// </summary>
public class SignalRService : ISignalRService
{
    private HubConnection? _connection;
    private string _userName = string.Empty;

    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;
    public string? ConnectionId => _connection?.ConnectionId;

    // 事件
    public event Action<ChatMessage>? MessageReceived;
    public event Action<SystemNotification>? NotificationReceived;
    public event Action<ConnectionStatus>? UserJoined;
    public event Action<ConnectionStatus>? UserLeft;
    public event Action<FileUploadProgress>? FileUploadProgressUpdated;
    public event Action<MonitoringDataPoint>? MonitoringDataReceived;
    public event Action<HubConnectionState>? ConnectionStateChanged;
    public event Action<Exception?>? Reconnecting;
    public event Action<string?>? Reconnected;
    public event Action<Exception?>? Closed;

    public async Task<bool> ConnectAsync(string serverUrl, string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            _userName = userName;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl.TrimEnd('/')}{HubConstants.ChatHubPath}?user={Uri.EscapeDataString(userName)}", options =>
                {
                    // 配置 HTTP 选项
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            // 开发环境忽略 SSL 证书验证
                            clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                        }
                        return handler;
                    };
                })
                .WithAutomaticReconnect(HubConstants.ReconnectDelays)
                .Build();

            // 注册事件处理
            RegisterEventHandlers();

            await _connection.StartAsync(cancellationToken);
            ConnectionStateChanged?.Invoke(_connection.State);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void RegisterEventHandlers()
    {
        if (_connection == null) return;

        // 接收消息
        _connection.On<ChatMessage>("ReceiveMessage", message =>
            MessageReceived?.Invoke(message));

        // 接收通知
        _connection.On<SystemNotification>("ReceiveNotification", notification =>
            NotificationReceived?.Invoke(notification));

        // 用户加入
        _connection.On<ConnectionStatus>("UserJoined", user =>
            UserJoined?.Invoke(user));

        // 用户离开
        _connection.On<ConnectionStatus>("UserLeft", user =>
            UserLeft?.Invoke(user));

        // 文件上传进度
        _connection.On<FileUploadProgress>("FileUploadProgressUpdated", progress =>
            FileUploadProgressUpdated?.Invoke(progress));

        // 监控数据
        _connection.On<MonitoringDataPoint>("ReceiveMonitoringData", data =>
            MonitoringDataReceived?.Invoke(data));

        // 连接状态事件
        _connection.Reconnecting += error =>
        {
            ConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            Reconnecting?.Invoke(error);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            ConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            Reconnected?.Invoke(connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            Closed?.Invoke(error);
            return Task.CompletedTask;
        };
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
            ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessage", _userName, message);
        }
    }

    public async Task SendMessageToUserAsync(string targetUser, string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessageToUser", targetUser, message);
        }
    }

    public async Task JoinRoomAsync(string roomName)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinRoom", roomName);
        }
    }

    public async Task LeaveRoomAsync(string roomName)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveRoom", roomName);
        }
    }

    public async Task SendMessageToRoomAsync(string roomName, string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessageToRoom", roomName, message);
        }
    }

    public async Task<List<ConnectionStatus>> GetOnlineUsersAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            return await _connection.InvokeAsync<List<ConnectionStatus>>("GetOnlineUsers");
        }
        return [];
    }

    public async IAsyncEnumerable<MonitoringDataPoint> StreamMonitoringDataAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            yield break;
        }

        var stream = _connection.StreamAsync<MonitoringDataPoint>("StreamMonitoringData", cancellationToken);

        await foreach (var dataPoint in stream.WithCancellation(cancellationToken))
        {
            yield return dataPoint;
        }
    }

    public async Task<bool> UploadFileAsync(string filePath, IProgress<FileUploadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            return false;
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return false;
        }

        var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / HubConstants.FileChunkSize);
        var buffer = new byte[HubConstants.FileChunkSize];

        await using var stream = fileInfo.OpenRead();

        for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, HubConstants.FileChunkSize), cancellationToken);
            var chunk = bytesRead == HubConstants.FileChunkSize ? buffer : buffer[..bytesRead];

            var result = await _connection.InvokeAsync<bool>(
                "UploadFileChunk",
                fileInfo.Name,
                chunk,
                chunkIndex,
                totalChunks,
                cancellationToken);

            progress?.Report(new FileUploadProgress(
                fileInfo.Name,
                fileInfo.Length,
                (chunkIndex + 1) * HubConstants.FileChunkSize,
                (double)(chunkIndex + 1) / totalChunks * 100
            ));

            if (chunkIndex == totalChunks - 1)
            {
                return result;
            }
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        GC.SuppressFinalize(this);
    }
}
