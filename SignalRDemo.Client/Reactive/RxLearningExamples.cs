using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;

namespace SignalRDemo.Client.Reactive;

/// <summary>
/// 响应式编程 (Rx) 学习示例
/// 
/// Rx 核心概念:
/// - Observable: 可观察的数据流 (生产者)
/// - Observer: 观察者 (消费者)  
/// - Subscription: 订阅 (连接生产者和消费者)
/// - Operators: 操作符 (转换、过滤、组合数据流)
/// </summary>
public static class RxLearningExamples
{
    #region 1. 基础概念 - 创建 Observable

    /// <summary>
    /// 示例1: 从集合创建 Observable
    /// </summary>
    public static void Example1_FromCollection()
    {
        Console.WriteLine("=== 示例1: 从集合创建 Observable ===");
        
        // 从数组创建
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var observable = numbers.ToObservable();
        
        // 订阅 - 三个回调: OnNext, OnError, OnCompleted
        observable.Subscribe(
            onNext: x => Console.WriteLine($"收到: {x}"),
            onError: ex => Console.WriteLine($"错误: {ex.Message}"),
            onCompleted: () => Console.WriteLine("完成!")
        );
        
        // 输出:
        // 收到: 1
        // 收到: 2
        // 收到: 3
        // 收到: 4
        // 收到: 5
        // 完成!
    }

    /// <summary>
    /// 示例2: 使用 Observable.Create 手动创建
    /// </summary>
    public static void Example2_ManualCreate()
    {
        Console.WriteLine("\n=== 示例2: 手动创建 Observable ===");
        
        var observable = Observable.Create<string>(observer =>
        {
            observer.OnNext("第一条消息");
            observer.OnNext("第二条消息");
            observer.OnNext("第三条消息");
            observer.OnCompleted();
            
            // 返回清理逻辑
            return Disposable.Empty;
        });
        
        observable.Subscribe(
            msg => Console.WriteLine($"收到: {msg}"),
            () => Console.WriteLine("流结束")
        );
    }

    /// <summary>
    /// 示例3: 定时器 Observable
    /// </summary>
    public static async Task Example3_TimerAsync()
    {
        Console.WriteLine("\n=== 示例3: 定时器 Observable ===");
        
        // 每秒发送一个值，共5个
        var timer = Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Take(5);  // 只取5个
        
        var cts = new TaskCompletionSource();
        
        timer.Subscribe(
            tick => Console.WriteLine($"Tick: {tick}"),
            () => cts.SetResult()
        );
        
        await cts.Task;
    }

    #endregion

    #region 2. 核心操作符

    /// <summary>
    /// 示例4: 转换操作符 - Select (Map)
    /// </summary>
    public static void Example4_Select()
    {
        Console.WriteLine("\n=== 示例4: Select 转换 ===");
        
        var numbers = Observable.Range(1, 5);
        
        // Select = Map: 转换每个元素
        numbers
            .Select(x => x * x)  // 平方
            .Subscribe(x => Console.WriteLine($"{x}"));
        
        // 输出: 1, 4, 9, 16, 25
    }

    /// <summary>
    /// 示例5: 过滤操作符 - Where (Filter)
    /// </summary>
    public static void Example5_Where()
    {
        Console.WriteLine("\n=== 示例5: Where 过滤 ===");
        
        Observable.Range(1, 10)
            .Where(x => x % 2 == 0)  // 只要偶数
            .Subscribe(x => Console.WriteLine($"偶数: {x}"));
        
        // 输出: 2, 4, 6, 8, 10
    }

