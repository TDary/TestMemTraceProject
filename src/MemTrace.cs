using System.Runtime.InteropServices;

namespace MemTrace;

/// <summary>
/// Managed wrapper around the native MemTrace.dll. Implements IDisposable —
/// wrap in a using block or call Dispose() to release native resources.
/// </summary>
public sealed class MemTrace : IDisposable
{
    private bool _disposed;

    // ── P/Invoke ────────────────────────────────────────────────────────

    [DllImport("MemTrace", EntryPoint = "InitMemTrace")]
    private static extern bool NativeInit();

    [DllImport("MemTrace", EntryPoint = "UpdateMemory")]
    private static extern void NativeUpdateMemory();

    [DllImport("MemTrace", EntryPoint = "GetCurrentProcessMemory")]
    private static extern ulong NativeGetProcessMemory();

    [DllImport("MemTrace", EntryPoint = "GetCurrentProcessWorkingSet")]
    private static extern ulong NativeGetProcessWorkingSet();

    [DllImport("MemTrace", EntryPoint = "GetProcessMemoryByPid")]
    private static extern ulong NativeGetProcessMemoryByPid(uint pid);

    [DllImport("MemTrace", EntryPoint = "GetProcessWorkingSetByPid")]
    private static extern ulong NativeGetProcessWorkingSetByPid(uint pid);

    [DllImport("MemTrace", EntryPoint = "GetCurrentCPUUsage")]
    private static extern double NativeGetCpuUsage();

    [DllImport("MemTrace", EntryPoint = "GetProcessCPUUsageByPid")]
    private static extern double NativeGetProcessCpuUsage(uint pid);

    [DllImport("MemTrace", EntryPoint = "GetProcessDiskReadBytesByPid")]
    private static extern ulong NativeGetProcessDiskReadBytes(uint pid);

    [DllImport("MemTrace", EntryPoint = "GetProcessDiskWriteBytesByPid")]
    private static extern ulong NativeGetProcessDiskWriteBytes(uint pid);

    [DllImport("MemTrace", EntryPoint = "GetCurrentGPUUsage")]
    private static extern double NativeGetGpuUsage();

    [DllImport("MemTrace", EntryPoint = "GetCurrentDiskReadRate")]
    private static extern double NativeGetDiskReadRate();

    [DllImport("MemTrace", EntryPoint = "GetCurrentDiskWriteRate")]
    private static extern double NativeGetDiskWriteRate();

    [DllImport("MemTrace", EntryPoint = "GetCurrentNetworkRecvRate")]
    private static extern double NativeGetNetworkRecvRate();

    [DllImport("MemTrace", EntryPoint = "GetCurrentNetworkSentRate")]
    private static extern double NativeGetNetworkSentRate();

    [DllImport("MemTrace", EntryPoint = "GetCurrentAvailableMemory")]
    private static extern double NativeGetAvailableMemory();

    [DllImport("MemTrace", EntryPoint = "UninitMemTrace")]
    private static extern void NativeUninit();

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>初始化 MemTrace，开始采集性能数据。</summary>
    public static bool Init() => NativeInit();

    /// <summary>更新当前进程内存计数器。GetProcessMemory / GetProcessWorkingSet 之前需调用。</summary>
    public void UpdateMemory() => NativeUpdateMemory();

    /// <summary>当前进程提交内存（PagefileUsage），单位字节。</summary>
    public ulong GetProcessMemory()
    {
        return NativeGetProcessMemory();
    }

    /// <summary>当前进程物理内存工作集（WorkingSetSize），单位字节。</summary>
    public ulong GetProcessWorkingSet()
    {
        return NativeGetProcessWorkingSet();
    }

    /// <summary>指定进程提交内存（PagefileUsage），单位字节。无需先调用 UpdateMemory。</summary>
    public static ulong GetProcessMemory(uint pid) => NativeGetProcessMemoryByPid(pid);

    /// <summary>指定进程物理内存工作集（WorkingSetSize），单位字节。无需先调用 UpdateMemory。</summary>
    public static ulong GetProcessWorkingSet(uint pid) => NativeGetProcessWorkingSetByPid(pid);

    /// <summary>系统整体 CPU 使用率（0~100%），非阻塞。</summary>
    public static double GetCpuUsage() => NativeGetCpuUsage();

    /// <summary>指定进程 CPU 使用率。首次调用返回 0，后续基于两次采样差值计算。</summary>
    public static double GetProcessCpuUsage(uint pid) => NativeGetProcessCpuUsage(pid);

    /// <summary>指定进程累计磁盘读取字节数。</summary>
    public static ulong GetProcessDiskReadBytes(uint pid) => NativeGetProcessDiskReadBytes(pid);

    /// <summary>指定进程累计磁盘写入字节数。</summary>
    public static ulong GetProcessDiskWriteBytes(uint pid) => NativeGetProcessDiskWriteBytes(pid);

    /// <summary>系统整体 GPU 使用率（0~100%）。</summary>
    public static double GetGpuUsage() => NativeGetGpuUsage();

    /// <summary>磁盘读速率，单位字节/秒。</summary>
    public static double GetDiskReadRate() => NativeGetDiskReadRate();

    /// <summary>磁盘写速率，单位字节/秒。</summary>
    public static double GetDiskWriteRate() => NativeGetDiskWriteRate();

    /// <summary>网络接收速率，单位字节/秒。</summary>
    public static double GetNetworkRecvRate() => NativeGetNetworkRecvRate();

    /// <summary>网络发送速率，单位字节/秒。</summary>
    public static double GetNetworkSentRate() => NativeGetNetworkSentRate();

    /// <summary>系统可用内存，单位 MB。</summary>
    public static double GetAvailableMemory() => NativeGetAvailableMemory();

    /// <summary>采集一次性能快照。</summary>
    public Snapshot TakeSnapshot()
    {
        UpdateMemory();
        return new Snapshot
        {
            ProcessMemory = NativeGetProcessMemory(),
            ProcessWorkingSet = NativeGetProcessWorkingSet(),
            CpuUsage = NativeGetCpuUsage(),
            GpuUsage = NativeGetGpuUsage(),
            AvailableMemory = NativeGetAvailableMemory(),
            DiskReadRate = NativeGetDiskReadRate(),
            DiskWriteRate = NativeGetDiskWriteRate(),
            NetworkRecvRate = NativeGetNetworkRecvRate(),
            NetworkSentRate = NativeGetNetworkSentRate(),
        };
    }

    // ── IDisposable ─────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeUninit();
            _disposed = true;
        }
    }
}

/// <summary>单次性能采样快照。</summary>
public struct Snapshot
{
    public ulong ProcessMemory;
    public ulong ProcessWorkingSet;
    public double CpuUsage;
    public double GpuUsage;
    public double AvailableMemory;
    public double DiskReadRate;
    public double DiskWriteRate;
    public double NetworkRecvRate;
    public double NetworkSentRate;
}
