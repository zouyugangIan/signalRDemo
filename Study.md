# Study Guide for SignalRDemo
SignalRDemo 学习指南

This document lists the key knowledge areas behind the demo and a phased learning roadmap
that covers backend, frontend (Avalonia + web), SignalR, Rx, and basic REST.
本文档列出该 demo 背后的关键知识点，并提供分阶段学习路线，覆盖后端、前端（Avalonia + Web）、SignalR、Rx 和基础 REST。

## 1) Knowledge Map (by topic)
知识图谱（按主题）

### Backend Basics (ASP.NET Core)
后端基础（ASP.NET Core）
- Minimal hosting model, middleware pipeline order, and endpoint mapping.
  最小化托管模型、中间件管线顺序与端点映射。
- Dependency injection lifetimes (Singleton/Scoped/Transient) and constructor injection.
  依赖注入生命周期（Singleton/Scoped/Transient）与构造函数注入。
- Environment-based configuration and appsettings conventions.
  基于环境的配置与 appsettings 约定。
- Logging basics with structured logs and log levels.
  结构化日志与日志级别的基础。
- Static file hosting and default document behavior.
  静态文件托管与默认文档行为。
- OpenAPI basics for minimal APIs.
  Minimal API 的 OpenAPI 基础。

### REST Basics
REST 基础
- Resource modeling and URL design.
  资源建模与 URL 设计。
- HTTP verbs and idempotency.
  HTTP 动词与幂等性。
- Status codes and error responses (including ProblemDetails).
  状态码与错误响应（含 ProblemDetails）。
- Model binding and validation.
  模型绑定与校验。
- REST vs SignalR use cases and trade-offs.
  REST 与 SignalR 的使用场景与权衡。
- API testing tooling (.http, curl, Postman).
  API 测试工具（.http、curl、Postman）。

### SignalR Core
SignalR 核心
- Hub concept and client/server method contracts.
  Hub 概念与客户端/服务端方法契约。
- Connection lifecycle events and state tracking.
  连接生命周期事件与状态跟踪。
- Client connection setup, automatic reconnect, and event callbacks.
  客户端连接配置、自动重连与事件回调。
- Broadcast vs groups vs caller/others.
  广播 vs 分组 vs 调用者/其他人。
- Streaming from server to client.
  服务端到客户端的流式推送。
- DTO evolution and compatibility.
  DTO 演进与兼容性。

### Real-time Features
实时功能
- Chat message scope design (global/room/private).
  聊天消息作用域设计（全局/房间/私聊）。
- Private messaging routing and metadata design.
  私聊消息路由与元数据设计。
- Room lifecycle and membership tracking.
  房间生命周期与成员跟踪。
- Typing indicators.
  正在输入指示。
- Chunked file upload and progress reporting.
  分片文件上传与进度上报。
- Monitoring data streaming and UI update logic.
  监控数据流与 UI 更新逻辑。

### Concurrency and Async
并发与异步
- async/await best practices.
  async/await 最佳实践。
- CancellationToken usage in long-running operations and streams.
  CancellationToken 在长任务与流中的使用。
- Thread-safe collections and locking.
  线程安全集合与锁。
- UI thread dispatch for safe UI updates.
  UI 线程调度以保证安全更新。
- Proper disposal of connections and subscriptions.
  连接与订阅的正确释放。

### Avalonia UI (Desktop)
Avalonia UI（桌面）
- XAML layout system (Grid, StackPanel, Border).
  XAML 布局系统（Grid、StackPanel、Border）。
- DataTemplate and ItemsControl usage patterns.
  DataTemplate 与 ItemsControl 的使用模式。
- Styles, theme resources, and dynamic theme switching.
  样式、主题资源与动态主题切换。
- Value converters and binding strategies.
  值转换器与绑定策略。
- App lifecycle and view location.
  应用生命周期与视图定位。

### MVVM and State
MVVM 与状态
- MVVM layering and responsibilities.
  MVVM 分层与职责。
- CommunityToolkit.Mvvm (ObservableProperty, RelayCommand, partial hooks).
  CommunityToolkit.Mvvm（ObservableProperty、RelayCommand、partial hooks）。
- ObservableCollection updates and UI binding.
  ObservableCollection 更新与 UI 绑定。
- Command CanExecute refresh logic.
  Command 的 CanExecute 刷新逻辑。
- Channel state (unread, selection, routing).
  频道状态（未读、选中、路由）。

### Reactive Extensions (Rx)
Reactive Extensions（Rx）
- Observable/Observer model and Subjects.
  Observable/Observer 模型与 Subject。
