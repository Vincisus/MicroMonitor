namespace MicroMonitor;

public static class GpuMonitor
{
    public static float? GetLoad()
    {
        return HardwareMonitor.Read().GpuLoad;
    }

    public static float? GetTemperature()
    {
        return HardwareMonitor.Read().GpuTemperature;
    }
}