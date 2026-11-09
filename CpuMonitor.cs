namespace MicroMonitor;

public static class CpuMonitor
{
    public static float? GetLoad()
    {
        return HardwareMonitor.Read().CpuLoad;
    }

    public static float? GetTemperature()
    {
        return HardwareMonitor.Read().CpuTemp;
    }
}