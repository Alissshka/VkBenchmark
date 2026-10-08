using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

class VkBenchmarkTask
{
    private const string BenchmarkFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool";
    private const string ConfigRelativePath = @"b1\Saved\Config\Windows\GameUserSettings.ini";
    private const string ExeRelativePath = @"b1_benchmark.exe";

    
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_RETURN = 0x0D;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Инструмент автоматизации Black Myth: Wukong Benchmark ===");
        Console.WriteLine();

        string fullExePath = Path.Combine(BenchmarkFolder, ExeRelativePath);
        string fullConfigPath = Path.Combine(BenchmarkFolder, ConfigRelativePath);

        if (!File.Exists(fullExePath)) { Console.WriteLine($"❌ Не найден бенчмарк: {fullExePath}"); return; }
        if (!File.Exists(fullConfigPath)) { Console.WriteLine($"❌ Не найден конфиг: {fullConfigPath}"); return; }

        Console.WriteLine("--- Характеристики ПК ---");
        Console.WriteLine($"  CPU: {GetCpuInfo()}");
        Console.WriteLine($"  RAM: {GetRamInfo()}");
        Console.WriteLine($"  GPU: {GetGpuInfo()}");
        Console.WriteLine();

      
        Console.WriteLine("[1/2] Настройка и запуск CPU-теста...");
        ApplySettings(fullConfigPath, GetCpuTestSettings());
        RunBenchmark(fullExePath);
        string cpuResult = ReadResult(fullConfigPath, "LastCPUBenchmarkResult");
        Console.WriteLine($"  ✅ Результат CPU-теста: {cpuResult}");
        Console.WriteLine();

        Thread.Sleep(3000);

      
        Console.WriteLine("[2/2] Настройка и запуск GPU-теста...");
        ApplySettings(fullConfigPath, GetGpuTestSettings());
        RunBenchmark(fullExePath);
        string gpuResult = ReadResult(fullConfigPath, "LastGPUBenchmarkResult");
        Console.WriteLine($"  ✅ Результат GPU-теста: {gpuResult}");
        Console.WriteLine();

       
        Console.WriteLine("================================================");
        Console.WriteLine("ИТОГОВЫЙ ОТЧЁТ");
        Console.WriteLine("================================================");
        Console.WriteLine($"CPU: {GetCpuInfo()}");
        Console.WriteLine($"GPU: {GetGpuInfo()}");
        Console.WriteLine($"RAM: {GetRamInfo()}");
        Console.WriteLine();
        Console.WriteLine("--- CPU-тест (нагрузка на процессор) ---");
        Console.WriteLine($"  Настройки: разрешение 25%, все эффекты на минимум, RT выключен");
        Console.WriteLine($"  Результат: {cpuResult}");
        Console.WriteLine();
        Console.WriteLine("--- GPU-тест (максимальная нагрузка на видеокарту) ---");
        Console.WriteLine($"  Настройки: разрешение 100%, все эффекты на максимум, RT включен");
        Console.WriteLine($"  Результат: {gpuResult}");
        Console.WriteLine();
        Console.WriteLine("Нажми любую клавишу для выхода...");
        Console.ReadKey();
    }

    static Dictionary<string, string> GetCpuTestSettings() => new()
    {
        { "sg.ResolutionQuality", "25.000000" },
        { "sg.ViewDistanceQuality", "3" },
        { "sg.AntiAliasingQuality", "0" },
        { "sg.ShadowQuality", "0" },
        { "sg.GlobalIlluminationQuality", "0" },
        { "sg.RayTracingQuality", "0" },
        { "sg.ReflectionQuality", "0" },
        { "sg.PostProcessQuality", "0" },
        { "sg.TextureQuality", "0" },
        { "sg.EffectsQuality", "0" },
        { "sg.FoliageQuality", "3" },
        { "sg.ShadingQuality", "0" }
    };

    static Dictionary<string, string> GetGpuTestSettings() => new()
    {
        { "sg.ResolutionQuality", "100.000000" },
        { "sg.ViewDistanceQuality", "3" },
        { "sg.AntiAliasingQuality", "3" },
        { "sg.ShadowQuality", "3" },
        { "sg.GlobalIlluminationQuality", "3" },
        { "sg.RayTracingQuality", "3" },
        { "sg.ReflectionQuality", "3" },
        { "sg.PostProcessQuality", "3" },
        { "sg.TextureQuality", "3" },
        { "sg.EffectsQuality", "3" },
        { "sg.FoliageQuality", "3" },
        { "sg.ShadingQuality", "3" }
    };

    static void ApplySettings(string configPath, Dictionary<string, string> settings)
    {
        var lines = File.ReadAllLines(configPath).ToList();
        var remaining = new Dictionary<string, string>(settings);
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("sg.") && line.Contains('='))
            {
                string key = line.Split('=')[0].Trim();
                if (remaining.ContainsKey(key))
                {
                    lines[i] = $"{key}={remaining[key]}";
                    remaining.Remove(key);
                }
            }
        }
        if (remaining.Count > 0)
        {
            int idx = lines.FindIndex(l => l.Trim().Equals("[ScalabilityGroups]", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                lines.InsertRange(idx + 1, remaining.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        File.WriteAllLines(configPath, lines);
        Console.WriteLine("  📝 Настройки записаны в конфиг.");
    }

    static void RunBenchmark(string exePath)
    {
        Console.WriteLine("  🚀 Запуск бенчмарка...");

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath),
            UseShellExecute = true
        };

        using var process = Process.Start(psi);
        if (process == null) { Console.WriteLine("  ❌ Не удалось запустить."); return; }

       
        Console.WriteLine("  ⏳ Ожидание загрузки меню (20 секунд)...");
        Thread.Sleep(20000);

        
        Console.WriteLine("  ⌨️ Отправка нажатия Enter для запуска теста...");
        process.Refresh();
        IntPtr handle = process.MainWindowHandle;
        if (handle != IntPtr.Zero)
        {
            SetForegroundWindow(handle);
            Thread.Sleep(500);
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        
        Console.WriteLine("  ⏳ Ожидание завершения теста (3 минуты)...");
        Thread.Sleep(180000);

        
        try
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                Thread.Sleep(2000);
                if (!process.HasExited) process.Kill();
            }
        }
        catch { }

        Console.WriteLine("  ⏹ Бенчмарк завершён.");
        Thread.Sleep(3000);
    }

    static string ReadResult(string configPath, string keyName)
    {
        if (!File.Exists(configPath)) return "не найдено";
        foreach (var line in File.ReadAllLines(configPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(keyName + "="))
                return trimmed.Substring(keyName.Length + 1);
        }
        return "не найдено";
    }

    static string GetCpuInfo() => Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Неизвестно";

    static string GetRamInfo()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
                return $"{(ulong)obj["TotalPhysicalMemory"] / 1024 / 1024 / 1024} GB";
        }
        catch { }
        return "Неизвестно";
    }

    static string GetGpuInfo()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (var obj in searcher.Get())
                return obj["Name"]?.ToString() ?? "Неизвестно";
        }
        catch { }
        return "Неизвестно";
    }
}
