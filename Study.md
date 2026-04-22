# SignalRDemo 深度学习手册

更新日期：2026-04-23

本项目不是 WPF 项目。桌面客户端使用的是 Avalonia。Avalonia 和 WPF 都使用 XAML、Binding、MVVM 等思想，但运行时、控件库、跨平台能力和工具链不同。

## 0. 你应该先建立的总图

项目分为三层：

- `SignalRDemo.Server`：ASP.NET Core + SignalR 服务端，同时托管 `wwwroot/index.html` 网页客户端。
- `SignalRDemo.Client`：Avalonia 桌面客户端，采用 MVVM、CommunityToolkit.Mvvm、SignalR .NET Client。
- `SignalRDemo.Shared`：服务端和客户端共享的 DTO、Hub 常量、Hub 调用契约。

最重要的一条链路：

```text
Avalonia UI 输入
-> MainWindowViewModel 命令
-> SignalRService.InvokeAsync(...)
-> ChatHub 方法
-> Clients.All / Others / Caller / Group / Client 推送
-> SignalRService.On(...) 监听
-> MainWindowViewModel 更新 ObservableCollection
-> Avalonia Binding 刷新界面
```

如果你能不看代码画出这条链路，并说出每一步对应的文件，这个项目你已经理解了 40%。

## 1. 最新资料核对摘要

本节根据官方文档重新校准了学习重点，避免只按 AI 生成代码的表面结构理解项目。

- SignalR Hub 通过 `builder.Services.AddSignalR()` 注册服务，通过 `app.MapHub<T>(path)` 映射端点。本项目对应 `SignalRDemo.Server/Program.cs`。
- Hub 是 transient。不要把连接状态放在 Hub 实例字段中；如果要跨调用保存状态，应该放到外部服务、数据库、缓存或 `Context.Items` 等合适位置。本项目使用 `static ConcurrentDictionary` 和 Singleton `RoomManager`，这是 Demo 级做法。
- `Hub<TClient>` 可以让服务端调用客户端方法时获得编译期检查。本项目的 `ChatHub : Hub<IChatHubClient>` 就是这个模式。
- SignalR 组适合聊天室。组成员关系保存在内存中，服务重启不保留，连接重建后也需要重新加入；组不是权限系统。
- SignalR .NET Client 使用 `HubConnectionBuilder` 建立连接，用 `connection.On<T>(...)` 注册服务端推送回调，用 `InvokeAsync(...)` 调用 Hub 方法，用 `WithAutomaticReconnect(...)` 配置自动重连。
- Server-to-client streaming 可以用 `IAsyncEnumerable<T>`；客户端用 `StreamAsync<T>` 消费。本项目的系统监控就是这个模式。
- Avalonia 使用单 UI 线程模型。后台线程不能直接改 UI，也不能随意修改已经绑定到 UI 的 `ObservableCollection`；需要回到 `Dispatcher.UIThread`。
- Avalonia Binding 是 View 和 ViewModel 的连接层。`TextBox.Text` 这类可编辑属性通常是双向绑定，`TextBlock.Text` 通常是单向绑定。
- Avalonia 编译绑定需要 `x:DataType`，项目文件中已经启用 `AvaloniaUseCompiledBindingsByDefault`。这能把部分绑定错误提前到编译期。
- CommunityToolkit.Mvvm 的 `[ObservableProperty]` 和 `[RelayCommand]` 是源生成器，不是运行时魔法。它们生成属性通知、命令对象、`NotifyCanExecuteChanged()` 等样板代码。

## 2. 运行和观察

先运行，不要先看完整代码。

```zsh
dotnet restore SignalRDemo.slnx
dotnet run --project SignalRDemo.Server --launch-profile http
```

另开终端：

```zsh
dotnet run --project SignalRDemo.Client
```

再打开网页客户端：

```zsh
open http://127.0.0.1:5072
```

观察顺序：

1. 一个 Avalonia 客户端连接，确认服务端输出连接日志。
2. 网页客户端再连接，观察在线用户变化。
3. Avalonia 发全局消息，看网页端是否收到。
4. 加入 `General` 房间，发送房间消息。
5. 点击右侧在线用户发起私聊，观察私聊路由是否符合预期。
6. 开启监控流，观察 CPU、内存、网络值。

完成标准：

- 你能说出哪些功能是 REST：`/health`、`/api/info`。
- 你能说出哪些功能是 SignalR：聊天、房间、私聊、在线用户、监控流、文件上传进度。
- 你能描述服务端和客户端各自常驻在哪里。

## 3. 第一遍：只追一条全局消息

这是本项目最重要的学习路径。

### 3.1 UI 触发命令

文件：`SignalRDemo.Client/Views/MainWindow.axaml`

关注：

- `TextBox Text="{Binding Message}"`
- `Button Command="{Binding SendMessageCommand}"`
- `KeyBinding Gesture="Enter" Command="{Binding SendMessageCommand}"`

含义：

- 输入框绑定到 `MainWindowViewModel.Message`。
- 点击按钮或按回车触发 `SendMessageCommand`。
- 这个命令不是手写属性，而是由 `[RelayCommand]` 从 `SendMessageAsync` 生成。

### 3.2 ViewModel 判断当前频道

文件：`SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`

关注：

- `SendMessageAsync`
- `SelectedChannel.Type`
- `ChannelType.Global`
- `ChannelType.Room`
- `ChannelType.Private`

这个方法是客户端业务路由中心。它不应该直接知道 SignalR 的底层细节，只应该调用 `ISignalRService`。

### 3.3 SignalRService 调 Hub

文件：`SignalRDemo.Client/Services/SignalRService.cs`

关注：

- `ConnectAsync`
- `HubConnectionBuilder`
- `WithUrl`
- `WithAutomaticReconnect`
- `InvokeAsync("SendMessage", _userName, message)`

这里是客户端网络层。它把 ViewModel 的“发送消息”转成 SignalR Hub 调用。

### 3.4 ChatHub 广播

文件：`SignalRDemo.Server/Hubs/ChatHub.cs`

关注：

