# SignalRDemo

SignalR + Avalonia 桌面客户端示例。项目包含：

- `SignalRDemo.Server`: ASP.NET Core SignalR 服务端，同时托管 `wwwroot` 里的网页客户端。
- `SignalRDemo.Client`: Avalonia 桌面客户端。
- `SignalRDemo.Shared`: 服务端和客户端共享的 DTO、Hub 常量和接口。

商业化升级规划和任务拆解见 [docs/CommercializationTasks.md](docs/CommercializationTasks.md)。

## 运行环境

本项目目标框架是 `net10.0`，请先安装支持 .NET 10 的 SDK。

macOS Apple Silicon（M1/M2/M3/M4）建议安装 .NET 10 SDK 的 macOS Arm64 版本，不要只安装 Runtime。

### macOS 安装 .NET 10 SDK

推荐方式：打开 .NET 官方下载页，选择 `.NET 10.0`、`SDK`、`macOS Arm64` 安装包：

```text
https://dotnet.microsoft.com/download/dotnet/10.0
```

如果你不想用安装包，也可以用官方安装脚本安装到当前用户目录：

```zsh
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

脚本安装后，当前终端先临时加入 PATH：

```zsh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
```

如果确认可用，再写入 zsh 配置，之后新终端也能直接使用 `dotnet`：

```zsh
printf '\nexport DOTNET_ROOT="$HOME/.dotnet"\nexport PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"\n' >> ~/.zprofile
source ~/.zprofile
```

安装后重新打开终端，并确认：

```zsh
uname -m
dotnet --info
dotnet --list-sdks
```

期望 `uname -m` 输出 `arm64`，并且 `dotnet --list-sdks` 能看到 `10.x` SDK。

如果终端提示 `zsh: command not found: dotnet`，说明当前终端找不到 .NET SDK。先执行：

```zsh
command -v dotnet
```

如果没有任何输出，回到上面的安装步骤；如果有输出但仍然无法运行，再检查 `DOTNET_ROOT` 和 `PATH`。

## macOS M4 启动方式（推荐）

在项目根目录执行一次依赖恢复：

```zsh
dotnet restore SignalRDemo.slnx
```

服务端是一个常驻进程，启动后会占用当前终端，所以客户端需要在另一个终端标签页里启动。

### 终端 A：启动服务器

```zsh
dotnet run --project SignalRDemo.Server --launch-profile http
```

看到类似 `Now listening on: http://0.0.0.0:5072` 后，服务端已经启动。

### 终端 B：打开网页客户端

```zsh
open http://127.0.0.1:5072
```

### 终端 C：启动桌面客户端

```zsh
dotnet run --project SignalRDemo.Client
```

桌面端默认连接地址是：

```text
http://127.0.0.1:5072
```

如果要连接局域网里的服务器，可以直接在桌面端界面里修改地址，或启动前设置环境变量：

```zsh
export SIGNALRDEMO_SERVER_URL="http://192.168.1.116:5072"
dotnet run --project SignalRDemo.Client
```

## macOS 一键启动（可选）

如果你只想用一个终端跑起来，可以用下面的 zsh 命令：它会在后台启动服务器，打开网页，然后启动桌面端；桌面端退出后会尝试关闭后台服务器。

```zsh
(
if ! command -v dotnet >/dev/null 2>&1; then
  echo "未找到 dotnet。请先安装 .NET 10 SDK，并重新打开终端。"
  exit 1
fi

dotnet run --project SignalRDemo.Server --launch-profile http >/tmp/srd-server.log 2>&1 &
SERVER_PID=$!
trap 'kill $SERVER_PID 2>/dev/null' EXIT

until curl -fsS http://127.0.0.1:5072/health >/dev/null 2>&1; do
  if ! kill -0 $SERVER_PID 2>/dev/null; then
    echo "服务端启动失败，日志如下："
    tail -n 80 /tmp/srd-server.log
    exit 1
  fi
  sleep 1
done

open http://127.0.0.1:5072
dotnet run --project SignalRDemo.Client
)
```

如果客户端连接失败，先看服务端日志：

```zsh
tail -f /tmp/srd-server.log
```

## 常见问题

### `dotnet: command not found`

说明当前终端找不到 .NET SDK。先确认命令是否存在：

```zsh
command -v dotnet
```

如果没有输出，安装 .NET 10 SDK 的 macOS Arm64 版本。如果你是用官方安装脚本安装到 `~/.dotnet`，执行：

```zsh
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
dotnet --info
```

macOS Apple Silicon 上也要确认当前终端不是 x64/Rosetta 环境：

```zsh
uname -m
```

应输出 `arm64`。

### `NETSDK1045` 或提示当前 SDK 不支持 `net10.0`

说明安装的 SDK 太旧。安装 .NET 10 SDK 或更高版本后再运行：

```zsh
dotnet --list-sdks
dotnet restore SignalRDemo.slnx
```

### 端口 `5072` 被占用

查找占用端口的进程：

```zsh
lsof -nP -iTCP:5072 -sTCP:LISTEN
```

确认是旧的本项目服务端后再结束它：

```zsh
kill <PID>
```

### 网页能打开，但桌面端连不上

确认桌面端连接地址是：

```text
http://127.0.0.1:5072
```

如果服务端运行在另一台机器上，服务端当前使用 `http://0.0.0.0:5072` 监听，局域网客户端应填入服务器的局域网 IP，例如：

```text
http://192.168.1.116:5072
```

## Ubuntu / Linux 启动方式

首次恢复依赖：

```bash
dotnet restore SignalRDemo.slnx
```

终端 A 启动服务器：

```bash
dotnet run --project SignalRDemo.Server --launch-profile http
```

终端 B 打开网页：

```bash
xdg-open http://127.0.0.1:5072
```

终端 C 启动桌面端：

```bash
dotnet run --project SignalRDemo.Client
```

设置服务器地址：

```bash
export SIGNALRDEMO_SERVER_URL="http://192.168.1.116:5072"
dotnet run --project SignalRDemo.Client
```

## 桌面端发布

使用 fish 脚本发布桌面端：

```fish
# 全平台一起发布：linux/win/macos x64+arm64
fish scripts/build-desktop.fish
```

```fish
# 只发布 macOS Intel
fish scripts/build-desktop.fish osx-x64
```

```fish
# 只发布 macOS Apple Silicon / M 系列芯片
fish scripts/build-desktop.fish osx-arm64
```

输出目录：

```text
artifacts/publish/<RID>/
```
