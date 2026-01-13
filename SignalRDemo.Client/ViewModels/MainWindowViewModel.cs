using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRDemo.Client.Services;
using SignalRDemo.Shared;
using SignalRDemo.Shared.DTOs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRDemo.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISignalRService _signalRService;
    private CancellationTokenSource? _monitoringCts;

    #region Observable Properties

    [ObservableProperty]
    private string _userName = $"User_{Random.Shared.Next(1000, 9999)}";

    [ObservableProperty]
    private string _serverUrl = HubConstants.DefaultServerUrl;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _roomName = "General";

    [ObservableProperty]
    private string _targetUser = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _connectionStatus = "未连接";

    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private double _memoryUsage;

    [ObservableProperty]
    private double _networkIn;

    [ObservableProperty]
    private double _networkOut;

    #endregion

    #region Collections

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];
    public ObservableCollection<ConnectionStatus> OnlineUsers { get; } = [];
    public ObservableCollection<SystemNotificationViewModel> Notifications { get; } = [];

    #endregion

    public MainWindowViewModel() : this(new SignalRService())
    {
    }

    public MainWindowViewModel(ISignalRService signalRService)
    {
        _signalRService = signalRService;

        // 订阅事件
        _signalRService.MessageReceived += OnMessageReceived;
        _signalRService.NotificationReceived += OnNotificationReceived;
        _signalRService.UserJoined += OnUserJoined;
        _signalRService.UserLeft += OnUserLeft;
        _signalRService.ConnectionStateChanged += OnConnectionStateChanged;
        _signalRService.Reconnecting += OnReconnecting;
        _signalRService.Reconnected += OnReconnected;
        _signalRService.Closed += OnClosed;
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        IsConnecting = true;
        ConnectionStatus = "正在连接...";

        var success = await _signalRService.ConnectAsync(ServerUrl, UserName);

        if (success)
        {
            IsConnected = true;
            ConnectionStatus = "已连接";

            // 获取在线用户列表
            await RefreshOnlineUsersAsync();
        }
        else
        {
            ConnectionStatus = "连接失败";
            AddNotification("连接失败", "无法连接到服务器，请检查服务器地址。", NotificationType.Error);
        }

        IsConnecting = false;
    }

    private bool CanConnect() => !IsConnected && !IsConnecting && !string.IsNullOrWhiteSpace(ServerUrl) && !string.IsNullOrWhiteSpace(UserName);

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        await StopMonitoringInternalAsync();
        await _signalRService.DisconnectAsync();
        IsConnected = false;
        ConnectionStatus = "已断开";
        OnlineUsers.Clear();
    }

    private bool CanDisconnect() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Message)) return;

        await _signalRService.SendMessageAsync(Message);
        Message = string.Empty;
    }

    private bool CanSendMessage() => IsConnected && !string.IsNullOrWhiteSpace(Message);

    [RelayCommand(CanExecute = nameof(CanSendPrivateMessage))]
    private async Task SendPrivateMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Message) || string.IsNullOrWhiteSpace(TargetUser)) return;

        await _signalRService.SendMessageToUserAsync(TargetUser, Message);
        Message = string.Empty;
    }

    private bool CanSendPrivateMessage() => IsConnected && !string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(TargetUser);

    [RelayCommand(CanExecute = nameof(CanJoinRoom))]
    private async Task JoinRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(RoomName)) return;

        await _signalRService.JoinRoomAsync(RoomName);
        AddNotification("加入房间", $"已加入房间: {RoomName}", NotificationType.Info);
    }

    private bool CanJoinRoom() => IsConnected && !string.IsNullOrWhiteSpace(RoomName);

    [RelayCommand(CanExecute = nameof(CanLeaveRoom))]
    private async Task LeaveRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(RoomName)) return;

        await _signalRService.LeaveRoomAsync(RoomName);
        AddNotification("离开房间", $"已离开房间: {RoomName}", NotificationType.Info);
    }

    private bool CanLeaveRoom() => IsConnected && !string.IsNullOrWhiteSpace(RoomName);

    [RelayCommand(CanExecute = nameof(CanSendRoomMessage))]
    private async Task SendRoomMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Message) || string.IsNullOrWhiteSpace(RoomName)) return;

        await _signalRService.SendMessageToRoomAsync(RoomName, Message);
        Message = string.Empty;
    }

    private bool CanSendRoomMessage() => IsConnected && !string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(RoomName);

    [RelayCommand(CanExecute = nameof(CanRefreshUsers))]
    private async Task RefreshOnlineUsersAsync()
    {
        var users = await _signalRService.GetOnlineUsersAsync();
        OnlineUsers.Clear();
        foreach (var user in users)
        {
            OnlineUsers.Add(user);
        }
    }

    private bool CanRefreshUsers() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanStartMonitoring))]
    private async Task StartMonitoringAsync()
    {
        if (IsMonitoring) return;

        IsMonitoring = true;
        _monitoringCts = new CancellationTokenSource();

        try
        {
            await foreach (var dataPoint in _signalRService.StreamMonitoringDataAsync(_monitoringCts.Token))
            {
                CpuUsage = dataPoint.CpuUsage;
                MemoryUsage = dataPoint.MemoryUsage;
                NetworkIn = dataPoint.NetworkIn;
                NetworkOut = dataPoint.NetworkOut;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        finally
        {
            IsMonitoring = false;
        }
    }

    private bool CanStartMonitoring() => IsConnected && !IsMonitoring;

    [RelayCommand(CanExecute = nameof(CanStopMonitoring))]
    private async Task StopMonitoringAsync()
    {
        await StopMonitoringInternalAsync();
    }

    private bool CanStopMonitoring() => IsMonitoring;

    private async Task StopMonitoringInternalAsync()
    {
        if (_monitoringCts != null)
        {
            await _monitoringCts.CancelAsync();
            _monitoringCts.Dispose();
            _monitoringCts = null;
        }
        IsMonitoring = false;
    }

    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
    }

    [RelayCommand]
    private void ClearNotifications()
    {
        Notifications.Clear();
    }

    #endregion

    #region Event Handlers

    private void OnMessageReceived(ChatMessage message)
    {
        // 确保在 UI 线程执行
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Messages.Add(new ChatMessageViewModel(message));
        });
    }

    private void OnNotificationReceived(SystemNotification notification)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, new SystemNotificationViewModel(notification));
        });
    }

    private void OnUserJoined(ConnectionStatus user)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OnlineUsers.Add(user);
            AddNotification("用户上线", $"{user.UserName} 已上线", NotificationType.Info);
        });
    }

    private void OnUserLeft(ConnectionStatus user)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var existingUser = OnlineUsers.FirstOrDefault(u => u.ConnectionId == user.ConnectionId);
            if (existingUser != null)
            {
                OnlineUsers.Remove(existingUser);
            }
            AddNotification("用户离线", $"{user.UserName} 已离线", NotificationType.Info);
        });
    }

    private void OnConnectionStateChanged(HubConnectionState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsConnected = state == HubConnectionState.Connected;
            ConnectionStatus = state switch
            {
                HubConnectionState.Connected => "已连接",
                HubConnectionState.Connecting => "正在连接...",
                HubConnectionState.Reconnecting => "正在重连...",
                HubConnectionState.Disconnected => "已断开",
                _ => "未知状态"
            };
        });
    }

    private void OnReconnecting(Exception? error)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = "正在重连...";
            AddNotification("连接断开", "正在尝试重新连接...", NotificationType.Warning);
        });
    }

    private void OnReconnected(string? connectionId)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = "已重连";
            AddNotification("重连成功", "已成功重新连接到服务器", NotificationType.Success);
        });
    }

    private void OnClosed(Exception? error)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsConnected = false;
            ConnectionStatus = "连接已关闭";
            if (error != null)
            {
                AddNotification("连接关闭", $"连接异常关闭: {error.Message}", NotificationType.Error);
            }
        });
    }

    private void AddNotification(string title, string content, NotificationType type)
    {
        Notifications.Insert(0, new SystemNotificationViewModel(new SystemNotification(title, content, type, DateTime.UtcNow)));
    }

    #endregion

    // 通知 CanExecute 变化
    partial void OnIsConnectedChanged(bool value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendMessageCommand.NotifyCanExecuteChanged();
        SendPrivateMessageCommand.NotifyCanExecuteChanged();
        JoinRoomCommand.NotifyCanExecuteChanged();
        LeaveRoomCommand.NotifyCanExecuteChanged();
        SendRoomMessageCommand.NotifyCanExecuteChanged();
        RefreshOnlineUsersCommand.NotifyCanExecuteChanged();
        StartMonitoringCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsConnectingChanged(bool value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
    }

    partial void OnMessageChanged(string value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
        SendPrivateMessageCommand.NotifyCanExecuteChanged();
        SendRoomMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsMonitoringChanged(bool value)
    {
        StartMonitoringCommand.NotifyCanExecuteChanged();
        StopMonitoringCommand.NotifyCanExecuteChanged();
    }
}

/// <summary>
/// 聊天消息 ViewModel
/// </summary>
public partial class ChatMessageViewModel : ObservableObject
{
    public ChatMessage Message { get; }

    public string User => Message.User;
    public string Content => Message.Message;
    public string Time => Message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public string DisplayText => $"[{Time}] {User}: {Content}";

    public ChatMessageViewModel(ChatMessage message)
    {
        Message = message;
    }
}

/// <summary>
/// 系统通知 ViewModel
/// </summary>
public partial class SystemNotificationViewModel : ObservableObject
{
    public SystemNotification Notification { get; }

    public string Title => Notification.Title;
    public string Content => Notification.Content;
    public string Time => Notification.Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public NotificationType Type => Notification.Type;
    public string TypeColor => Type switch
    {
        NotificationType.Info => "#3498db",
        NotificationType.Warning => "#f39c12",
        NotificationType.Error => "#e74c3c",
        NotificationType.Success => "#2ecc71",
        _ => "#95a5a6"
    };

    public SystemNotificationViewModel(SystemNotification notification)
    {
        Notification = notification;
    }
}