- `SendMessage(string user, string message)`
- `new ChatMessage(user, message, DateTime.UtcNow)`
- `Clients.All.ReceiveMessage(chatMessage)`

`Clients.All` 代表给所有已连接客户端推送，包括发送者自己。

### 3.5 客户端监听推送

文件：`SignalRDemo.Client/Services/SignalRService.cs`

关注：

- `_connection.On<ChatMessage>("ReceiveMessage", ...)`
- `MessageReceived?.Invoke(message)`

服务端方法名和客户端监听名必须匹配。这个项目服务端用强类型 Hub 调用 `ReceiveMessage`，客户端用字符串注册 `ReceiveMessage`。

### 3.6 ViewModel 更新 UI 状态

文件：`SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`

关注：

- `OnMessageReceived`
- `Dispatcher.UIThread.Post`
- `Channels.FirstOrDefault(...)`
- `targetChannel.AddMessage(message)`
- `HasUnreadMessages`

关键点：

- SignalR 回调不应该假设自己在 UI 线程。
- `ObservableCollection` 绑定到 UI 后，集合修改要在 UI 线程执行。
- UI 本身没有主动刷新；它响应属性变化和集合变化。

完成标准：

- 能画出“UI -> ViewModel -> Service -> Hub -> Service -> ViewModel -> UI”的链路。
- 能解释 `InvokeAsync` 和 `On` 分别负责什么。
- 能解释为什么收到消息后要用 `Dispatcher.UIThread.Post`。

## 4. 服务端学习重点

### 4.1 Program.cs：ASP.NET Core 最小托管模型

文件：`SignalRDemo.Server/Program.cs`

你要能解释：

- `WebApplication.CreateBuilder(args)`：创建主机和服务容器。
- `builder.Services.AddOpenApi()`：开发期 OpenAPI。
- `AddSingleton<SystemMonitorService>()`：系统监控服务单例。
- `AddSingleton<RoomManager>()`：房间状态管理单例。
- `AddSignalR(...)`：注册 SignalR 服务和配置。
- `AddCors(...)`：跨域策略。
- `app.UseHttpsRedirection()`：HTTP 到 HTTPS 重定向。
- `app.UseCors("AllowAll")`：启用 CORS。
- `UseDefaultFiles()` + `UseStaticFiles()`：托管 `wwwroot/index.html`。
- `app.MapHub<ChatHub>(HubConstants.ChatHubPath)`：映射 Hub。
- `MapGet("/health")`、`MapGet("/api/info")`：Minimal API。

面试表达：

> 这个服务端采用 ASP.NET Core 最小托管模型。服务注册阶段把 SignalR、房间管理和监控服务放进 DI；请求管线阶段启用 HTTPS、CORS、静态文件，然后把 `ChatHub` 映射到 `/hubs/chat`。普通 HTTP 接口只负责健康检查和服务器信息，实时交互都走 SignalR Hub。

### 4.2 ChatHub：实时通信入口

文件：`SignalRDemo.Server/Hubs/ChatHub.cs`

Hub 方法分组：

- 连接生命周期：`OnConnectedAsync`、`OnDisconnectedAsync`
- 全局消息：`SendMessage`
- 私聊：`SendMessageToUser`
- 输入状态：`SendTypingStatus`
- 房间：`GetRooms`、`CreateRoom`、`JoinRoom`、`LeaveRoom`、`SendMessageToRoom`
- 在线用户：`GetOnlineUsers`
- 监控流：`StreamMonitoringData`
- 文件上传：`UploadFileChunk`

你要特别注意：

- `Context.ConnectionId` 是连接级 ID，不是用户 ID。
- 当前用户名来自 query string：`?user=...`，这不可信。
- `_onlineUsers` 是静态内存字典，Demo 可以，生产不行。
- Hub 是 transient，不能用实例字段保存在线用户。
- 服务端当前信任客户端传入的 `SendMessage(string user, ...)`，这也是安全问题。

### 4.3 Clients 目标对象

在本项目里的含义：

- `Clients.All`：所有连接。用于全局聊天、全员离线通知。
- `Clients.Others`：除调用者以外的所有连接。用于“某用户加入”通知。
- `Clients.Caller`：当前调用 Hub 方法的连接。用于连接成功通知、发送失败通知、上传进度。
- `Clients.Group(roomName)`：某个房间组。用于房间消息和房间通知。
- `Clients.Client(connectionId)`：指定连接。当前私聊就是通过目标用户查到连接 ID 后调用。

更生产化的写法：

- 对用户发消息优先考虑 `Clients.User(userId)`，前提是认证系统能提供稳定唯一的 `UserIdentifier`。
- 对聊天室发消息用 `Clients.Group(roomId)`，但授权不能只靠 group。

### 4.4 RoomManager：内存房间状态

文件：`SignalRDemo.Server/Services/RoomManager.cs`

当前结构：

- `_rooms`：`RoomName -> RoomInfo`
- `_roomUsers`：`RoomName -> HashSet<ConnectionId>`
- `_userRooms`：`ConnectionId -> HashSet<RoomName>`

为什么用 `ConcurrentDictionary`：

- 多个客户端会并发连接、断开、加入房间、发送消息。
- 字典的增删查需要线程安全。

还存在的并发风险：

- `ConcurrentDictionary` 只保护字典本身，不保护字典值里的 `HashSet`。
- 代码对 `HashSet` 使用 `lock` 是必要的，但 `UpdateRoomCount` 里读取 `users.Count` 没有锁，读写仍可能竞争。
- `RoomInfo.UserCount` 是可变属性，多线程写入也没有统一保护。
- 多服务器部署时，每台服务器内存各算各的，房间人数会失真。

更稳的方向：

- 单机 Demo：用一个专门的 `lock` 或不可变集合简化状态。
- 生产：房间事实存数据库，在线连接映射放 Redis，SignalR 组只作为推送通道。

### 4.5 SystemMonitorService：监控数据

文件：`SignalRDemo.Server/Services/SystemMonitorService.cs`

当前实现重点：

- CPU 是服务端进程级 CPU，不是整机 CPU。
- Linux 内存从 `/proc/meminfo` 读取。
- Windows 内存用当前进程工作集近似。
- 网络统计只实现了 Linux 的 `/sys/class/net`。
- macOS 下内存和网络大概率长期为 0。

