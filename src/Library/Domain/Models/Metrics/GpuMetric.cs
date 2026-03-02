using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: gpu_accelerators.csv — Timestamp,Vendor,Device,GPU_Util,Mem_Util,Temp</summary>
public class GpuMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Vendor { get; set; }
    public string Device { get; set; }
    public double GpuUtil { get; set; } // Changed to double
    public double MemUtil { get; set; } // Changed to double
    public double Temp { get; set; }    // Changed to double
}
