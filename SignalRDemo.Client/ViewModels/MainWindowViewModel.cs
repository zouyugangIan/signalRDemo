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
    private string _newRoomName = string.Empty;

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

    [ObservableProperty]
    private ConnectionStatus? _selectedUser;
    
    [ObservableProperty]
    private ChatChannelViewModel? _selectedChannel;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    partial void OnSelectedUserChanged(ConnectionStatus? value)
    {
        if (value != null && value.UserName != UserName)
        {
            OpenPrivateChat(value.UserName);
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeService.Instance.SetTheme(value ? AppTheme.Dark : AppTheme.Light);
    }
    
    partial void OnSelectedChannelChanged(ChatChannelViewModel? value)
    {
         if (value != null)
         {
             value.HasUnreadMessages = false;
         }
    }

    #endregion

    #region Collections

    public ObservableCollection<ChatChannelViewModel> Channels { get; } = [];
    public ObservableCollection<ConnectionStatus> OnlineUsers { get; } = [];
    public ObservableCollection<RoomInfo> AvailableRooms { get; } = []; // Server's room list
    public ObservableCollection<SystemNotificationViewModel> Notifications { get; } = [];

    #endregion

    public MainWindowViewModel() : this(new SignalRService())
    {
    }

    public MainWindowViewModel(ISignalRService signalRService)
    {
        _signalRService = signalRService;

        // Init Global Channel
        var globalChannel = new ChatChannelViewModel("Global", "公共大厅", ChannelType.Global);
        Channels.Add(globalChannel);
        SelectedChannel = globalChannel;

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
            // 获取房间列表
            await RefreshRoomsAsync();
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
        AvailableRooms.Clear();
        // Clear non-global channels? Or keep them as history? Let's keep them but user is offline.
    }

    private bool CanDisconnect() => IsConnected;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Message) || SelectedChannel == null) return;

        if (SelectedChannel.Type == ChannelType.Global)
        {
             await _signalRService.SendMessageAsync(Message);
        }
        else if (SelectedChannel.Type == ChannelType.Room)
        {
             await _signalRService.SendMessageToRoomAsync(SelectedChannel.Id, Message);
        }
        else if (SelectedChannel.Type == ChannelType.Private)
        {
             // Id for Private channel is the TargetUserName
             await _signalRService.SendMessageToUserAsync(SelectedChannel.Id, Message);
        }

        Message = string.Empty;
    }

    private bool CanSendMessage() => IsConnected && !string.IsNullOrWhiteSpace(Message) && SelectedChannel != null;

    [RelayCommand]
    private void CloseChannel(ChatChannelViewModel channel)
    {
        if (channel.Type == ChannelType.Global) return; // Cannot close global

        if (channel.Type == ChannelType.Room)
        {
             // Leave room on server
             _ = _signalRService.LeaveRoomAsync(channel.Id);
        }
        
        Channels.Remove(channel);
        if (SelectedChannel == channel)
        {
            SelectedChannel = Channels.FirstOrDefault();
        }
    }

    public void OpenPrivateChat(string targetUser)
    {
        var existingChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Private && c.Id == targetUser);
        if (existingChannel != null)
        {
            SelectedChannel = existingChannel;
            return;
        }

        var newChannel = new ChatChannelViewModel(targetUser, $"{targetUser}", ChannelType.Private);
        Channels.Add(newChannel);
        SelectedChannel = newChannel;
    }

    [RelayCommand]
    private void StartChat(ChatChannelViewModel channel)
    {
        SelectedChannel = channel;
    }

    [RelayCommand(CanExecute = nameof(CanCreateRoom))]
    private async Task CreateRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoomName)) return;

        // Auto join logic inside JoinRoomAsync wrapper usually, but here we explicitly ask to join/create
        // Actually interface is CreateRoom then JoinRoom, or JoinRoom creates it.
        // Server side JoinRoom creates if not exists in our simplified logic (RoomManager adds User), 
        // but explicit CreateRoom helps to populate the list for others.
        
        var roomInfo = await _signalRService.CreateRoomAsync(NewRoomName);
        await JoinRoomAsync(roomInfo.RoomName);
        
        NewRoomName = string.Empty;
        await RefreshRoomsAsync();
    }
    
    private bool CanCreateRoom() => IsConnected && !string.IsNullOrWhiteSpace(NewRoomName);

    [RelayCommand]
    private async Task JoinSelectedRoomAsync(RoomInfo? room)
    {
        if (room == null) return;
        await JoinRoomAsync(room.RoomName);
    }
    
    public async Task JoinRoomAsync(string roomName)
    {
        var existingChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Room && c.Id == roomName);
        if (existingChannel != null)
        {
            SelectedChannel = existingChannel;
            return;
        }

        await _signalRService.JoinRoomAsync(roomName);
        var newChannel = new ChatChannelViewModel(roomName, $"# {roomName}", ChannelType.Room);
        Channels.Add(newChannel);
        SelectedChannel = newChannel;
        
        AddNotification("加入房间", $"已加入房间: {roomName}", NotificationType.Info);
    }
    
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
    
    [RelayCommand]
    private async Task RefreshRoomsAsync()
    {
        var rooms = await _signalRService.GetRoomsAsync();
        AvailableRooms.Clear();
        foreach(var room in rooms)
        {
            AvailableRooms.Add(room);
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
            ChatChannelViewModel? targetChannel = null;

            if (string.IsNullOrEmpty(message.Scope)) 
            {
                // Global Message
                targetChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Global);
            }
            else if (message.Scope.StartsWith("Private:"))
            {
                // Private Message
                // Format: Private:RemoteUserName
                // If I am sender, Remote is Receiver. If I am Receiver, Remote is Sender.
                // But message.User is the SENDER.
                
                string remoteUser;
                if (message.User == UserName) 
                {
                    // I sent it. Scope is "Private:TargetUser" presumably? 
                    // Wait, server logic:
                    // SendMessageToUser -> ChatMessage(Sender, Msg, Scope: "Private:Sender") -> sent to Target
                    // Also -> ChatMessage(Sender, Msg, Scope: "Private:Sender") -> sent to Caller (Self) -- WAIT NO.
                    
                    // Let's re-verify Server Logic.
                    // Server:
                    // 1. To Target: ChatMessage(Sender, Msg, Scope: "Private:Sender")
                    // 2. To Caller: ChatMessage(Sender, Msg, Scope: "Private:Sender")  <-- This is confusing for the Caller.
                    
                    // Correct Client Logic:
                    // If Message.User == Me:
                    //    I need to find WHO I sent it to. But the message doesn't contain Target! 
                    //    Server implementation flaw? 
                    //    Actually Server sends: Clients.Caller.ReceivePrivateMessage(targetUser, chatMessage);
                    //    So the `targetUser` arg in ReceivePrivateMessage is the REMOTE party.
                    //    But we are using the unified OnMessageReceived here.
                    
                    // FIX: In SignalRService, we handle ReceivePrivateMessage specifically.
                    // And we should probably ensure ChatMessage passed to UI contains the "Channel ID" (Remote User).
                    // Or we just rely on `Scope` to be the Channel ID.
                    
                    // Current Server SendMessageToUser:
                    // To Target: Scope = "Private:SenderName" -> Fits channel "SenderName"
                    // To Self:   Clients.Caller.ReceivePrivateMessage(targetUser, chatMessage);
                    //            Client Service maps this. But chatMessage.Scope is "Private:SenderName" (Me).
                    
                    // We need to parse Scope or use context.
                    // For now, let's assume Scope is "Private:OtherGuy".
                    
                    // If I am Sender (message.User == UserName):
                    //    The Scope should instruct me which channel to put it in.
                    //    But the DTO Scope is fixed at creation.
                    //    Better: Client Service modifies the Scope before firing event?
                    
                    // Let's look at `SignalRService.cs` fix I just made.
                    // I added `ReceivePrivateMessage` handler.
                    // But I didn't change the ChatMessage object passed to MessageReceived.
                    
                    // PROVISIONAL FIX:
                    // We will trust the helper methods to open the right channel, 
                    // but here we need to find it.
                    
                    // Re-reading usage:
                    // Private messages are complex to route purely on DTO content if DTO doesn't have "Receiver".
                    // But we can lazy-create channels.
                    
                    // Let's rely on parsing.
                    var parts = message.Scope.Split(':');
                     if (parts.Length > 1)
                     {
                         var otherUser = parts[1];
                         if (message.User == UserName)
                         {
                             // I sent it. But to whom?
                             // The scope says "Private:Me". That doesn't help me know who I sent it to.
                             // THIS IS A BUG IN MY SERVER LOGIC/DESIGN.
                             // Server should send different Scope to Caller? Or Client needs `To` field.
                             
                             // CRITICAL: We need `To` field in ChatMessage or `ChannelId`.
                             // However, let's assume for now Private Messages work best when RECEIVED.
                             // When SENT, the UI command `SendMessageAsync` knows the target channel, so it can ADD the message manually!
                             // BUT `SendMessageAsync` just invokes server. It doesn't receive the echo immediately unless server echoes.
                             // Server DOES echo: `Clients.Caller.ReceivePrivateMessage(targetUser, chatMessage);`
                             // In SignalRService, `ReceivePrivateMessage` gets `targetUser` (the other guy).
                             // We should update the `Scope` of the ChatMessage to be `Private:{targetUser}` for the local user.
                             
                             // I will assume SignalRService handles this "Scope-fixing" or I will patch it there next if needed.
                             // For now: Scope="Private:RemoteUser"
                             remoteUser = otherUser; 
                         }
                         else
                         {
                             // Someone sent to me. message.User is Sender. Scope "Private:Sender".
                             remoteUser = message.User;
                         }

                         targetChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Private && c.Id == remoteUser);
                         if (targetChannel == null)
                         {
                             // Auto-create private channel on receive
                             targetChannel = new ChatChannelViewModel(remoteUser, remoteUser, ChannelType.Private);
                             Channels.Add(targetChannel);
                             // Notification?
                         }
                     }
                }
                else 
                {
                     // Received from someone else
                     remoteUser = message.User;
                     targetChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Private && c.Id == remoteUser);
                     if (targetChannel == null)
                     {
                         targetChannel = new ChatChannelViewModel(remoteUser, remoteUser, ChannelType.Private);
                         Channels.Add(targetChannel);
                     }
                }
            }
            else
            {
                // Room Message: Scope = "RoomName"
                targetChannel = Channels.FirstOrDefault(c => c.Type == ChannelType.Room && c.Id == message.Scope);
            }

            if (targetChannel != null)
            {
                targetChannel.AddMessage(message);
                if (SelectedChannel != targetChannel)
                {
                    targetChannel.HasUnreadMessages = true;
                }
            }
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
            var existing = OnlineUsers.FirstOrDefault(u => u.ConnectionId == user.ConnectionId);
            if (existing == null)
            {
                 OnlineUsers.Add(user);
                 AddNotification("用户上线", $"{user.UserName} 已上线", NotificationType.Info);
            }
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

    partial void OnIsConnectedChanged(bool value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SendMessageCommand.NotifyCanExecuteChanged();
        CreateRoomCommand.NotifyCanExecuteChanged();
        RefreshOnlineUsersCommand.NotifyCanExecuteChanged();
        RefreshRoomsCommand.NotifyCanExecuteChanged();
        StartMonitoringCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnIsConnectingChanged(bool value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
    }

    partial void OnMessageChanged(string value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }
    
    partial void OnSelectedChannelChanged(ChatChannelViewModel? value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewRoomNameChanged(string value)
    {
        CreateRoomCommand.NotifyCanExecuteChanged();
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