学习时要能说清楚：

> UI 显示的是服务端采样数据，不是客户端机器数据。当前实现有平台差异，macOS 不完整。商业化前要明确指标语义，是进程级还是整机级，并补齐 macOS 实现或改用跨平台监控库。

## 5. 客户端学习重点：Avalonia + MVVM

### 5.1 启动链路

文件：

- `SignalRDemo.Client/Program.cs`
- `SignalRDemo.Client/App.axaml`
- `SignalRDemo.Client/App.axaml.cs`
- `SignalRDemo.Client/Views/MainWindow.axaml`
- `SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`

启动顺序：

```text
Program.Main
-> BuildAvaloniaApp()
-> StartWithClassicDesktopLifetime(args)
-> App.Initialize()
-> AvaloniaXamlLoader.Load(this)
-> ThemeService.Initialize(...)
-> App.OnFrameworkInitializationCompleted()
-> new MainWindow { DataContext = new MainWindowViewModel() }
-> MainWindow.axaml 里的 Binding 开始解析 DataContext
```

你要记住：

- `MainWindow.axaml` 是 View。
- `MainWindowViewModel` 是 ViewModel。
- `DataContext` 把 View 和 ViewModel 连起来。
- View 通过 Binding 和 Command 使用 ViewModel。

### 5.2 和 WinForms 的差异

WinForms 常见写法：

```text
button.Click += ...
textBox.Text = ...
listBox.Items.Add(...)
```

这个项目的写法：

```text
Button.Command -> ViewModel RelayCommand
TextBox.Text <-> ViewModel property
ItemsControl.ItemsSource -> ObservableCollection
```

本质差异：

- WinForms 偏事件驱动，逻辑容易写在窗体 code-behind。
- Avalonia MVVM 偏状态驱动，View 只描述布局和绑定，逻辑放在 ViewModel。
- WinForms 中控件本身经常是状态源；MVVM 中 ViewModel 才是状态源。
- MVVM 更容易单元测试，因为 ViewModel 不需要启动真实窗口。

面试表达：

> 在 WinForms 中，我通常直接响应控件事件并操作控件；在 Avalonia MVVM 中，View 不直接处理业务逻辑，而是绑定到 ViewModel 暴露的属性、集合和命令。用户输入改变 ViewModel 状态，命令调用服务，服务回调再更新 ViewModel，最后由 Binding 刷新 UI。

### 5.3 Binding：UI 和 ViewModel 的连接

文件：`SignalRDemo.Client/Views/MainWindow.axaml`

典型绑定：

- `Text="{Binding ServerUrl}"`：服务器地址输入。
- `Text="{Binding UserName}"`：用户名输入。
- `IsEnabled="{Binding !IsConnected}"`：连接后禁用输入。
- `Command="{Binding ConnectCommand}"`：按钮绑定命令。
- `ItemsSource="{Binding Channels}"`：频道列表。
- `ItemsSource="{Binding SelectedChannel.Messages}"`：当前频道消息。

项目文件启用了：

```xml
<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
```

所以 `MainWindow.axaml` 顶部的：

```xml
x:DataType="vm:MainWindowViewModel"
```

很重要。它让 Avalonia 能对很多绑定做编译期检查，而不是运行时才发现属性名错了。

### 5.4 ObservableProperty

文件：`SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`

例如：

```csharp
[ObservableProperty]
private string _message = string.Empty;
```

CommunityToolkit.Mvvm 会生成类似：

```csharp
public string Message
{
    get => _message;
    set => SetProperty(ref _message, value);
}
```

它还会支持 partial hook，例如项目里的：

```csharp
partial void OnMessageChanged(string value)
{
    SendMessageCommand.NotifyCanExecuteChanged();
}
```

这表示：当 `Message` 改变时，通知发送按钮重新计算能否点击。

### 5.5 RelayCommand

例如：

```csharp
[RelayCommand(CanExecute = nameof(CanSendMessage))]
private async Task SendMessageAsync()
```

会生成：

```text
SendMessageCommand
```

Avalonia 的按钮绑定这个命令：

```xml
<Button Command="{Binding SendMessageCommand}" />
```

`CanExecute` 的重点：

- `CanSendMessage()` 返回 false 时按钮不可用。
- 依赖属性变化时要调用 `NotifyCanExecuteChanged()`。
- 本项目在 `OnMessageChanged`、`OnIsConnectedChanged`、`OnSelectedChannelChanged` 里刷新命令状态。

### 5.6 ObservableCollection

本项目中：

- `Channels`
- `OnlineUsers`
- `AvailableRooms`
- `Notifications`
- `ChatChannelViewModel.Messages`

这些都是 UI 绑定集合。

为什么不用 `List<T>`：

- `List<T>` 增删元素时不会通知 UI。
- `ObservableCollection<T>` 实现了集合变化通知，UI 能自动刷新。

重要约束：

- 已绑定到 UI 的集合不要在后台线程随意修改。
- SignalR 回调进入 ViewModel 后，用 `Dispatcher.UIThread.Post` 回到 UI 线程。

### 5.7 UI 线程

文件：`SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`

关注：

```csharp
Avalonia.Threading.Dispatcher.UIThread.Post(() =>
{
    Notifications.Insert(0, ...);
});
```

原因：

- Avalonia 和 WinForms/WPF 一样，是单 UI 线程模型。
- 控件创建、布局、渲染、输入都在 UI 线程。
- 后台线程直接访问 UI 会抛异常或造成难诊断的丢更新。
- 修改绑定集合也要谨慎，因为 UI 正在枚举或监听集合变化。

## 6. Shared 项目：契约比你想象中更重要

文件：

- `SignalRDemo.Shared/Hubs/IChatHub.cs`
- `SignalRDemo.Shared/DTOs/ChatMessage.cs`
- `SignalRDemo.Shared/DTOs/RoomInfo.cs`
- `SignalRDemo.Shared/HubConstants.cs`

### 6.1 IChatHub

`IChatHub` 描述客户端可以调用服务端哪些方法：