- Core operators: Select, Where, Buffer, Merge, Throttle, DistinctUntilChanged.
  核心操作符：Select、Where、Buffer、Merge、Throttle、DistinctUntilChanged。
- Error handling and retry.
  错误处理与重试。
- Subscription lifetime management.
  订阅生命周期管理。
- Using Rx to model real-time UI flows.
  使用 Rx 建模实时 UI 流程。

### Web Frontend (Static HTML)
Web 前端（静态 HTML）
- HTML semantic structure and form controls.
  HTML 语义结构与表单控件。
- CSS variables, gradients, theming.
  CSS 变量、渐变与主题化。
- Layout (flex/grid) and responsive behavior.
  布局（flex/grid）与响应式行为。
- SignalR JS client usage and event handling.
  SignalR JS 客户端使用与事件处理。
- Basic DOM state update patterns.
  基本的 DOM 状态更新模式。

### System Monitoring
系统监控
- CPU usage calculation strategy.
  CPU 使用率计算策略。
- Memory usage sources and OS differences.
  内存使用来源与系统差异。
- Network throughput sampling and rate conversion.
  网络吞吐采样与速率换算。
- Accuracy vs overhead vs permissions trade-offs.
  精度、开销与权限的权衡。

### Security and Reliability
安全与可靠性
- CORS design and credentialed requests.
  CORS 设计与凭据请求。
- TLS basics and dev-time certificate validation risks.
  TLS 基础与开发期证书校验风险。
- Authentication/authorization for hubs and APIs.
  Hub 与 API 的认证/授权。
- Input validation and message size limits.
  输入校验与消息大小限制。
- Reconnect strategy and user experience.
  重连策略与用户体验。

### Project Architecture
项目架构
- Solution and project references.
  解决方案与项目引用关系。
- Shared contract and DTO alignment.
  共享契约与 DTO 对齐。
- Constants management and configuration.
  常量管理与配置。
- Separation of concerns and maintainability.
  关注点分离与可维护性。
- Contract versioning and backward compatibility.
  契约版本管理与向后兼容。

### Testing and Ops
测试与运维
- Unit tests for room logic and monitoring.
  房间逻辑与监控的单元测试。
- Integration tests for hub calls and reconnection.
  Hub 调用与重连的集成测试。
- Load testing for concurrency and fan-out.
  并发与广播扇出的负载测试。
- Observability: logs, metrics, alerts.
  可观测性：日志、指标、告警。
- Deployment basics: ports, HTTPS, environment config.
  部署基础：端口、HTTPS、环境配置。

## 2) Knowledge Map (by file)
知识图谱（按文件）

Use this map to connect concepts to code.
使用此映射将概念与代码对应起来。

### Server
服务端
- `SignalRDemo.Server/Program.cs`: middleware order, CORS, static files, minimal APIs, hub mapping.
  `SignalRDemo.Server/Program.cs`：中间件顺序、CORS、静态文件、Minimal API、Hub 映射。
- `SignalRDemo.Server/Hubs/ChatHub.cs`: hub lifecycle, messaging, rooms, streaming, file upload.
  `SignalRDemo.Server/Hubs/ChatHub.cs`：Hub 生命周期、消息、房间、流式、文件上传。
- `SignalRDemo.Server/Services/RoomManager.cs`: room state and concurrency patterns.
  `SignalRDemo.Server/Services/RoomManager.cs`：房间状态与并发模式。
- `SignalRDemo.Server/Services/SystemMonitorService.cs`: CPU/memory/network sampling logic.
  `SignalRDemo.Server/Services/SystemMonitorService.cs`：CPU/内存/网络采样逻辑。
- `SignalRDemo.Server/wwwroot/index.html`: frontend HTML/CSS/SignalR JS.
  `SignalRDemo.Server/wwwroot/index.html`：前端 HTML/CSS/SignalR JS。

### Shared
共享项目
- `SignalRDemo.Shared/Hubs/IChatHub.cs`: typed hub contract.
  `SignalRDemo.Shared/Hubs/IChatHub.cs`：强类型 Hub 契约。
- `SignalRDemo.Shared/DTOs/*.cs`: DTOs and basic modeling.
  `SignalRDemo.Shared/DTOs/*.cs`：DTO 与基础建模。
- `SignalRDemo.Shared/HubConstants.cs`: shared constants and configuration defaults.
  `SignalRDemo.Shared/HubConstants.cs`：共享常量与配置默认值。

### Client (Avalonia)
客户端（Avalonia）
- `SignalRDemo.Client/Views/MainWindow.axaml`: layout and bindings.
  `SignalRDemo.Client/Views/MainWindow.axaml`：布局与绑定。
