using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    static void Main()
    {
        PrintDriveInfo("C");
        PrintDriveInfo("D");

        float cpuUsage = GetCpuUsage();
        Console.WriteLine($"CPU利用率: {cpuUsage:F1}%");

        PrintMemoryInfo();

        string folderPath = @"C:\Temp";
        if (Directory.Exists(folderPath))
        {
            long folderSize = GetDirectorySize(folderPath);
            Console.WriteLine($"フォルダサイズ [{folderPath}]: {FormatBytes(folderSize)}");
        }
        else
        {
            Console.WriteLine($"フォルダが存在しません: {folderPath}");
        }

        string filePath = @"C:\Windows\System32\notepad.exe";
        if (File.Exists(filePath))
        {
            PrintFileInfo(filePath);
        }
        else
        {
            Console.WriteLine($"ファイルが存在しません: {filePath}");
        }
    }

    static void PrintDriveInfo(string driveLetter)
    {
        string driveName = driveLetter + @":\";

        DriveInfo? drive = DriveInfo.GetDrives()
            .FirstOrDefault(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));

        if (drive == null || !drive.IsReady)
        {
            Console.WriteLine($"{driveName} は存在しないか、使用可能ではありません。");
            return;
        }

        long total = drive.TotalSize;
        long free = drive.TotalFreeSpace;
        long used = total - free;
        double usageRate = total > 0 ? (double)used / total * 100 : 0;

        Console.WriteLine($"--- {driveName} ---");
        Console.WriteLine($"総容量   : {FormatBytes(total)}");
        Console.WriteLine($"使用容量 : {FormatBytes(used)}");
        Console.WriteLine($"空き容量 : {FormatBytes(free)}");
        Console.WriteLine($"使用率   : {usageRate:F1}%");
    }

    static float GetCpuUsage()
    {
        using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        cpuCounter.NextValue();
        Thread.Sleep(1000);
        return cpuCounter.NextValue();
    }

    static void PrintMemoryInfo()
    {
        if (!GetPhysicallyInstalledMemory(out var memStatus))
        {
            Console.WriteLine("メモリ情報を取得できませんでした。");
            return;
        }

        ulong total = memStatus.ullTotalPhys;
        ulong available = memStatus.ullAvailPhys;
        ulong used = total - available;
        double usageRate = total > 0 ? (double)used / total * 100 : 0;

        Console.WriteLine("--- メモリ ---");
        Console.WriteLine($"総メモリ   : {FormatBytes((long)total)}");
        Console.WriteLine($"使用メモリ : {FormatBytes((long)used)}");
        Console.WriteLine($"空きメモリ : {FormatBytes((long)available)}");
        Console.WriteLine($"使用率     : {usageRate:F1}%");
    }

    static bool GetPhysicallyInstalledMemory(out MEMORYSTATUSEX memStatus)
    {
        memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        return GlobalMemoryStatusEx(memStatus);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    class MEMORYSTATUSEX
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

    static long GetDirectorySize(string folderPath)
    {
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

    static void PrintFileInfo(string filePath)
    {
        FileInfo fi = new FileInfo(filePath);
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);

        Console.WriteLine("--- ファイル情報 ---");
        Console.WriteLine($"パス             : {fi.FullName}");
        Console.WriteLine($"サイズ           : {FormatBytes(fi.Length)}");
        Console.WriteLine($"作成日時         : {fi.CreationTime}");
        Console.WriteLine($"更新日時         : {fi.LastWriteTime}");
        Console.WriteLine($"アクセス日時     : {fi.LastAccessTime}");
        Console.WriteLine($"ファイルVersion  : {fvi.FileVersion}");
        Console.WriteLine($"製品Version      : {fvi.ProductVersion}");
        Console.WriteLine($"製品名           : {fvi.ProductName}");
        Console.WriteLine($"会社名           : {fvi.CompanyName}");
    }

    static string FormatBytes(long bytes)
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
}