- `SendMessage`
- `SendMessageToUser`
- `GetRooms`
- `JoinRoom`
- `StreamMonitoringData`
- `UploadFileChunk`

### 6.2 IChatHubClient

`IChatHubClient` 描述服务端可以推送给客户端哪些方法：

- `ReceiveMessage`
- `ReceiveNotification`
- `UserJoined`
- `UserLeft`
- `ReceivePrivateMessage`
- `UserTyping`

### 6.3 DTO 演进

`ChatMessage` 当前只有：

```csharp
public record ChatMessage(
    string User,
    string Message,
    DateTime Timestamp,
    string? Scope = null
);
```

这个设计对全局消息和房间消息勉强够用，但对私聊不够清晰。私聊至少需要知道：

- 谁发的：`FromUser`
- 发给谁：`ToUser`
- 属于哪个会话：`ConversationId`
- 消息展示在哪个频道：`ChannelId` 或可推导的频道 ID
- 消息类型：Global / Room / Private

这是本仓库最值得做的练习，后面有完整任务拆解。

## 7. 第二遍：按功能读代码

### 7.1 连接

路径：

```text
ConnectCommand
-> MainWindowViewModel.ConnectAsync
-> SignalRService.ConnectAsync
-> HubConnectionBuilder.WithUrl(...)
-> _connection.StartAsync(...)
-> ChatHub.OnConnectedAsync
-> Clients.Caller.ReceiveNotification(...)
-> Clients.Others.UserJoined(...)
```

检查点：

- 用户名现在从 query string 进入服务端。
- 连接成功后客户端主动刷新在线用户和房间列表。
- 自动重连只处理连接建立后的断线，不自动重试首次连接失败。

### 7.2 全局消息

路径：

```text
SendMessageCommand
-> SendMessageAsync
-> SignalRService.SendMessageAsync
-> ChatHub.SendMessage
-> Clients.All.ReceiveMessage
-> SignalRService RegisterEventHandlers
-> MainWindowViewModel.OnMessageReceived
-> Global channel Messages.Add(...)
```

检查点：

- `Clients.All` 包括发送者。
- 全局消息的 `Scope` 是 null。

### 7.3 房间消息

路径：

```text
JoinSelectedRoomCommand
-> JoinRoomAsync
-> SignalRService.JoinRoomAsync
-> ChatHub.JoinRoom
-> Groups.AddToGroupAsync
-> RoomManager.AddUserToRoom

SendMessageCommand
-> SelectedChannel.Type == Room
-> SignalRService.SendMessageToRoomAsync
-> ChatHub.SendMessageToRoom
-> Clients.Group(roomName).ReceiveMessage
```

检查点：

- SignalR group 是推送分组，不是数据库房间。
- `RoomManager` 是本项目自己维护的房间列表和人数。
- 断线后 `OnDisconnectedAsync` 清理 `RoomManager`，SignalR group 自身的连接移除由框架处理。

### 7.4 私聊

当前路径：

```text
SelectUserCommand
-> OpenPrivateChat(targetUser)
-> SendMessageCommand
-> SignalRService.SendMessageToUserAsync
-> ChatHub.SendMessageToUser
-> _onlineUsers.FirstOrDefault(x => x.Value == targetUser)
-> Clients.Client(targetConnection.Key).ReceivePrivateMessage(...)
-> Clients.Caller.ReceivePrivateMessage(...)
```

当前问题：

- 目标用户按用户名查找，用户名可重复。
- 用户名来自客户端，不能防伪造。
- `ChatMessage.Scope = "Private:{senderName}"` 对接收者有用，对发送者不够表达目标是谁。
- `ReceivePrivateMessage(string fromUser, ChatMessage message)` 这个参数名对发送者回显时语义不一致，因为服务端给 caller 传的是 `targetUser`。
- ViewModel 里已经出现大量注释说明这个设计混乱。

结论：

私聊 DTO 是当前最应该重构的地方。

### 7.5 监控流

路径：

```text
StartMonitoringCommand
-> MainWindowViewModel.StartMonitoringAsync
-> SignalRService.StreamMonitoringDataAsync
-> HubConnection.StreamAsync<MonitoringDataPoint>
-> ChatHub.StreamMonitoringData
-> yield return MonitoringDataPoint
-> await foreach 更新 CpuUsage / MemoryUsage / NetworkIn / NetworkOut
```

检查点：

- 服务端用 `IAsyncEnumerable<MonitoringDataPoint>`。
- 客户端用 `await foreach` 消费。
- 停止时通过 `CancellationTokenSource.CancelAsync()` 取消。

### 7.6 文件上传

路径：

```text
SignalRService.UploadFileAsync
-> 读取本地文件并按 FileChunkSize 切块
-> InvokeAsync("UploadFileChunk", ...)
-> ChatHub.UploadFileChunk
-> _fileChunks[fileKey][chunkIndex] = chunk
-> Clients.Caller.FileUploadProgressUpdated(...)
```

当前问题：

- 桌面 UI 里还没有完整文件选择入口。
- 文件块存在服务端内存里，大文件会增加内存压力。
- 没有文件大小、扩展名、MIME、病毒扫描、权限控制。
- 没有真正保存文件，只是模拟上传完成。

## 8. 14 天可执行学习计划

每天都要产出一个小结果，不要只看。

### Day 1：跑起来并画图

任务：

- 跑 Server、Avalonia Client、Web Client。
- 用两个客户端互发消息。
- 手画一张消息链路图。

产出：

- 一张图：UI -> ViewModel -> Service -> Hub -> Client callback -> UI。
- 一段话解释 REST 和 SignalR 在本项目里的边界。

### Day 2：读 Program.cs

任务：

- 给 `Program.cs` 每一段写一句中文注释，不提交也可以。
- 解释 `AddSignalR` 和 `MapHub` 的区别。
- 解释 CORS 为什么现在危险。

产出：

- 能口述服务端启动和请求管线。

### Day 3：读 ChatHub 生命周期

任务：

- 在 `OnConnectedAsync`、`OnDisconnectedAsync` 打断点或加日志。
- 连接两个客户端，断开一个，观察 `_onlineUsers` 和通知。

