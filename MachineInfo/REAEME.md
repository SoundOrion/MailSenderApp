もちろんです。
今の `Program.cs` の処理を、**クラス化 + サービス化** した形に分けます。元コードは、ドライブ、CPU、メモリ、フォルダサイズ、ファイル情報を 1 つの `Program` にまとめて実装している状態でした。

おすすめ構成はこれです。

* `Models`
  取得結果を入れるクラス
* `Services`
  実際に情報を取得するサービス
* `Interfaces`
  サービスのインターフェース
* `Program.cs`
  呼び出しだけ

---

# 構成例

```text
MachineInfo
├─ Interfaces
│  └─ IMachineInfoService.cs
├─ Models
│  ├─ DriveUsageInfo.cs
│  ├─ MemoryUsageInfo.cs
│  ├─ FileDetailInfo.cs
│  └─ MachineInfoResult.cs
├─ Services
│  └─ MachineInfoService.cs
└─ Program.cs
```

---

# 1. Models

## `Models/DriveUsageInfo.cs`

```csharp
namespace MachineInfo.Models;

public class DriveUsageInfo
{
    public string DriveName { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long TotalSize { get; set; }
    public long UsedSize { get; set; }
    public long FreeSize { get; set; }
    public double UsageRate { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

## `Models/MemoryUsageInfo.cs`

```csharp
namespace MachineInfo.Models;

public class MemoryUsageInfo
{
    public long TotalMemory { get; set; }
    public long UsedMemory { get; set; }
    public long FreeMemory { get; set; }
    public double UsageRate { get; set; }
}
```

## `Models/FileDetailInfo.cs`

```csharp
using System;

namespace MachineInfo.Models;

public class FileDetailInfo
{
    public string FilePath { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long FileSize { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public string FileVersion { get; set; } = string.Empty;
    public string ProductVersion { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

## `Models/MachineInfoResult.cs`

```csharp
using System.Collections.Generic;

namespace MachineInfo.Models;

public class MachineInfoResult
{
    public List<DriveUsageInfo> Drives { get; set; } = new();
    public float CpuUsage { get; set; }
    public MemoryUsageInfo? Memory { get; set; }
    public long FolderSize { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public FileDetailInfo? FileInfo { get; set; }
}
```

---

# 2. Interface

## `Interfaces/IMachineInfoService.cs`

```csharp
using MachineInfo.Models;

namespace MachineInfo.Interfaces;

public interface IMachineInfoService
{
    DriveUsageInfo GetDriveInfo(string driveLetter);
    float GetCpuUsage();
    MemoryUsageInfo? GetMemoryInfo();
    long GetDirectorySize(string folderPath);
    FileDetailInfo GetFileInfo(string filePath);
    MachineInfoResult GetMachineInfo(string folderPath, string filePath, params string[] driveLetters);
    string FormatBytes(long bytes);
}
```

---

# 3. Service 本体

## `Services/MachineInfoService.cs`

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using MachineInfo.Interfaces;
using MachineInfo.Models;

namespace MachineInfo.Services;

public class MachineInfoService : IMachineInfoService
{
    public DriveUsageInfo GetDriveInfo(string driveLetter)
    {
        string driveName = driveLetter + @":\";

        DriveInfo? drive = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));

        if (drive == null || !drive.IsReady)
        {
            return new DriveUsageInfo
            {
                DriveName = driveName,
                Exists = false,
                Message = $"{driveName} は存在しないか、使用可能ではありません。"
            };
        }

        long total = drive.TotalSize;
        long free = drive.TotalFreeSpace;
        long used = total - free;
        double usageRate = total > 0 ? (double)used / total * 100 : 0;

        return new DriveUsageInfo
        {
            DriveName = driveName,
            Exists = true,
            TotalSize = total,
            UsedSize = used,
            FreeSize = free,
            UsageRate = usageRate
        };
    }

    public float GetCpuUsage()
    {
        using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        cpuCounter.NextValue();
        Thread.Sleep(1000);
        return cpuCounter.NextValue();
    }

    public MemoryUsageInfo? GetMemoryInfo()
    {
        if (!TryGetMemoryStatus(out var memStatus))
        {
            return null;
        }

        ulong total = memStatus.ullTotalPhys;
        ulong free = memStatus.ullAvailPhys;
        ulong used = total - free;
        double usageRate = total > 0 ? (double)used / total * 100 : 0;

        return new MemoryUsageInfo
        {
            TotalMemory = (long)total,
            UsedMemory = (long)used,
            FreeMemory = (long)free,
            UsageRate = usageRate
        };
    }

    public long GetDirectorySize(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return 0;
        }

        try
        {
            return Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                .Sum(file =>
                {
                    try
                    {
                        return new FileInfo(file).Length;
                    }
                    catch
                    {
                        return 0L;
                    }
                });
        }
        catch
        {
            return 0L;
        }
    }

    public FileDetailInfo GetFileInfo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new FileDetailInfo
            {
                FilePath = filePath,
                Exists = false,
                Message = $"ファイルが存在しません: {filePath}"
            };
        }

