#pragma once

// DLL导出类中使用STL成员的警告，同编译器下无实际影响
#pragma warning(disable: 4251)

#include <string>
#include <sstream>
#include <map>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <vector>
#include <atomic>
#include <cstdint>

#ifdef _WIN32
// winsock2 必须在 windows.h 之前
#include <winsock2.h>
#include <ws2tcpip.h>
#include <windows.h>
#include <psapi.h>
#include <d3d9.h>
#include <pdh.h>
#pragma comment(lib, "d3d9.lib")
#pragma comment(lib, "Pdh.lib")
#else
#include <cstddef>
typedef size_t SIZE_T;
typedef uint32_t DWORD;
#endif

#ifdef _WIN32
#ifdef DMemTrace_EXPORTS
#define MemTrace_API __declspec(dllexport)
#else
#define MemTrace_API __declspec(dllimport)
#endif
#else
#define MemTrace_API __attribute__((visibility("default")))
#endif

#if defined(_WIN32) || defined(_WIN64)
#define PERFM_PATH_CPU_UTILITY              L"\\Processor Information(_Total)\\% Processor Utility"
#define PERFM_PATH_CPU_PERFORMANCE          L"\\Processor Information(_Total)\\% Processor Performance"
#define PERFM_PATH_CPU_FREQUENCY            L"\\Processor Information(_Total)\\Processor Frequency"
#define PERFM_PATH_GPU_UTILITY              L"\\GPU Engine(*)\\Utilization Percentage"
#define PERFM_PATH_MEMORYAVALIABLE_UTILITY  L"\\Memory\\Available MBytes"
#define PERFM_PATH_DISK_READ_RATE           L"\\PhysicalDisk(_Total)\\Disk Read Bytes/sec"
#define PERFM_PATH_DISK_WRITE_RATE          L"\\PhysicalDisk(_Total)\\Disk Write Bytes/sec"
#define PERFM_PATH_NETWORK_RECV_RATE        L"\\Network Interface(*)\\Bytes Received/sec"
#define PERFM_PATH_NETWORK_SENT_RATE        L"\\Network Interface(*)\\Bytes Sent/sec"
#endif

#ifdef _UNICODE
using _tstring = std::wstring;
#else
using _tstring = std::string;
#endif

#ifdef _WIN32
#ifdef UNICODE
using PerfMonInfo = std::map<std::wstring, HCOUNTER>;
#else
using PerfMonInfo = std::map<std::string, HCOUNTER>;
#endif
#else
using PerfMonInfo = std::map<std::string, void*>;
#endif

class MemTrace_API DMemTrace {
public:
    DMemTrace();
    virtual ~DMemTrace();

    // 设备信息
    std::string GetOSVersionCommon() const;
    SIZE_T GetTotalPhysicalMemory() const;
    std::string GetCPUModel() const;
    std::string GetCPUModelCommon() const;
    std::string GetGPUDescription() const;
    std::string GetPrintPCDeviceInfo() const;
    std::string GetLocalIp() const;

    // 进程内存
    void UpdateProcessMemoryCounters();
    SIZE_T GetProcessMemoryUsage();
    SIZE_T GetProcessWorkingSet();
    SIZE_T GetProcessMemory(DWORD pid);
    SIZE_T GetProcessWorkingSet(DWORD pid);

    // CPU使用率（非阻塞，基于历史数据计算）
    double GetCPUUsageCommon();

    // 按进程CPU使用率
    double GetProcessCPUUsage(DWORD pid);

    // 按进程磁盘IO
    uint64_t GetProcessDiskReadBytes(DWORD pid);
    uint64_t GetProcessDiskWriteBytes(DWORD pid);