产出：

- 能解释 `Context.ConnectionId`、`Clients.Caller`、`Clients.Others`。

### Day 4：读全局消息和房间消息

任务：

- 跟踪全局消息。
- 跟踪房间消息。
- 改一个小功能：房间消息日志里加消息长度。

产出：

- 能解释 `Clients.All` 和 `Clients.Group`。

### Day 5：读 Avalonia 启动和 Binding

任务：

- 从 `Program.Main` 追到 `MainWindowViewModel` 构造函数。
- 找出 `MainWindow.axaml` 中 10 个 Binding，写出它们绑定到哪个属性。

产出：

- 能解释 `DataContext`。
- 能解释 `x:DataType` 和编译绑定。

### Day 6：读 CommunityToolkit.Mvvm

任务：

- 找出所有 `[ObservableProperty]`。
- 找出所有 `[RelayCommand]`。
- 解释 `ConnectCommand` 和 `SendMessageCommand` 是哪里来的。

产出：

- 能解释源生成器生成了什么。

### Day 7：UI 线程实验

任务：

- 找出所有 `Dispatcher.UIThread.Post`。
- 故意在一个 SignalR 回调里直接改集合，观察风险或异常；实验后恢复。

产出：

- 能解释为什么绑定集合要在 UI 线程修改。

### Day 8：RoomManager 并发分析

任务：

- 读 `RoomManager` 的三个字典。
- 写出加入房间、离开房间、断线清理的状态变化。
- 标出当前可能的并发风险。

产出：

- 能解释 `ConcurrentDictionary` 保护什么、不保护什么。

### Day 9：监控流

任务：

- 跟踪 `StartMonitoringCommand` 到 `StreamMonitoringData`。
- 解释 `IAsyncEnumerable`、`yield return`、`CancellationToken`。

产出：

- 能解释 SignalR streaming 和普通消息推送的区别。

### Day 10：私聊问题复盘

任务：

- 用两个用户互发私聊。
- 读 `SendMessageToUser` 和 `OnMessageReceived` 中私聊分支。
- 写出当前 DTO 为什么无法清楚表达发送者和接收者。

产出：

- 一份私聊 DTO 改造草案。

### Day 11-12：实现私聊 DTO 重构

任务：

- 修改 Shared DTO。
- 修改 Hub 私聊发送。
- 修改 SignalRService 接收逻辑。
- 修改 ViewModel 私聊路由。

产出：

- 私聊双方消息都进入正确频道。
- 代码中不再需要大量“猜测 Scope”的注释。

### Day 13：生产化方案

任务：

- 写一页生产化改造方案。
- 包含认证、数据库、Redis、限流、CORS、日志、部署。

产出：

- 能回答“这个 Demo 离商业应用差什么”。

### Day 14：模拟面试

任务：

- 不看代码回答本文第 12 节的 9 个问题。
- 回答不顺的地方，回代码重新追踪。

产出：

- 能用自己的话解释这个项目。

## 9. 最值得的练习：重构私聊 DTO

这是本仓库最高价值练习，因为它会逼你同时理解 Shared、Hub、SignalRService、ViewModel 路由和 UI Binding。

### 9.1 当前设计的问题

当前 `ChatMessage`：

```csharp
public record ChatMessage(
    string User,
    string Message,
    DateTime Timestamp,
    string? Scope = null
);
```

问题：

- `User` 是发送者，但名字太泛。
- `Message` 是内容，但属性名和类型名重复，阅读上不清晰。
- `Scope` 同时表达 Global、Room、Private，语义过载。
- 私聊缺少 `ToUser`。
- 私聊缺少稳定 `ConversationId`。
- 私聊路由依赖字符串 `"Private:{name}"`，容易写错且不可扩展。
- 用户名可重复，不能作为生产级用户标识。

### 9.2 建议目标模型

可以先做学习版，不必一步到位上数据库。

```csharp
public enum ChatMessageKind
{
    Global,
    Room,
    Private
}

public record ChatMessage(
    string MessageId,
    ChatMessageKind Kind,
    string FromUser,
    string? ToUser,
    string? RoomName,
    string ConversationId,
    string Content,
    DateTime Timestamp
);
```

字段解释：

- `MessageId`：消息唯一 ID，便于将来做去重、编辑、撤回。
- `Kind`：明确区分全局、房间、私聊。
- `FromUser`：发送者。
- `ToUser`：私聊接收者，全局和房间可为 null。
- `RoomName`：房间消息所属房间。
- `ConversationId`：UI 路由使用。全局可以是 `"global"`，房间可以是 `"room:{roomName}"`，私聊可以是 `"private:{minUser}:{maxUser}"`。
- `Content`：消息内容。
- `Timestamp`：服务端生成时间。

### 9.3 变更步骤

步骤 1：修改 Shared DTO。

文件：

- `SignalRDemo.Shared/DTOs/ChatMessage.cs`

步骤 2：修改服务端全局消息。

文件：

- `SignalRDemo.Server/Hubs/ChatHub.cs`

原则：

- 不再信任客户端传入的 `user`。
- 先用 `_onlineUsers[Context.ConnectionId]` 获取发送者。
- 生成 `MessageId` 和 `Timestamp`。

步骤 3：修改房间消息。

原则：

- `Kind = Room`
- `RoomName = roomName`
- `ConversationId = $"room:{roomName}"`

步骤 4：修改私聊消息。

原则：

- `Kind = Private`
- `FromUser = senderName`
- `ToUser = targetUser`
- `ConversationId = CreatePrivateConversationId(senderName, targetUser)`
- 给接收者和发送者推送同一个语义完整的 DTO。

步骤 5：简化客户端 SignalRService。

当前：

```csharp
_connection.On<string, ChatMessage>("ReceivePrivateMessage", (from, message) => ...)
```

可以改成：

```csharp
_connection.On<ChatMessage>("ReceivePrivateMessage", message =>
    MessageReceived?.Invoke(message));
```

也可以直接统一用 `ReceiveMessage(ChatMessage message)`，让 `Kind` 决定 UI 路由。

步骤 6：简化 ViewModel 路由。

目标逻辑：