    /// <summary>
    /// 示例6: 防抖 - Throttle (实用！搜索框常用)
    /// </summary>
    public static async Task Example6_ThrottleAsync()
    {
        Console.WriteLine("\n=== 示例6: Throttle 防抖 ===");
        
        // 模拟用户快速输入
        var subject = new Subject<string>();
        
        // 用户停止输入 500ms 后才处理
        subject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(text => Console.WriteLine($"搜索: {text}"));
        
        // 模拟快速输入
        subject.OnNext("h");
        await Task.Delay(100);
        subject.OnNext("he");
        await Task.Delay(100);
        subject.OnNext("hel");
        await Task.Delay(100);
        subject.OnNext("hell");
        await Task.Delay(100);
        subject.OnNext("hello");  // 只有这个会被处理
        
        await Task.Delay(600);  // 等待 throttle
        
        // 输出: 搜索: hello (只输出最后一个!)
    }

    /// <summary>
    /// 示例7: 去重 - DistinctUntilChanged
    /// </summary>
    public static void Example7_DistinctUntilChanged()
    {
        Console.WriteLine("\n=== 示例7: DistinctUntilChanged 去重 ===");
        
        var values = new[] { 1, 1, 2, 2, 2, 3, 1, 1 }.ToObservable();
        
        values
            .DistinctUntilChanged()  // 连续相同的只保留一个
            .Subscribe(x => Console.WriteLine($"值: {x}"));
        
        // 输出: 1, 2, 3, 1 (注意最后的1保留了，因为它不是连续的)
    }

    #endregion

    #region 3. 组合操作符

    /// <summary>
    /// 示例8: 合并多个流 - Merge
    /// </summary>
    public static async Task Example8_MergeAsync()
    {
        Console.WriteLine("\n=== 示例8: Merge 合并流 ===");
        
        // 两个独立的数据源
        var source1 = Observable.Interval(TimeSpan.FromMilliseconds(300))
            .Select(x => $"A-{x}")
            .Take(3);
            
        var source2 = Observable.Interval(TimeSpan.FromMilliseconds(500))
            .Select(x => $"B-{x}")
            .Take(3);
        
        // 合并成一个流
        var merged = source1.Merge(source2);
        
        var cts = new TaskCompletionSource();
        merged.Subscribe(
            x => Console.WriteLine(x),
            () => cts.SetResult()
        );
        
        await cts.Task;
        // 输出: A-0, B-0, A-1, A-2, B-1, B-2 (交错)
    }

    /// <summary>
    /// 示例9: 组合最新值 - CombineLatest
    /// </summary>
    public static async Task Example9_CombineLatestAsync()
    {
        Console.WriteLine("\n=== 示例9: CombineLatest 组合最新 ===");
        
        var firstName = new Subject<string>();
        var lastName = new Subject<string>();
        
        // 当任一个变化时，组合两个最新值
        firstName.CombineLatest(lastName, (first, last) => $"{first} {last}")
            .Subscribe(fullName => Console.WriteLine($"全名: {fullName}"));
        
        firstName.OnNext("张");      // 等待 lastName
        lastName.OnNext("三");       // 输出: 张 三
        firstName.OnNext("李");      // 输出: 李 三
        lastName.OnNext("四");       // 输出: 李 四
    }

    /// <summary>
    /// 示例10: 扁平化 - SelectMany (FlatMap)
    /// </summary>
    public static async Task Example10_SelectManyAsync()
    {
        Console.WriteLine("\n=== 示例10: SelectMany 扁平化 ===");
        
        // 模拟: 用户ID -> 获取用户详情 (异步)
        var userIds = new[] { 1, 2, 3 }.ToObservable();
        
        userIds
            .SelectMany(id => GetUserAsync(id).ToObservable())
            .Subscribe(user => Console.WriteLine($"用户: {user}"));
        
        await Task.Delay(500);
    }
    
    private static async Task<string> GetUserAsync(int id)
    {
        await Task.Delay(100);
        return $"User_{id}";
    }

    #endregion

    #region 4. 错误处理

