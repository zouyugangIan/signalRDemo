# SignalRDemo

## 快速启动（Ubuntu，直接命令）

先在项目根目录执行一次依赖恢复（首次）：

```fish
dotnet restore SignalRDemo.slnx
```

## 分开启动（推荐）

```fish
# 终端 A：启动服务器
dotnet run --project SignalRDemo.Server --launch-profile http
```

```fish
# 终端 B：打开网页
xdg-open http://127.0.0.1:5072
```

```fish
# 终端 C：启动桌面端（Ubuntu/Avalonia）
dotnet run --project SignalRDemo.Client
```

## 一键启动（可选）

```fish
# 后台启动服务器
dotnet run --project SignalRDemo.Server --launch-profile http >/tmp/srd-server.log 2>&1 &
set SERVER_PID $last_pid

# 打开网页 + 启动桌面端
xdg-open http://127.0.0.1:5072
dotnet run --project SignalRDemo.Client

# 结束后关闭后台服务器
kill $SERVER_PID
```

注意：
- 桌面端默认服务器地址是 `http://127.0.0.1:5072`。
- 如果你要连接局域网服务器，直接在桌面端界面里改地址。
- 也可以启动前设置环境变量覆盖默认值：

```fish
set -x SIGNALRDEMO_SERVER_URL http://192.168.1.116:5072
dotnet run --project SignalRDemo.Client
```

## 桌面端编译（含 macOS）

使用 fish 脚本（不改 shell 配置）：

```fish
# 全平台一起编译（linux/win/macos x64+arm64）
fish scripts/build-desktop.fish
```

```fish
# 只编译 macOS Intel
fish scripts/build-desktop.fish osx-x64
```

```fish
# 只编译 macOS Apple Silicon
fish scripts/build-desktop.fish osx-arm64
```

输出目录：`artifacts/publish/<RID>/`
