using System.Diagnostics;
using MemTrace;

Console.WriteLine("=== MemTrace Test Program ===");
Console.WriteLine($"OS: {Environment.OSVersion}");
Console.WriteLine($"64-bit: {Environment.Is64BitProcess}");
Console.WriteLine();

if (!MemTrace.MemTrace.Init())
{
    Console.WriteLine("[FAIL] InitMemTrace returned false.");
    return;
}
Console.WriteLine("[OK] InitMemTrace succeeded.");

uint currentPid = (uint)Environment.ProcessId;
Console.WriteLine($"Current PID: {currentPid}");
Console.WriteLine();

using var mt = new MemTrace.MemTrace();
Thread.Sleep(500);

// ── 进程内存 ───────────────────────────────────────────────────────────
Console.WriteLine("── Process Memory ──");
var mem = mt.GetProcessMemory();
var ws = mt.GetProcessWorkingSet();
Console.WriteLine($"  Commit (PagefileUsage): {mem / (1024.0 * 1024.0):F2} MB");
Console.WriteLine($"  WorkingSet:             {ws / (1024.0 * 1024.0):F2} MB");

var memByPid = MemTrace.MemTrace.GetProcessMemory(currentPid);
var wsByPid = MemTrace.MemTrace.GetProcessWorkingSet(currentPid);
Console.WriteLine($"  Commit  (by PID):       {memByPid / (1024.0 * 1024.0):F2} MB");
Console.WriteLine($"  WorkingSet (by PID):    {wsByPid / (1024.0 * 1024.0):F2} MB");

// ── CPU ─────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── CPU ──");
Console.WriteLine($"  System CPU:  {MemTrace.MemTrace.GetCpuUsage():F2}%");

MemTrace.MemTrace.GetProcessCpuUsage(currentPid);
Thread.Sleep(500);
Console.WriteLine($"  Process CPU: {MemTrace.MemTrace.GetProcessCpuUsage(currentPid):F2}%");

// ── GPU ─────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── GPU ──");
Console.WriteLine($"  GPU Usage: {MemTrace.MemTrace.GetGpuUsage():F2}%");

// ── Memory ──────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── Memory ──");
Console.WriteLine($"  Available: {MemTrace.MemTrace.GetAvailableMemory():F0} MB");

// ── Disk ────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── Disk ──");
Console.WriteLine($"  Read Rate:   {MemTrace.MemTrace.GetDiskReadRate() / 1024.0:F2} KB/s");
Console.WriteLine($"  Write Rate:  {MemTrace.MemTrace.GetDiskWriteRate() / 1024.0:F2} KB/s");
Console.WriteLine($"  Proc Read:   {MemTrace.MemTrace.GetProcessDiskReadBytes(currentPid) / 1024.0:F2} KB");
Console.WriteLine($"  Proc Write:  {MemTrace.MemTrace.GetProcessDiskWriteBytes(currentPid) / 1024.0:F2} KB");

// ── Network ─────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── Network ──");
Console.WriteLine($"  Recv Rate: {MemTrace.MemTrace.GetNetworkRecvRate() / 1024.0:F2} KB/s");
Console.WriteLine($"  Sent Rate: {MemTrace.MemTrace.GetNetworkSentRate() / 1024.0:F2} KB/s");

// ── 持续采样 ───────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("── Continuous sampling (press any key to stop) ──");
Console.WriteLine($"{"Time",-10} {"CPU%",-8} {"GPU%",-8} {"Commit(MB)",-12} {"WS(MB)",-12} {"Avail(MB)",-12} {"DiskR(KB/s)",-12} {"DiskW(KB/s)",-12}");
Console.WriteLine(new string('-', 100));

var stopwatch = Stopwatch.StartNew();
using var cts = new CancellationTokenSource();

_ = Task.Run(() =>
{
    Console.ReadKey(true);
    cts.Cancel();
});

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var s = mt.TakeSnapshot();
        var elapsed = stopwatch.Elapsed;

        Console.WriteLine(
            $"{elapsed.TotalSeconds,8:F1}s " +
            $"{s.CpuUsage,6:F1}% " +
            $"{s.GpuUsage,6:F1}% " +
            $"{s.ProcessMemory / (1024.0 * 1024.0),10:F2} " +
            $"{s.ProcessWorkingSet / (1024.0 * 1024.0),10:F2} " +
            $"{s.AvailableMemory,10:F0} " +
            $"{s.DiskReadRate / 1024.0,10:F2} " +
            $"{s.DiskWriteRate / 1024.0,10:F2}"
        );

        await Task.Delay(1000, cts.Token);
    }
}
catch (OperationCanceledException) { }

Console.WriteLine();
Console.WriteLine("=== Test complete ===");
