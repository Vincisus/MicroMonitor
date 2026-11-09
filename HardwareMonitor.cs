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
            if (_computer != null) return;

            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = true,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false,
                IsPsuEnabled = false
            };

            _computer.Open();
        }
    }

    public static (float? CpuLoad, float? CpuTemp, float? GpuLoad, float? GpuTemp) Read()
    {
        Initialize();

        lock (_lock)
        {
            if (_computer == null) return (null, null, null, null);

            IHardware? cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            IHardware? gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType.ToString().StartsWith("Gpu", StringComparison.OrdinalIgnoreCase));
            IHardware? mobo = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Motherboard);

            cpu?.Update();
            gpu?.Update();
            mobo?.Update();

            if (mobo != null)
            {
                foreach (var sub in mobo.SubHardware)
                {
                    sub.Update();
                }
            }

            float? cpuLoad = GetSensor(cpu, SensorType.Load, "CPU Total", "Total");
            float? cpuTemp = FindCpuTemperature(cpu, mobo);

            float? gpuLoad = GetSensor(gpu, SensorType.Load, "GPU Core", "Core", "D3D 3D");
            float? gpuTemp = GetSensor(gpu, SensorType.Temperature, "GPU Core", "Core", "Hot Spot");
            if (gpuTemp <= 0f) gpuTemp = null;

            return (cpuLoad, cpuTemp, gpuLoad, gpuTemp);
        }
    }

    private static float? FindCpuTemperature(IHardware? cpu, IHardware? mobo)
    {
        string[] priorityNames =
        [
            "Core (Tctl/Tdie)",
            "CPU (Tctl/Tdie)",
            "Tctl",
            "Tdie",
            "CPU Package",
            "Package",
            "Core Max",
            "CPU Core",
            "Core Average",
            "CPU Die",
            "CPU"
        ];

        if (cpu != null)
        {
            var cpuSensors = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value > 0).ToArray();
            foreach (var name in priorityNames)
            {
                var match = cpuSensors.FirstOrDefault(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (match?.Value is float val && val > 0) return val;
            }

            var anyCpuTemp = cpuSensors.FirstOrDefault();
            if (anyCpuTemp?.Value is float anyVal && anyVal > 0) return anyVal;
        }

        if (mobo != null)
        {
            foreach (var sub in mobo.SubHardware)
            {
                var moboSensors = sub.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value > 0).ToArray();
                foreach (var name in priorityNames)
                {
                    var match = moboSensors.FirstOrDefault(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                    if (match?.Value is float val && val > 0) return val;
                }
            }
        }

        return null;
    }

    private static float? GetSensor(IHardware? hardware, SensorType type, params string[] preferredNames)
    {
        if (hardware == null) return null;

        var sensors = hardware.Sensors.Where(s => s.SensorType == type && s.Value.HasValue).ToArray();

        foreach (string preferred in preferredNames)
        {
            var match = sensors.FirstOrDefault(s => s.Name.Contains(preferred, StringComparison.OrdinalIgnoreCase));
            if (match?.Value is float val && val > 0) return val;
        }

        var fallback = sensors.FirstOrDefault(s => s.Value > 0);
        return fallback?.Value;
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