    /// <summary>
    /// 示例11: 错误重试 - Retry
    /// </summary>
    public static void Example11_Retry()
    {
        Console.WriteLine("\n=== 示例11: Retry 重试 ===");
        
        var attempts = 0;
        
        var unreliable = Observable.Create<string>(observer =>
        {
            attempts++;
            Console.WriteLine($"尝试第 {attempts} 次...");
            
            if (attempts < 3)
            {
                observer.OnError(new Exception("连接失败"));
            }
            else
            {
                observer.OnNext("成功!");
                observer.OnCompleted();
            }
            
            return Disposable.Empty;
        });
        
        unreliable
            .Retry(3)  // 最多重试3次
            .Subscribe(
                x => Console.WriteLine(x),
                ex => Console.WriteLine($"最终失败: {ex.Message}")
            );
        
        // 输出:
        // 尝试第 1 次...
        // 尝试第 2 次...
        // 尝试第 3 次...
        // 成功!
    }

    /// <summary>
    /// 示例12: 错误恢复 - Catch
    /// </summary>
    public static void Example12_Catch()
    {
        Console.WriteLine("\n=== 示例12: Catch 错误恢复 ===");
        
        var problematic = Observable.Create<int>(observer =>
        {
            observer.OnNext(1);
            observer.OnNext(2);
            observer.OnError(new Exception("出错了!"));
            return Disposable.Empty;
        });
        
        problematic
            .Catch<int, Exception>(ex => 
            {
                Console.WriteLine($"捕获错误: {ex.Message}");
                return Observable.Return(-1);  // 返回默认值
            })
            .Subscribe(x => Console.WriteLine($"值: {x}"));
        
        // 输出: 1, 2, 捕获错误: 出错了!, 值: -1
    }

    #endregion

    #region 5. Subject (可读可写的流)

    /// <summary>
    /// 示例13: Subject 基础
    /// </summary>
    public static void Example13_Subject()
    {
        Console.WriteLine("\n=== 示例13: Subject 可读可写流 ===");
        
        // Subject 既是 Observable 又是 Observer
        var subject = new Subject<string>();
        
        // 订阅1
        subject.Subscribe(x => Console.WriteLine($"订阅者A: {x}"));
        
        // 订阅2
        subject.Subscribe(x => Console.WriteLine($"订阅者B: {x}"));
        
        // 发送数据
        subject.OnNext("消息1");
        subject.OnNext("消息2");
        
        // 输出:
        // 订阅者A: 消息1
        // 订阅者B: 消息1
        // 订阅者A: 消息2
        // 订阅者B: 消息2
    }

    /// <summary>
    /// 示例14: BehaviorSubject (记住最新值)
    /// </summary>
    public static void Example14_BehaviorSubject()
    {
        Console.WriteLine("\n=== 示例14: BehaviorSubject ===");
        
        // 有初始值，新订阅者立即收到最新值
        var behavior = new BehaviorSubject<string>("初始值");
        
        behavior.OnNext("值1");
        behavior.OnNext("值2");
        
        // 后来的订阅者也能收到最新值
        behavior.Subscribe(x => Console.WriteLine($"收到: {x}"));
        
        // 输出: 收到: 值2 (立即收到最新值!)
    }

    /// <summary>
    /// 示例15: ReplaySubject (重放历史)
    /// </summary>
    public static void Example15_ReplaySubject()
    {
        Console.WriteLine("\n=== 示例15: ReplaySubject ===");
        
        // 缓存最近2个值
        var replay = new ReplaySubject<int>(bufferSize: 2);
        
        replay.OnNext(1);
        replay.OnNext(2);
        replay.OnNext(3);
        replay.OnNext(4);
        
        // 新订阅者收到最近2个值
        replay.Subscribe(x => Console.WriteLine($"收到: {x}"));
        
        // 输出: 收到: 3, 收到: 4
    }

    #endregion

    #region 6. 实际应用场景