- `SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`: MVVM, commands, state, UI thread dispatch.
  `SignalRDemo.Client/ViewModels/MainWindowViewModel.cs`：MVVM、命令、状态、UI 线程调度。
- `SignalRDemo.Client/Services/SignalRService.cs`: SignalR client, reconnect, and message flow.
  `SignalRDemo.Client/Services/SignalRService.cs`：SignalR 客户端、重连与消息流。
- `SignalRDemo.Client/Converters/BoolToColorConverter.cs`: converters.
  `SignalRDemo.Client/Converters/BoolToColorConverter.cs`：转换器。
- `SignalRDemo.Client/Styles/*.axaml`: theme resources and visual styles.
  `SignalRDemo.Client/Styles/*.axaml`：主题资源与视觉样式。
- `SignalRDemo.Client/Reactive/*.cs`: Rx examples and SignalR + Rx integration.
  `SignalRDemo.Client/Reactive/*.cs`：Rx 示例与 SignalR + Rx 集成。

## 3) Phased Learning Roadmap (comprehensive)
分阶段学习路线（全面版）

Each phase is designed to be practical. Complete the checkpoints before moving on.
每个阶段都偏重实践，完成检查点再进入下一阶段。

### Phase 0: Baseline Foundations (1-2 days)
阶段 0：基础打底（1-2 天）
Focus: make the demo runnable and understand the data flow.
重点：让 demo 能跑起来并理解数据流。
- Run the server and client; connect and send messages.
  运行服务端和客户端，连接并发送消息。
- Identify which features are REST vs SignalR.
  识别哪些功能属于 REST，哪些属于 SignalR。
- Draw a data-flow diagram (client input -> hub -> broadcast -> UI update).
  绘制数据流图（客户端输入 -> Hub -> 广播 -> UI 更新）。
- Learn the basics of async/await and CancellationToken.
  学习 async/await 和 CancellationToken 的基础。
- Learn the basic concept of MVVM.
  学习 MVVM 的基本概念。

Checklist:
检查清单：
- [ ] Can run server and client.
  可以运行服务端和客户端。
- [ ] Can explain the difference between REST and SignalR in this project.
  能解释本项目中 REST 与 SignalR 的区别。
- [ ] Can explain how a message gets from UI to other clients.
  能解释消息如何从 UI 到其他客户端。

### Phase 1: REST and Backend Fundamentals (3-5 days)
阶段 1：REST 与后端基础（3-5 天）
Focus: backend structure, API design, and middleware.
重点：后端结构、API 设计与中间件。
- Middleware order and why it matters.
  中间件顺序及其影响原因。
- CORS and how it affects SignalR.
  CORS 及其对 SignalR 的影响。
- Minimal APIs and routing design.
  Minimal API 与路由设计。
- HTTP verbs, status codes, and error models.
  HTTP 动词、状态码与错误模型。
- DTO validation patterns.
  DTO 校验模式。

Suggested exercises:
建议练习：
- Add a new minimal API endpoint with proper status codes and validation.
  新增一个带正确状态码与校验的 minimal API 端点。
- Add a server-side logging message for that endpoint.
  为该端点添加服务端日志。

Checklist:
检查清单：
- [ ] Can explain pipeline order and effects of each middleware.
  能解释管线顺序及各中间件的影响。
- [ ] Can define basic REST routes and status codes.
  能定义基础 REST 路由与状态码。
- [ ] Can add a new endpoint with validation.
  能新增一个带校验的端点。

### Phase 2: SignalR Core + Real-time Features (5-7 days)
阶段 2：SignalR 核心与实时功能（5-7 天）
Focus: hub design, broadcasting, and group logic.
重点：Hub 设计、广播与分组逻辑。
- Hub lifecycle (OnConnected/OnDisconnected).
  Hub 生命周期（OnConnected/OnDisconnected）。
- Client connection and reconnect behavior.
  客户端连接与重连行为。
- Groups and room membership.
  分组与房间成员关系。
- Private chat routing strategy.
  私聊路由策略。
- Streamed data and cancellation.
  流式数据与取消。
- File upload chunking.
  文件分片上传。

Suggested exercises:
建议练习：
- Add a "typing" event flow in UI.
  在 UI 中加入“正在输入”事件流。
- Add a server-side rate limit for message sending.
  为消息发送增加服务端限流。

Checklist:
检查清单：
- [ ] Can describe how groups work in SignalR.
  能描述 SignalR 中分组的工作方式。
- [ ] Can add a new hub method and call it from client.
  能新增一个 Hub 方法并在客户端调用。