```text
if Kind == Global:
    channel = Global

if Kind == Room:
    channel = room channel by RoomName

if Kind == Private:
    remoteUser = FromUser == UserName ? ToUser : FromUser
    channel = private channel by remoteUser
```

不再解析 `"Private:{name}"`。

步骤 7：补两个测试或手动验证。

验证场景：

- A 给 B 发私聊，A 的消息出现在 B 频道，B 的消息出现在 A 频道。
- A 给 B 发私聊时，A 本地频道显示远端用户 B，而不是自己。
- B 收到私聊时自动打开或创建 A 的私聊频道。
- 全局和房间消息不受影响。

### 9.4 面试表达

> 原实现把消息作用域压成一个字符串 Scope，对全局和房间还可以，但私聊需要同时知道发送者、接收者和会话标识。发送者回显时，如果 DTO 只带 `Private:{sender}`，客户端无法可靠知道这条消息应该进入哪个私聊频道。因此我会把 DTO 改成显式字段：`Kind`、`FromUser`、`ToUser`、`RoomName`、`ConversationId`。这样客户端路由不靠字符串猜测，也为消息历史、未读、撤回、搜索打基础。

## 10. 生产化改造路线

当前项目适合学习和 Demo，不适合直接商业化。

### 10.1 认证和授权

当前问题：

- 用户名来自 query string。
- 客户端能伪造任意用户名。
- Hub 默认没有 `[Authorize]`。

改造方向：

- 接入 OIDC/OAuth2/JWT。
- 桌面客户端登录后保存 access token 和 refresh token。
- SignalR `.WithUrl(..., options => options.AccessTokenProvider = ...)` 携带 token。
- 服务端 `AddAuthentication().AddJwtBearer(...)`。
- Hub 加 `[Authorize]`。
- 从 `Context.UserIdentifier` 或 `Context.User` 获取用户 ID，不信任客户端传入用户名。
- 房间加入、发言、私聊、管理动作走策略授权。

### 10.2 持久化

当前问题：

- 在线用户在内存。
- 房间在内存。
- 文件块在内存。
- 消息无历史。

改造方向：

- PostgreSQL 或 SQL Server 存用户、房间、成员、消息、附件、审计日志。
- EF Core migration 管理 schema。
- Redis 存在线状态、连接映射、短期缓存。
- 对象存储存附件，例如 S3、MinIO、Azure Blob。
- Hub 只负责实时通道，业务规则放应用服务。

### 10.3 限流和滥用防护

当前问题：

- 消息发送没有频率限制。
- 房间名、消息长度、文件大小校验不足。
- `MaximumReceiveMessageSize` 只能限制单条 SignalR 消息大小，不等于业务安全。

改造方向：

- ASP.NET Core Rate Limiting 中间件保护 HTTP endpoint。
- Hub 方法内部按用户 ID 做消息频率限制。
- 校验消息长度、房间名格式、文件大小、扩展名、MIME。
- 对异常行为记录安全日志。

### 10.4 横向扩容

当前问题：

- 单机内存状态无法多实例共享。
- SignalR 默认只知道本进程连接。

改造方向：

- Azure 上优先考虑 Azure SignalR Service。
- 自托管可以考虑 Redis backplane。
- 使用多实例时通常还要 sticky sessions，除非架构满足不需要的条件。
- 在线状态迁移到 Redis，并设置 TTL 和心跳。
- 房间成员事实存数据库，Redis 缓存在线连接。

### 10.5 CORS 和 HTTPS

当前问题：

```csharp
.SetIsOriginAllowed(_ => true)
.AllowCredentials()
```

这是 Demo 级配置。允许任意来源同时允许凭据有安全风险。

改造方向：

- 配置化 origin 白名单。
- 生产强制 HTTPS/WSS。
- 日志避免记录 token、私聊正文和敏感 query string。

### 10.6 可观测性

改造方向：

- 结构化日志：连接、断开、Hub 方法、失败原因。
- 指标：连接数、消息吞吐、失败率、重连次数、P95/P99 Hub 方法耗时。
- OpenTelemetry traces/metrics/logs。
- Prometheus/Grafana 或云监控。
- 健康检查拆分 `/health/live` 和 `/health/ready`。

## 11. 代码知识图谱

### 11.1 Server

- `SignalRDemo.Server/Program.cs`
  - DI
  - SignalR 注册
  - CORS
  - 静态文件
  - Minimal API
  - Hub 映射

- `SignalRDemo.Server/Hubs/ChatHub.cs`
  - 连接生命周期
  - 全局消息
  - 私聊
  - 房间
  - 在线用户
  - 监控流
  - 文件上传

- `SignalRDemo.Server/Services/RoomManager.cs`
  - 房间状态
  - 连接和房间双向索引
  - 并发集合和锁

- `SignalRDemo.Server/Services/SystemMonitorService.cs`
  - CPU 采样
  - 内存采样
  - 网络采样
  - OS 差异

- `SignalRDemo.Server/wwwroot/index.html`
  - Web UI
  - SignalR JS Client
  - DOM 状态更新

### 11.2 Shared

- `SignalRDemo.Shared/Hubs/IChatHub.cs`
  - 客户端调用服务端的契约
  - 服务端调用客户端的契约

- `SignalRDemo.Shared/DTOs/ChatMessage.cs`
  - 聊天消息
  - 系统通知
  - 连接状态
  - 文件上传进度
  - 监控数据点

- `SignalRDemo.Shared/DTOs/RoomInfo.cs`
  - 房间名
  - 在线人数

- `SignalRDemo.Shared/HubConstants.cs`
  - Hub 路径
  - 默认服务端地址
  - 文件块大小
  - 监控间隔
  - 重连延迟

### 11.3 Avalonia Client

- `SignalRDemo.Client/Program.cs`
  - Avalonia AppBuilder
  - 桌面生命周期

- `SignalRDemo.Client/App.axaml`
  - 全局样式
  - ViewLocator

- `SignalRDemo.Client/App.axaml.cs`
  - 主题初始化
  - MainWindow 创建
  - DataContext 设置

