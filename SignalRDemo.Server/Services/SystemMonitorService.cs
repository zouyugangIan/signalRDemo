using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 系统监控服务 - 获取真实的 CPU、内存、网络数据
/// </summary>
public class SystemMonitorService
{
    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private DateTime _lastCpuTime = DateTime.UtcNow;
    private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private long _lastBytesReceived = 0;
    private long _lastBytesSent = 0;
    private DateTime _lastNetworkTime = DateTime.UtcNow;

    public SystemMonitorService()
    {
        // 初始化 CPU 时间
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _lastCpuTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 获取 CPU 使用率 (进程级别)
    /// </summary>
    public double GetCpuUsage()
    {
        try
        {
            _currentProcess.Refresh();
            var currentTime = DateTime.UtcNow;
            var currentCpuTime = _currentProcess.TotalProcessorTime;

            var cpuUsedMs = (currentCpuTime - _lastTotalProcessorTime).TotalMilliseconds;
            var totalTimeMs = (currentTime - _lastCpuTime).TotalMilliseconds;

            _lastTotalProcessorTime = currentCpuTime;
            _lastCpuTime = currentTime;

            if (totalTimeMs <= 0) return 0;

            // CPU 使用率 = (进程 CPU 时间 / 总时间) / CPU 核心数 * 100
            var cpuUsage = cpuUsedMs / totalTimeMs / Environment.ProcessorCount * 100;
            return Math.Min(cpuUsage, 100);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取内存使用率
    /// </summary>
    public double GetMemoryUsage()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryUsage();
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetLinuxMemoryUsage()
    {
        try
        {
            var memInfo = File.ReadAllLines("/proc/meminfo");
            long totalMem = 0, availableMem = 0;

            foreach (var line in memInfo)
            {
                if (line.StartsWith("MemTotal:"))
                    totalMem = ParseMemInfoValue(line);
                else if (line.StartsWith("MemAvailable:"))
                    availableMem = ParseMemInfoValue(line);
            }

            if (totalMem > 0)
            {
                return (1.0 - (double)availableMem / totalMem) * 100;
            }
        }
        catch { }
        return 0;
    }

    private double GetWindowsMemoryUsage()
    {
        // 使用进程内存作为近似
        _currentProcess.Refresh();
        var usedMemory = _currentProcess.WorkingSet64;
        var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        
        if (totalMemory > 0)
            return (double)usedMemory / totalMemory * 100;
        
        return 0;
    }

    private static long ParseMemInfoValue(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out var value))
            return value;
        return 0;
    }

    /// <summary>
    /// 获取网络入站流量 (MB/s)
    /// </summary>
    public double GetNetworkBytesReceivedPerSec()
    {
        try
        {
            var currentBytes = GetTotalBytesReceived();
            var currentTime = DateTime.UtcNow;
            var elapsed = (currentTime - _lastNetworkTime).TotalSeconds;

            if (elapsed > 0 && _lastBytesReceived > 0)
            {
                var bytesPerSec = (currentBytes - _lastBytesReceived) / elapsed;
                _lastBytesReceived = currentBytes;
                _lastNetworkTime = currentTime;
                return bytesPerSec / 1024 / 1024; // 转换为 MB/s
            }

            _lastBytesReceived = currentBytes;
            _lastNetworkTime = currentTime;
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取网络出站流量 (MB/s)
    /// </summary>
    public double GetNetworkBytesSentPerSec()
    {
        try
        {
            var currentBytes = GetTotalBytesSent();
            var currentTime = DateTime.UtcNow;
            var elapsed = (currentTime - _lastNetworkTime).TotalSeconds;

            if (elapsed > 0 && _lastBytesSent > 0)
            {
                var bytesPerSec = (currentBytes - _lastBytesSent) / elapsed;
                _lastBytesSent = currentBytes;
                return Math.Max(0, bytesPerSec / 1024 / 1024);
            }

            _lastBytesSent = currentBytes;
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private long GetTotalBytesReceived()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxNetworkBytes("rx_bytes");
        }
        return 0;
    }

    private long GetTotalBytesSent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxNetworkBytes("tx_bytes");
        }
        return 0;
    }

    private static long GetLinuxNetworkBytes(string metric)
    {
        try
        {
            long total = 0;
            var netDir = "/sys/class/net";
            
            if (Directory.Exists(netDir))
            {
                foreach (var iface in Directory.GetDirectories(netDir))
                {
                    var name = Path.GetFileName(iface);
                    if (name == "lo") continue; // 跳过回环
                    
                    var statFile = Path.Combine(iface, "statistics", metric);
                    if (File.Exists(statFile))
                    {
                        var value = File.ReadAllText(statFile).Trim();
                        if (long.TryParse(value, out var bytes))
                            total += bytes;
                    }
                }
            }
            return total;
        }
        catch
        {
            return 0;
        }
    }
}