        FileInfo fi = new FileInfo(filePath);
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);

        return new FileDetailInfo
        {
            FilePath = fi.FullName,
            Exists = true,
            FileSize = fi.Length,
            CreationTime = fi.CreationTime,
            LastWriteTime = fi.LastWriteTime,
            LastAccessTime = fi.LastAccessTime,
            FileVersion = fvi.FileVersion ?? string.Empty,
            ProductVersion = fvi.ProductVersion ?? string.Empty,
            ProductName = fvi.ProductName ?? string.Empty,
            CompanyName = fvi.CompanyName ?? string.Empty
        };
    }

    public MachineInfoResult GetMachineInfo(string folderPath, string filePath, params string[] driveLetters)
    {
        var result = new MachineInfoResult
        {
            CpuUsage = GetCpuUsage(),
            Memory = GetMemoryInfo(),
            FolderPath = folderPath,
            FolderSize = GetDirectorySize(folderPath),
            FileInfo = GetFileInfo(filePath)
        };

        foreach (var driveLetter in driveLetters)
        {
            result.Drives.Add(GetDriveInfo(driveLetter));
        }

        return result;
    }

    public string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:F2} {units[unit]}";
    }

    private static bool TryGetMemoryStatus(out MEMORYSTATUSEX memStatus)
    {
        memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        return GlobalMemoryStatusEx(memStatus);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
```

---

# 4. `Program.cs`

```csharp
using System;
using MachineInfo.Interfaces;
using MachineInfo.Services;

namespace MachineInfo;

class Program
{
    static void Main()
    {
        IMachineInfoService machineInfoService = new MachineInfoService();

        var result = machineInfoService.GetMachineInfo(
            folderPath: @"C:\Temp",
            filePath: @"C:\Windows\System32\notepad.exe",
            driveLetters: new[] { "C", "D" });

        foreach (var drive in result.Drives)
        {
            Console.WriteLine($"--- {drive.DriveName} ---");
            if (!drive.Exists)
            {
                Console.WriteLine(drive.Message);
                continue;
            }

            Console.WriteLine($"総容量   : {machineInfoService.FormatBytes(drive.TotalSize)}");
            Console.WriteLine($"使用容量 : {machineInfoService.FormatBytes(drive.UsedSize)}");
            Console.WriteLine($"空き容量 : {machineInfoService.FormatBytes(drive.FreeSize)}");
            Console.WriteLine($"使用率   : {drive.UsageRate:F1}%");
        }

        Console.WriteLine($"CPU利用率: {result.CpuUsage:F1}%");

        Console.WriteLine("--- メモリ ---");
        if (result.Memory != null)
        {
            Console.WriteLine($"総メモリ   : {machineInfoService.FormatBytes(result.Memory.TotalMemory)}");
            Console.WriteLine($"使用メモリ : {machineInfoService.FormatBytes(result.Memory.UsedMemory)}");
            Console.WriteLine($"空きメモリ : {machineInfoService.FormatBytes(result.Memory.FreeMemory)}");
            Console.WriteLine($"使用率     : {result.Memory.UsageRate:F1}%");
        }
        else
        {
            Console.WriteLine("メモリ情報を取得できませんでした。");
        }

        if (result.FolderSize > 0)
        {
            Console.WriteLine($"フォルダサイズ [{result.FolderPath}]: {machineInfoService.FormatBytes(result.FolderSize)}");
        }
        else
        {
            Console.WriteLine($"フォルダが存在しないか、サイズを取得できません: {result.FolderPath}");
        }

        Console.WriteLine("--- ファイル情報 ---");
        if (result.FileInfo != null && result.FileInfo.Exists)
        {
            Console.WriteLine($"パス             : {result.FileInfo.FilePath}");
            Console.WriteLine($"サイズ           : {machineInfoService.FormatBytes(result.FileInfo.FileSize)}");
            Console.WriteLine($"作成日時         : {result.FileInfo.CreationTime}");
            Console.WriteLine($"更新日時         : {result.FileInfo.LastWriteTime}");
            Console.WriteLine($"アクセス日時     : {result.FileInfo.LastAccessTime}");
            Console.WriteLine($"ファイルVersion  : {result.FileInfo.FileVersion}");
            Console.WriteLine($"製品Version      : {result.FileInfo.ProductVersion}");
            Console.WriteLine($"製品名           : {result.FileInfo.ProductName}");
            Console.WriteLine($"会社名           : {result.FileInfo.CompanyName}");
        }
        else
        {
            Console.WriteLine(result.FileInfo?.Message ?? "ファイル情報を取得できませんでした。");
        }
    }
}
```

---

# 5. これで何が良くなるか

この形にすると、

* `Program.cs` が薄くなる
* 後で API に載せやすい
* 単体テストしやすい
* DI 登録しやすい
* `Console` でも `Web API` でも同じサービスを使い回せる

特に次に API 化するなら、この形がかなりやりやすいです。

---

# 6. ASP.NET Core でサービス登録するなら

将来 `MailSenderApp` 側に入れるなら `Program.cs` にこれを追加です。

```csharp
builder.Services.AddScoped<IMachineInfoService, MachineInfoService>();
```

コントローラや Minimal API からはそのまま使えます。

```csharp
app.MapGet("/machine-info", (IMachineInfoService service) =>
{
    var result = service.GetMachineInfo(
        @"C:\Temp",
        @"C:\Windows\System32\notepad.exe",
        "C", "D");

    return Results.Ok(result);
});
```

---

# 7. さらにおすすめの分け方

実務なら、もう一段分けてもいいです。

* `IDriveInfoService`
* `ICpuInfoService`
* `IMemoryInfoService`
* `IFileInfoService`
* `IFolderInfoService`

ただ、最初はそこまで細かくしなくて大丈夫です。
まずは **`IMachineInfoService` 1本** が一番扱いやすいです。

---

必要なら次に、あなたの今のプロジェクト構成に合わせて、

**「Visual Studio のフォルダ作成手順つきで、そのまま貼れる完全版」**

にしてまとめます。