- `SignalRDemo.Client/Views/MainWindow.axaml`
  - 布局
  - Binding
  - Command
  - DataTemplate
  - ItemsControl

- `SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`
  - 状态
  - 命令
  - SignalR 事件处理
  - UI 线程调度
  - 频道路由

- `SignalRDemo.Client/ViewModels/ChatChannelViewModel.cs`
  - 频道 ID
  - 显示名
  - 类型
  - 未读状态
  - 消息集合

- `SignalRDemo.Client/Services/SignalRService.cs`
  - HubConnection
  - 连接和断开
  - 自动重连
  - InvokeAsync 调用 Hub
  - On 注册服务端推送
  - StreamAsync 消费监控流
  - 文件分片上传

- `SignalRDemo.Client/Services/ThemeService.cs`
  - 深浅主题切换
  - 动态 StyleInclude

- `SignalRDemo.Client/Converters/BoolToColorConverter.cs`
  - bool 到颜色
  - 用户名到首字母
  - 频道类型到可见性
  - 选中频道背景

- `SignalRDemo.Client/Reactive/*.cs`
  - Rx 学习示例
  - Subject
  - Buffer
  - Throttle
  - Merge
  - ReplaySubject

## 12. 面试问题和高质量回答

### 12.1 SignalR 和 REST 的区别是什么？这个项目为什么用 SignalR？

REST 是请求/响应模型。客户端发起 HTTP 请求，服务端返回结果，适合资源查询、保存、分页、配置、健康检查等。

SignalR 是实时双向通信抽象。客户端能调用服务端 Hub 方法，服务端也能主动推送到客户端，底层可用 WebSockets 等传输方式，适合聊天、通知、在线状态、实时监控。

本项目中：

- REST：`/health`、`/api/info`
- SignalR：消息、房间、私聊、在线用户、监控流、文件上传进度

回答重点：

> 聊天和监控都需要服务端主动推送，REST 需要轮询，延迟和资源浪费更大。SignalR 提供了连接管理、广播、分组、重连和流式数据，所以更适合这个项目。

### 12.2 Hub 是怎么被注册和映射的？

注册：

```csharp
builder.Services.AddSignalR(...)
```

映射：

```csharp
app.MapHub<ChatHub>(HubConstants.ChatHubPath);
```

本项目路径：

```csharp
public const string ChatHubPath = "/hubs/chat";
```

回答重点：

> `AddSignalR` 把 Hub 所需服务加入 DI，`MapHub` 把某个 Hub 类型映射到 HTTP 路由。客户端连接的 URL 必须和这个路由一致。

### 12.3 Clients.All、Clients.Others、Clients.Caller、Clients.Group 区别是什么？

- `All`：所有连接，包括调用者。
- `Others`：除调用者之外的所有连接。
- `Caller`：当前调用 Hub 方法的连接。
- `Group(name)`：指定组内所有连接。
- `Client(connectionId)`：指定连接。
- `User(userId)`：指定用户的所有连接，需要认证和稳定用户 ID。

本项目使用：

- 全局消息：`Clients.All.ReceiveMessage`
- 新用户加入：`Clients.Others.UserJoined`
- 连接成功、上传进度：`Clients.Caller`
- 房间消息：`Clients.Group(roomName)`
- 当前私聊：`Clients.Client(connectionId)`

### 12.4 客户端如何监听服务端推送？

文件：`SignalRDemo.Client/Services/SignalRService.cs`

核心是：

```csharp
_connection.On<ChatMessage>("ReceiveMessage", message =>
    MessageReceived?.Invoke(message));
```

服务端调用客户端方法名，客户端用相同名称注册回调。回调再通过 C# event 抛给 ViewModel，ViewModel 决定怎么更新 UI 状态。

回答重点：

> 我把 SignalR 回调封装在 `SignalRService` 中，不让 ViewModel 直接依赖 `HubConnection`。这样 ViewModel 只处理业务状态，网络细节集中在 Service。

### 12.5 Avalonia 的 MVVM 数据绑定和 WinForms 事件模型有什么不同？

WinForms：

- 控件事件驱动。
- code-behind 里经常直接读写控件。
- UI 控件容易变成状态源。

Avalonia MVVM：

- View 通过 Binding 连接 ViewModel。
- ViewModel 暴露属性、集合、命令。
- View 不负责业务逻辑。
- 状态变化通过 `INotifyPropertyChanged` 和 `INotifyCollectionChanged` 通知 UI。

回答重点：

> WinForms 更像“控件发生事件，我去改控件”；MVVM 更像“用户操作改变 ViewModel 状态，Binding 自动反映到控件”。这让业务逻辑更容易测试，也更适合复杂界面状态。

### 12.6 为什么 UI 集合更新要考虑 UI 线程？

Avalonia 是单 UI 线程模型。控件创建、布局、渲染、输入都在 UI 线程。SignalR 回调可能来自非 UI 上下文，如果直接修改绑定集合，可能抛 `InvalidOperationException`，也可能出现难诊断的丢更新。

本项目做法：

```csharp
Avalonia.Threading.Dispatcher.UIThread.Post(() =>
{
    Notifications.Insert(0, ...);
});
```

回答重点：

> 不只是控件属性需要 UI 线程，绑定集合也要谨慎。因为 UI 正在监听集合变化，后台线程修改会破坏 UI 框架的线程假设。

### 12.7 当前在线用户为什么用 ConcurrentDictionary？它还有什么并发风险？

使用原因：

- 多个连接可能同时上线、离线、发消息。
- 字典增删查需要线程安全。

风险：

- `ConcurrentDictionary` 不保护 value 对象内部状态。
- `HashSet` 仍需额外锁。
- `RoomInfo.UserCount` 可变，没有完整同步。
- 静态内存状态不能跨进程。
- 用户名可能重复。

回答重点：

> `ConcurrentDictionary` 只能保证字典操作线程安全，不代表整个房间状态一致性就安全。生产环境我会把用户身份、房间成员和消息持久化，把在线连接映射放 Redis 或专门的 presence 服务。

### 12.8 如果要商业化，你会怎么加认证、持久化、限流、Redis backplane？

认证：