- [ ] Can explain streaming and cancellation behavior.
  能解释流式与取消行为。

### Phase 3: Avalonia UI + MVVM (5-7 days)
阶段 3：Avalonia UI 与 MVVM（5-7 天）
Focus: desktop UI, binding, and state.
重点：桌面 UI、绑定与状态。
- Grid layout, ItemsControl, and DataTemplates.
  Grid 布局、ItemsControl 与 DataTemplate。
- Binding and value converters.
  绑定与值转换器。
- Themes and dynamic resources.
  主题与动态资源。
- Command enable/disable logic.
  命令启用/禁用逻辑。
- UI thread dispatch for events.
  事件的 UI 线程调度。

Suggested exercises:
建议练习：
- Add a new panel showing message counts.
  新增一个显示消息数量的面板。
- Add a theme toggle effect in UI state.
  在 UI 状态中加入主题切换效果。

Checklist:
检查清单：
- [ ] Can create a new view and bind to ViewModel.
  能创建新视图并绑定到 ViewModel。
- [ ] Can implement a converter for UI state.
  能为 UI 状态实现一个转换器。
- [ ] Can explain how CanExecute refresh works.
  能解释 CanExecute 刷新的机制。

### Phase 4: Rx and Real-time Streams (4-6 days)
阶段 4：Rx 与实时流（4-6 天）
Focus: modeling real-time events with Rx.
重点：用 Rx 建模实时事件。
- Observable and Subject types.
  Observable 与 Subject 类型。
- Key operators (Throttle, Buffer, Merge).
  关键操作符（Throttle、Buffer、Merge）。
- Error handling and retries.
  错误处理与重试。
- Subscription lifetime management.
  订阅生命周期管理。

Suggested exercises:
建议练习：
- Implement spam detection using Buffer in the UI.
  使用 Buffer 在 UI 中实现刷屏检测。
- Add a search box with Throttle + DistinctUntilChanged.
  添加带 Throttle + DistinctUntilChanged 的搜索框。

Checklist:
检查清单：
- [ ] Can explain how Throttle vs Buffer works.
  能解释 Throttle 与 Buffer 的差异。
- [ ] Can build a small Rx pipeline for a UI feature.
  能为某个 UI 功能构建一个小型 Rx 流水线。

### Phase 5: Reliability, Security, and Production Thinking (4-6 days)
阶段 5：可靠性、安全与生产化思维（4-6 天）
Focus: practical risks and system behavior under load.
重点：实际风险与压力下的系统行为。
- Authentication/authorization for hubs.
  Hub 的认证/授权。
- TLS and certificate validation.
  TLS 与证书校验。
- Message size limits and abuse prevention.
  消息大小限制与滥用防护。
- Reconnect policy and UX.
  重连策略与用户体验。
- Observability and logging.
  可观测性与日志。

Suggested exercises:
建议练习：
- Add a basic auth check on connect.
  在连接时增加基础鉴权检查。
- Add a simple server-side message length validation.
  添加服务端消息长度校验。

Checklist:
检查清单：
- [ ] Can list main security risks in this project.
  能列出本项目的主要安全风险。
- [ ] Can design a safe reconnect strategy.
  能设计安全的重连策略。

### Phase 6: Testing and Deployment (3-5 days)
阶段 6：测试与部署（3-5 天）
Focus: testability and real-world running.
重点：可测试性与真实环境运行。
- Unit testing RoomManager logic.
  RoomManager 逻辑的单元测试。
- Integration testing SignalR hub methods.
  SignalR Hub 方法的集成测试。
- Simple load tests for broadcast.
  广播的简单负载测试。
- Publish and run in a different environment.
  发布并在不同环境运行。

Suggested exercises:
建议练习：
- Write a test for room join/leave counts.
  编写房间加入/退出计数测试。
- Run a basic load script and observe logs.
  运行基础压测脚本并观察日志。

Checklist:
检查清单：
- [ ] Can write a unit test for room logic.
  能编写房间逻辑的单元测试。
- [ ] Can run the app with a non-dev config.
  能在非开发配置下运行应用。

## 4) Recommended Next Steps
下一步建议

Pick one:
选择一个：
1) I can draft a focused 2-week schedule with daily tasks based on your current skill level.
   我可以根据你的当前水平拟定一份专注的 2 周每日任务计划。
2) I can walk through one subsystem line-by-line (SignalR, Avalonia, Rx, or REST).
   我可以逐行讲解一个子系统（SignalR、Avalonia、Rx 或 REST）。
3) I can add small practice tasks directly in the repo (with code changes).
   我可以在仓库里直接添加小练习任务（包含代码改动）。
