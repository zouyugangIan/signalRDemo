using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using SignalRDemo.Shared.DTOs;

namespace SignalRDemo.Client.Reactive;

/// <summary>
/// SignalR + Rx 集成服务
/// 
/// 展示如何将 SignalR 事件转换为 Rx 可观察流，
/// 然后利用 Rx 操作符进行复杂的事件处理。
/// </summary>
public class ReactiveSignalRService : IDisposable
{
    // 将 SignalR 事件转换为 Rx Subject
    private readonly Subject<ChatMessage> _messageSubject = new();
    private readonly Subject<ConnectionStatus> _userJoinedSubject = new();
    private readonly Subject<ConnectionStatus> _userLeftSubject = new();
    private readonly Subject<(string User, bool IsTyping)> _typingSubject = new();
    private readonly Subject<MonitoringDataPoint> _monitoringSubject = new();
    
    private readonly CompositeDisposable _disposables = new();

    #region 公开的 Observable 流

    /// <summary>
    /// 消息流 - 所有收到的消息
    /// </summary>
    public IObservable<ChatMessage> Messages => _messageSubject.AsObservable();

    /// <summary>
    /// 用户上线流
    /// </summary>
    public IObservable<ConnectionStatus> UserJoined => _userJoinedSubject.AsObservable();

    /// <summary>
    /// 用户离线流
    /// </summary>
    public IObservable<ConnectionStatus> UserLeft => _userLeftSubject.AsObservable();

    /// <summary>
    /// 输入状态流
    /// </summary>
    public IObservable<(string User, bool IsTyping)> TypingStatus => _typingSubject.AsObservable();

    /// <summary>
    /// 监控数据流
    /// </summary>
    public IObservable<MonitoringDataPoint> MonitoringData => _monitoringSubject.AsObservable();

    #endregion

    #region 高级 Rx 处理流 (学习重点!)

    /// <summary>
    /// 🌟 正在输入的用户列表 (实时更新)
    /// 
    /// 技术点: 
    /// - Scan: 累积状态
    /// - 管理一个Set来跟踪正在输入的用户
    /// </summary>
    public IObservable<HashSet<string>> TypingUsers =>
        _typingSubject
            .Scan(new HashSet<string>(), (set, tuple) =>
            {
                var newSet = new HashSet<string>(set);
                if (tuple.IsTyping)
                    newSet.Add(tuple.User);
                else
                    newSet.Remove(tuple.User);
                return newSet;
            })
            .DistinctUntilChanged(HashSet<string>.CreateSetComparer());

    /// <summary>
    /// 🌟 消息速率 (每5秒的消息数)
    /// 
    /// 技术点:
    /// - Buffer: 按时间窗口聚合
    /// - Select: 转换为数量
    /// </summary>
    public IObservable<int> MessageRate =>
        _messageSubject
            .Buffer(TimeSpan.FromSeconds(5))
            .Select(buffer => buffer.Count);

    /// <summary>
    /// 🌟 高频消息警告 (每分钟超过30条)
    /// 
    /// 技术点:
    /// - Buffer: 时间窗口
    /// - Where: 条件过滤
    /// </summary>
    public IObservable<string> SpamWarning =>
        _messageSubject
            .Buffer(TimeSpan.FromMinutes(1))
            .Where(buffer => buffer.Count > 30)
            .Select(buffer => 
            {
                var topSender = buffer
                    .GroupBy(m => m.User)
                    .OrderByDescending(g => g.Count())
                    .First();
                return $"警告: {topSender.Key} 在1分钟内发送了 {topSender.Count()} 条消息!";
            });

    /// <summary>
    /// 🌟 用户活动摘要 (上线/离线合并)
    /// 
    /// 技术点:
    /// - Merge: 合并多个流
    /// - Select: 统一格式
    /// </summary>
    public IObservable<string> UserActivity =>
        _userJoinedSubject
            .Select(u => $"👋 {u.UserName} 上线了")
            .Merge(_userLeftSubject.Select(u => $"👋 {u.UserName} 离线了"));