    // 网络速率（基于 /proc/net/dev 采样差值）
    double GetNetworkRecvRate();
    double GetNetworkSentRate();

#ifdef _WIN32
    bool Initialize();
    void Uninitialize();
    bool AddCounter(const _tstring& strCounterPath);
    bool RemoveCounter(const _tstring& strCounterPath);
    void SetCollectInterval(DWORD millisecond = 1000);
    bool StartCollect();
    bool GetFormattedCounterValue(const _tstring& strCounterPath, DWORD dwFormat, PPDH_FMT_COUNTERVALUE pValue);
    bool GetFormattedCounterArray(const _tstring& strCounterPath, DWORD dwFormat, PPDH_FMT_COUNTERVALUE pValue);
#else
    bool Initialize();
    void Uninitialize();
#endif

private:
#ifdef _WIN32
    PROCESS_MEMORY_COUNTERS m_processMemoryCounters;
    HQUERY m_hQuery;
    PerfMonInfo m_hPerfMonInfos;
    std::mutex m_Mutex;
    std::thread m_task;
    std::atomic<bool> m_fQuit;
    DWORD m_msCollectInterval;

    // 条件变量替代忙等
    std::mutex m_sleepMutex;
    std::condition_variable m_sleepCV;

    // 非阻塞CPU使用率所需的上一次采样数据
    FILETIME m_prevIdleTime;
    FILETIME m_prevKernelTime;
    FILETIME m_prevUserTime;
    bool m_hasCpuHistory;

    // 按进程CPU使用率所需的上一次采样数据
    std::map<DWORD, FILETIME> m_prevProcKernelTime;
    std::map<DWORD, FILETIME> m_prevProcUserTime;
    std::map<DWORD, FILETIME> m_prevProcTimestamp;
    std::map<DWORD, bool> m_hasProcCpuHistory;

    // GetFormattedCounterArray 缓冲区，避免高频堆分配
    std::vector<BYTE> m_counterArrayBuffer;

    // 缓存OS版本，避免重复 LoadLibrary
    mutable std::string m_cachedOSVersion;
    mutable bool m_osVersionCached;
#else
    // Linux/Android 平台成员
    struct ProcMemInfo {
        SIZE_T pagefileUsage;
        SIZE_T workingSetSize;
    };
    ProcMemInfo m_processMemoryCounters;

    // 按进程CPU使用率所需的上一次采样数据
    std::map<DWORD, uint64_t> m_prevProcCpuTime;
    std::map<DWORD, uint64_t> m_prevProcTimestamp;
    std::map<DWORD, bool> m_hasProcCpuHistory;

    // 系统CPU使用率所需的上一次采样数据
    uint64_t m_prevIdleTime;
    uint64_t m_prevTotalTime;
    bool m_hasCpuHistory;

    // 网络速率所需的上一次采样数据
    uint64_t m_prevNetRecvBytes;
    uint64_t m_prevNetSendBytes;
    uint64_t m_prevNetTimestamp;
    bool m_hasNetHistory;

    mutable std::string m_cachedOSVersion;
    mutable bool m_osVersionCached;
#endif
};

// C风格便捷接口
extern "C" {
    MemTrace_API bool InitMemTrace();
    MemTrace_API void UpdateMemory();
    MemTrace_API SIZE_T GetCurrentProcessMemory();
    MemTrace_API SIZE_T GetCurrentProcessWorkingSet();
    MemTrace_API SIZE_T GetProcessMemoryByPid(DWORD pid);
    MemTrace_API SIZE_T GetProcessWorkingSetByPid(DWORD pid);
    MemTrace_API double GetCurrentCPUUsage();
    MemTrace_API double GetProcessCPUUsageByPid(DWORD pid);
    MemTrace_API uint64_t GetProcessDiskReadBytesByPid(DWORD pid);
    MemTrace_API uint64_t GetProcessDiskWriteBytesByPid(DWORD pid);
    MemTrace_API double GetCurrentGPUUsage();
    MemTrace_API double GetCurrentDiskReadRate();
    MemTrace_API double GetCurrentDiskWriteRate();
    MemTrace_API double GetCurrentNetworkRecvRate();
    MemTrace_API double GetCurrentNetworkSentRate();
    MemTrace_API double GetCurrentAvailableMemory();
    MemTrace_API void UninitMemTrace();
}