- OIDC/OAuth2/JWT。
- Hub 加 `[Authorize]`。
- 客户端用 `AccessTokenProvider`。
- 服务端从 `Context.UserIdentifier` 获取用户 ID。

持久化：

- 数据库存用户、房间、成员、消息、附件元数据。
- 对象存储保存附件。
- 消息发送先执行业务校验，再写库，再推送。

限流：

- HTTP endpoint 用 ASP.NET Core Rate Limiting。
- Hub 方法按用户 ID 做频率控制。
- 限制消息长度、房间名、文件大小、上传类型。

横向扩容：

- Azure 上用 Azure SignalR Service。
- 自托管用 Redis backplane。
- sticky sessions 视部署方式配置。
- 在线状态用 Redis TTL，业务事实进数据库。

### 12.9 为什么私聊现在的 DTO 设计不够好？应该怎么改？

当前问题：

- `Scope` 是字符串，语义过载。
- 只有 `User`，没有明确 `FromUser` / `ToUser`。
- 发送者回显时无法可靠推断目标频道。
- 用户名可伪造且可重复。

改法：

- `ChatMessageKind Kind`
- `FromUser`
- `ToUser`
- `RoomName`
- `ConversationId`
- `Content`
- `MessageId`

回答重点：

> DTO 是跨端契约，不应该让客户端靠字符串猜测业务语义。私聊应显式表达发送者、接收者和会话 ID，这样 UI 路由、未读计数、历史消息、撤回编辑都能建立在稳定模型上。

## 13. 自测清单

不看代码回答：

- [ ] `Program.cs` 中 `AddSignalR` 和 `MapHub` 分别做什么？
- [ ] `Hub<T>` 的 T 是什么？本项目为什么用 `IChatHubClient`？
- [ ] `Context.ConnectionId` 和用户 ID 有什么区别？
- [ ] `Clients.All` 是否包括调用者？
- [ ] SignalR group 是否会在服务端重启后保留？
- [ ] Avalonia 的 `DataContext` 在哪里设置？
- [ ] `[ObservableProperty]` 生成了什么？
- [ ] `[RelayCommand]` 生成的命令叫什么？
- [ ] `ObservableCollection` 和 `List` 对 UI 有什么区别？
- [ ] 为什么 SignalR 回调里要调度到 UI 线程？
- [ ] 当前私聊 DTO 有什么缺陷？
- [ ] 当前项目商业化前最先要补哪三件事？

## 14. 延伸练习

### 14.1 小练习：消息长度校验

目标：

- 服务端拒绝空消息和超过 500 字符的消息。
- 返回 `ReceiveNotification` 给调用者。

涉及文件：

- `SignalRDemo.Server/Hubs/ChatHub.cs`
- `SignalRDemo.Shared/DTOs/ChatMessage.cs` 可选

完成标准：

- 客户端不能发送空白消息。
- 服务端也能防住绕过 UI 的非法调用。

### 14.2 小练习：房间名校验

目标：

- 房间名只能包含字母、数字、`-`、`_`。
- 长度 2 到 32。

涉及文件：

- `RoomManager`
- `ChatHub.JoinRoom`
- `ChatHub.CreateRoom`

完成标准：

- UI 提示创建失败。
- 服务端日志记录非法房间名。

### 14.3 中练习：私聊 DTO 重构

目标：

- 用显式 `FromUser`、`ToUser`、`ConversationId` 替代 `Scope` 猜测。

涉及文件：

- `SignalRDemo.Shared/DTOs/ChatMessage.cs`
- `SignalRDemo.Shared/Hubs/IChatHub.cs`
- `SignalRDemo.Server/Hubs/ChatHub.cs`
- `SignalRDemo.Client/Services/SignalRService.cs`
- `SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`
- `SignalRDemo.Client/ViewModels/ChatChannelViewModel.cs`

完成标准：

- A 给 B 发私聊，双方都显示在与对方的私聊频道里。
- 删除 `OnMessageReceived` 中关于私聊路由混乱的大段注释。
- 全局和房间消息仍正常。

### 14.4 中练习：重连后恢复房间

目标：

- 客户端记录已加入房间。
- `Reconnected` 后自动重新 `JoinRoom`。
- 重新刷新在线用户和房间列表。

涉及文件：

- `SignalRService.cs`
- `MainWindowViewModel.cs`

完成标准：

- 停掉服务端再启动，客户端重连后房间状态恢复。

### 14.5 大练习：消息历史

目标：

- 引入数据库保存消息。
- 加 REST API 分页查询历史。
- 连接后加载最近 50 条。
- SignalR 只负责实时增量。

涉及方向：

- EF Core
- Message entity
- Repository/Application service
- REST endpoint
- ViewModel 分页状态

完成标准：

- 服务端重启后消息仍在。
- 新客户端连接后能看到历史。

## 15. 参考资料

官方资料：

- ASP.NET Core SignalR Hubs: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-10.0
- ASP.NET Core SignalR users and groups: https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0
- ASP.NET Core SignalR .NET client: https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-10.0
- ASP.NET Core SignalR streaming: https://learn.microsoft.com/en-us/aspnet/core/signalr/streaming?view=aspnetcore-10.0
- ASP.NET Core SignalR authentication and authorization: https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0
- ASP.NET Core SignalR hosting and scaling: https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0
- ASP.NET Core Rate Limiting: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0
- ASP.NET Core CORS: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0
- Avalonia MVVM pattern: https://docs.avaloniaui.net/docs/fundamentals/the-mvvm-pattern
- Avalonia data binding: https://docs.avaloniaui.net/docs/data-binding/introduction-to-data-binding
- Avalonia compiled bindings: https://docs.avaloniaui.net/docs/data-binding/compiled-bindings
- Avalonia collection binding: https://docs.avaloniaui.net/docs/data-binding/how-to-bind-to-a-collection
- Avalonia threading model: https://docs.avaloniaui.net/docs/app-development/threading
- CommunityToolkit.Mvvm ObservableProperty: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
- CommunityToolkit.Mvvm RelayCommand: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand

仓库内资料：

- `README.md`
- `docs/CommercializationTasks.md`
- `docs/RxCheatSheet.md`
