# Rx 响应式编程速查表

> 快速参考：常用操作符和模式

## 核心概念

```
Observable (数据流) ──► Operator (操作符) ──► Observer (观察者)
     生产者                 转换/过滤              消费者
```

## 创建 Observable

| 方法 | 说明 | 示例 |
|------|------|------|
| `Observable.Return(x)` | 发送一个值后完成 | `Observable.Return(42)` |
| `Observable.Empty<T>()` | 直接完成，不发送任何值 | `Observable.Empty<int>()` |
| `Observable.Never<T>()` | 永不发送，永不完成 | `Observable.Never<int>()` |
| `Observable.Throw<T>(ex)` | 直接发送错误 | `Observable.Throw<int>(new Exception())` |
| `Observable.Range(start, count)` | 发送连续整数 | `Observable.Range(1, 10)` |
| `Observable.Interval(time)` | 定时发送递增整数 | `Observable.Interval(TimeSpan.FromSeconds(1))` |
| `Observable.Timer(delay)` | 延迟后发送一个值 | `Observable.Timer(TimeSpan.FromSeconds(5))` |
| `collection.ToObservable()` | 从集合创建 | `new[]{1,2,3}.ToObservable()` |
| `Observable.Create<T>(...)` | 自定义创建 | 见下方示例 |

## 转换操作符

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `Select` | 转换每个元素 (Map) | `.Select(x => x * 2)` |
| `SelectMany` | 扁平化 (FlatMap) | `.SelectMany(x => GetAsync(x).ToObservable())` |
| `Cast<T>` | 类型转换 | `.Cast<string>()` |
| `OfType<T>` | 过滤+转换 | `.OfType<int>()` |

## 过滤操作符

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `Where` | 条件过滤 | `.Where(x => x > 0)` |
| `Take(n)` | 取前 n 个 | `.Take(5)` |
| `TakeLast(n)` | 取后 n 个 | `.TakeLast(3)` |
| `Skip(n)` | 跳过前 n 个 | `.Skip(2)` |
| `First` | 第一个 | `.First()` |
| `Last` | 最后一个 | `.Last()` |
| `Distinct` | 去重 | `.Distinct()` |
| `DistinctUntilChanged` | 连续去重 ⭐ | `.DistinctUntilChanged()` |
| `IgnoreElements` | 忽略所有值 | `.IgnoreElements()` |

## 时间操作符 ⭐

| 操作符 | 说明 | 应用场景 |
|--------|------|----------|
| `Throttle` | 防抖 (等待静止) | 搜索框输入 |
| `Sample` | 采样 (定时取最新) | 高频数据降采样 |
| `Delay` | 延迟发送 | 延迟通知 |
| `Timeout` | 超时报错 | 网络请求超时 |
| `Buffer(time)` | 时间窗口聚合 | 批量处理 |
| `Buffer(count)` | 数量聚合 | 批量处理 |
| `Window` | 分窗口 (返回 Observable) | 复杂聚合 |

### Throttle vs Sample vs Debounce

```
输入:  --A--B--C--------D--E---->

Throttle(300ms):  
       -----------C--------E---->  (停止输入后才发)

Sample(300ms):
       ----A--------C--------E-->  (每隔固定时间取最新)
```

## 组合操作符

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `Merge` | 合并多个流 | `s1.Merge(s2)` |
| `Concat` | 顺序连接 | `s1.Concat(s2)` |
| `Zip` | 配对合并 | `s1.Zip(s2, (a,b) => ...)` |
| `CombineLatest` | 组合最新值 ⭐ | `s1.CombineLatest(s2, ...)` |
| `WithLatestFrom` | 用另一个流的最新值 | `s1.WithLatestFrom(s2, ...)` |
| `Switch` | 切换到最新的内部流 | `.Select(...).Switch()` |

### CombineLatest vs Zip

```
s1: --1-----2-----3-->
s2: ----A-----B------>

CombineLatest: --1A--2A--2B--3B-->  (任一变化时组合最新)
Zip:           ----1A----2B------>  (配对组合)
```

## 错误处理

| 操作符 | 说明 | 示例 |
|--------|------|------|
| `Catch` | 捕获错误并恢复 | `.Catch(ex => Observable.Return(default))` |
| `Retry(n)` | 重试 n 次 | `.Retry(3)` |
| `RetryWhen` | 自定义重试逻辑 | `.RetryWhen(errors => ...)` |
| `OnErrorResumeNext` | 忽略错误继续 | `.OnErrorResumeNext(nextStream)` |

## Subject 类型

| 类型 | 特点 | 使用场景 |
|------|------|----------|
| `Subject<T>` | 基础，不缓存 | 事件总线 |
| `BehaviorSubject<T>` | 缓存最新值 ⭐ | 状态管理 |
| `ReplaySubject<T>` | 缓存多个值 | 历史回放 |
| `AsyncSubject<T>` | 只发送最后一个 | 异步结果 |

## 常用模式

### 1. 搜索框防抖

```csharp
searchInput
    .Throttle(TimeSpan.FromMilliseconds(300))
    .DistinctUntilChanged()
    .Where(text => text.Length >= 2)
    .SelectMany(query => SearchAsync(query).ToObservable())
    .Subscribe(results => UpdateUI(results));
```

### 2. 自动重试

```csharp
apiCall
    .RetryWhen(errors => errors
        .Zip(Observable.Range(1, 3), (err, i) => i)
        .SelectMany(i => Observable.Timer(TimeSpan.FromSeconds(i))))
    .Subscribe(...);
```

### 3. 状态管理

```csharp
var state = new BehaviorSubject<AppState>(initialState);

// 读取当前状态
var current = state.Value;

// 订阅状态变化
state.Subscribe(s => RenderUI(s));

// 更新状态
state.OnNext(newState);
```

### 4. 事件聚合

```csharp
clicks
    .Buffer(TimeSpan.FromMilliseconds(500))
    .Where(buffer => buffer.Count >= 2)
    .Subscribe(_ => HandleDoubleClick());
```

## 生命周期管理

```csharp
// 保存订阅
var subscription = observable.Subscribe(...);

// 取消订阅
subscription.Dispose();

// 多个订阅统一管理
var disposables = new CompositeDisposable();
disposables.Add(subscription1);
disposables.Add(subscription2);
disposables.Dispose();  // 全部取消
```

## 调试技巧

```csharp
observable
    .Do(x => Console.WriteLine($"Before: {x}"))  // 调试输出
    .Where(x => x > 0)
    .Do(x => Console.WriteLine($"After: {x}"))
    .Subscribe(...);
```

---

> 💡 **记住**: Rx 的核心是把异步事件当作数据流来处理，用声明式的方式描述数据如何流动和转换。