    /// <summary>
    /// 🌟 CPU 告警 (连续3次超过80%)
    /// 
    /// 技术点:
    /// - Buffer: 滑动窗口
    /// - Where: 条件判断
    /// </summary>
    public IObservable<double> CpuAlert =>
        _monitoringSubject
            .Select(m => m.CpuUsage)
            .Buffer(3, 1)  // 每次滑动1个，取3个
            .Where(buffer => buffer.Count == 3 && buffer.All(cpu => cpu > 80))
            .Select(buffer => buffer.Average());

    /// <summary>
    /// 🌟 消息搜索 (带防抖)
    /// 
    /// 技术点:
    /// - Throttle: 防抖
    /// - DistinctUntilChanged: 去重
    /// - SelectMany: 异步处理
    /// </summary>
    public IObservable<IEnumerable<ChatMessage>> CreateSearchObservable(
        IObservable<string> searchTerms)
    {
        return searchTerms
            .Throttle(TimeSpan.FromMilliseconds(300))
            .DistinctUntilChanged()
            .Where(term => term.Length >= 2)
            .SelectMany(term => 
                _messageSubject
                    .ToList()  // 收集所有消息
                    .Select(messages => 
                        messages.Where(m => 
                            m.Message.Contains(term, StringComparison.OrdinalIgnoreCase))));
    }

    /// <summary>
    /// 🌟 最近消息缓存 (保留最近50条)
    /// 
    /// 技术点:
    /// - ReplaySubject: 缓存历史
    /// </summary>
    public IObservable<ChatMessage> RecentMessages { get; }

    #endregion

    public ReactiveSignalRService()
    {
        // 创建带缓存的消息流
        var replaySubject = new ReplaySubject<ChatMessage>(bufferSize: 50);
        _messageSubject.Subscribe(replaySubject);
        RecentMessages = replaySubject.AsObservable();
    }

    #region 接收 SignalR 事件 (由 SignalRService 调用)

    public void OnMessageReceived(ChatMessage message) => _messageSubject.OnNext(message);
    public void OnUserJoined(ConnectionStatus user) => _userJoinedSubject.OnNext(user);
    public void OnUserLeft(ConnectionStatus user) => _userLeftSubject.OnNext(user);
    public void OnTypingStatusChanged(string user, bool isTyping) => _typingSubject.OnNext((user, isTyping));
    public void OnMonitoringDataReceived(MonitoringDataPoint data) => _monitoringSubject.OnNext(data);

    #endregion

    #region 使用示例

    /// <summary>
    /// 演示如何订阅这些 Observable
    /// </summary>
    public void DemoSubscriptions()
    {
        // 示例1: 订阅消息并显示
        Messages.Subscribe(msg =>
        {
            Console.WriteLine($"[消息] {msg.User}: {msg.Message}");
        });

        // 示例2: 订阅正在输入的用户
        TypingUsers.Subscribe(users =>
        {
            if (users.Count > 0)
                Console.WriteLine($"[输入中] {string.Join(", ", users)}");
        });

        // 示例3: 订阅消息速率
        MessageRate.Subscribe(rate =>
        {
            Console.WriteLine($"[速率] 最近5秒: {rate} 条消息");
        });

        // 示例4: 订阅用户活动
        UserActivity.Subscribe(activity =>
        {
            Console.WriteLine(activity);
        });

        // 示例5: CPU 告警
        CpuAlert.Subscribe(avgCpu =>
        {
            Console.WriteLine($"⚠️ CPU 告警! 平均使用率: {avgCpu:F1}%");
        });
    }

    #endregion

    public void Dispose()
    {
        _messageSubject.Dispose();
        _userJoinedSubject.Dispose();
        _userLeftSubject.Dispose();
        _typingSubject.Dispose();
        _monitoringSubject.Dispose();
        _disposables.Dispose();
    }
}
