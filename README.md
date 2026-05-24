# MemTrace

A .NET 9 console application that monitors system and process performance metrics via a native MemTrace.dll.

## Features

- **CPU** — system-wide and per-process CPU usage
- **GPU** — system-wide GPU usage
- **Memory** — process commit memory (PagefileUsage), working set, and system available memory
- **Disk I/O** — system-wide read/write rates and per-process cumulative bytes
- **Network** — system-wide receive/send rates
- **Continuous sampling** — real-time metrics display at 1-second intervals

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows (native MemTrace.dll uses Win32 APIs)

## Run

```bash
dotnet run
```

## Docker

```bash
docker compose up --build
```

## Project Structure

| Path | Description |
|------|-------------|
| `Program.cs` | Entry point — init, sample, and continuous monitoring loop |
| `src/MemTrace.cs` | Managed wrapper (P/Invoke) around native MemTrace.dll |
| `src/MemTrace.h` | Native DLL C header |
| `resource/` | Native library files |
