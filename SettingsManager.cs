using System;
using System.IO;
using System.Text.Json;

namespace MicroMonitor;

public static class SettingsManager
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MicroMonitor");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static int RefreshIntervalMs { get; private set; } = 2000;
    public static bool ShowCpuTemperature { get; private set; } = true;
    public static bool ShowGpuTemperature { get; private set; } = true;

    static SettingsManager()
    {
        Load();
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Save();
                return;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(FilePath));
            var root = document.RootElement;

            if (root.TryGetProperty("RefreshIntervalMs", out var interval) &&
                interval.TryGetInt32(out int parsedInterval))
            {
                RefreshIntervalMs = Math.Clamp(parsedInterval, 500, 10000);
            }

            if (root.TryGetProperty("ShowCpuTemperature", out var cpuTemp))
            {
                ShowCpuTemperature = cpuTemp.GetBoolean();
            }

            if (root.TryGetProperty("ShowGpuTemperature", out var gpuTemp))
            {
                ShowGpuTemperature = gpuTemp.GetBoolean();
            }
        }
        catch
        {
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);

            var settings = new
            {
                RefreshIntervalMs,
                ShowCpuTemperature,
                ShowGpuTemperature
            };

            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch
        {
        }
    }
}