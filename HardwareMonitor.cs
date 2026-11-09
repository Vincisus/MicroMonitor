using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace MicroMonitor;

public static class HardwareMonitor
{
    private static Computer? _computer;
    private static readonly object _lock = new();

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_computer != null)
            {
                return;
            }

            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsStorageEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsPsuEnabled = false
            };

            _computer.Open();
        }
    }

    public static (float? CpuLoad, float? CpuTemperature, float? GpuLoad, float? GpuTemperature) Read()
    {
        Initialize();

        lock (_lock)
        {
            if (_computer == null)
            {
                return (null, null, null, null);
            }

            IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase));

            cpu?.Update();
            gpu?.Update();

            float? cpuLoad = GetSensor(cpu, SensorType.Load, "CPU Total", "Total");
            float? cpuTemp = GetSensor(cpu, SensorType.Temperature, "Package", "CPU Package", "Core (Max)");

            float? gpuLoad = GetSensor(gpu, SensorType.Load, "GPU Core", "Core");
            float? gpuTemp = GetSensor(gpu, SensorType.Temperature, "GPU Core", "Core", "Hot Spot");

            return (cpuLoad, cpuTemp, gpuLoad, gpuTemp);
        }
    }

    private static float? GetSensor(IHardware? hardware, SensorType type, params string[] preferredNames)
    {
        if (hardware == null)
        {
            return null;
        }

        var sensors = hardware.Sensors.Where(s => s.SensorType == type && s.Value.HasValue).ToArray();

        foreach (string preferred in preferredNames)
        {
            var match = sensors.FirstOrDefault(s =>
                s.Name.Contains(preferred, StringComparison.OrdinalIgnoreCase));

            if (match?.Value is float value)
            {
                return value;
            }
        }

        return sensors.FirstOrDefault()?.Value;
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            _computer?.Close();
            _computer = null;
        }
    }
}