    /// <summary>
    /// 示例16: 搜索框防抖 + 去重 + 异步搜索 (最常用!)
    /// </summary>
    public static async Task Example16_SearchBoxAsync()
    {
        Console.WriteLine("\n=== 示例16: 搜索框实战 ===");
        
        var searchInput = new Subject<string>();
        
        // 完整的搜索流程
        searchInput
            .Throttle(TimeSpan.FromMilliseconds(300))   // 防抖
            .DistinctUntilChanged()                      // 去重
            .Where(text => text.Length >= 2)             // 至少2个字符
            .SelectMany(query => SearchApiAsync(query).ToObservable())  // 异步搜索
            .Subscribe(
                results => Console.WriteLine($"搜索结果: {string.Join(", ", results)}"),
                ex => Console.WriteLine($"搜索失败: {ex.Message}")
            );
        
        // 模拟用户输入
        searchInput.OnNext("a");        // 忽略 (少于2字符)
        searchInput.OnNext("ap");       // 等待防抖
        await Task.Delay(100);
        searchInput.OnNext("app");      // 覆盖上一个
        await Task.Delay(100);
        searchInput.OnNext("apple");    // 覆盖上一个
        await Task.Delay(400);          // 触发搜索 "apple"
        
        searchInput.OnNext("apple");    // 忽略 (和上次相同)
        searchInput.OnNext("banana");
        await Task.Delay(400);          // 触发搜索 "banana"
        
        await Task.Delay(300);
    }
    
    private static async Task<string[]> SearchApiAsync(string query)
    {
        Console.WriteLine($"  [API调用] 搜索: {query}");
        await Task.Delay(100);
        return new[] { $"{query}_结果1", $"{query}_结果2" };
    }

    /// <summary>
    /// 示例17: 点击按钮防连点
    /// </summary>
    public static async Task Example17_ButtonThrottleAsync()
    {
        Console.WriteLine("\n=== 示例17: 按钮防连点 ===");
        
        var buttonClicks = new Subject<DateTime>();
        
        // 1秒内只处理第一次点击
        buttonClicks
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(time => Console.WriteLine($"处理点击: {time:HH:mm:ss.fff}"));
        
        // 快速点击5次
        for (int i = 0; i < 5; i++)
        {
            buttonClicks.OnNext(DateTime.Now);
            Console.WriteLine($"点击 {i + 1}");
            await Task.Delay(200);
        }
        
        await Task.Delay(1100);
        // 输出: 只处理了1次!
    }

    /// <summary>
    /// 示例18: 监控数据流处理
    /// </summary>
    public static async Task Example18_MonitoringAsync()
    {
        Console.WriteLine("\n=== 示例18: 监控数据处理 ===");
        
        // 模拟服务器监控数据流
        var cpuData = Observable.Interval(TimeSpan.FromMilliseconds(500))
            .Select(_ => Random.Shared.Next(0, 100))
            .Take(10);
        
        cpuData
            .Buffer(TimeSpan.FromSeconds(2))  // 每2秒聚合
            .Where(buffer => buffer.Count > 0)
            .Subscribe(buffer =>
            {
                var avg = buffer.Select(x => (double)x).Average();
                var max = buffer.Select(x => (double)x).Max();
                Console.WriteLine($"CPU 统计 - 平均: {avg:F1}%, 最大: {max}%");
            });
        
        await Task.Delay(5500);
    }

    #endregion

    /// <summary>
    /// 运行所有示例
    /// </summary>
    public static async Task RunAllExamplesAsync()
    {
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║    Rx 响应式编程学习示例                 ║");
        Console.WriteLine("╚══════════════════════════════════════════╝\n");
        
        // 基础
        Example1_FromCollection();
        Example2_ManualCreate();
        
        // 操作符
        Example4_Select();
        Example5_Where();
        await Example6_ThrottleAsync();
        Example7_DistinctUntilChanged();
        
        // 组合
        await Example9_CombineLatestAsync();
        
        // 错误处理
        Example11_Retry();
        Example12_Catch();
        
        // Subject
        Example13_Subject();
        Example14_BehaviorSubject();
        Example15_ReplaySubject();
        
        // 实战
        await Example16_SearchBoxAsync();
        await Example17_ButtonThrottleAsync();
        
        Console.WriteLine("\n✅ 所有示例运行完成!");
    }
}
