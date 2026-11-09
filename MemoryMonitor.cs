using System;
using System.Runtime.InteropServices;

namespace MicroMonitor;

public static class MemoryMonitor
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailablePhys;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

    public static void GetMemory(out float usedGb, out float totalGb)
    {
        var status = new MEMORYSTATUSEX
        {
            Length = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            usedGb = 0;
            totalGb = 0;
            return;
        }

        const float gb = 1024f * 1024f * 1024f;
        totalGb = status.TotalPhys / gb;
        usedGb = (status.TotalPhys - status.AvailablePhys) / gb;
    }

    public static int GetUsedPercent()
    {
        var status = new MEMORYSTATUSEX
        {
            Length = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        return GlobalMemoryStatusEx(ref status) ? (int)status.MemoryLoad : 0;
    }
}