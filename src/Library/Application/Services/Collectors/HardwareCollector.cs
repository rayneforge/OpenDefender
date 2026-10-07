using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects hardware metrics via smartctl/sensors (Linux) or CIM/WMI (Windows).
/// Output format: Device|Attribute|Value|Status
/// </summary>
public class HardwareCollector : ShellCollector<HardwareMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        // Normalize to pipe-delimited: Device|Attribute|Value|Status
        return @"
            SMART=$(smartctl -A /dev/nvme0n1 2>/dev/null);
            TEMP=$(echo ""$SMART"" | grep -i 'Temperature:' | head -1 | grep -oP '[0-9]+' | head -1);
            PCT=$(echo ""$SMART"" | grep -i 'Percentage Used' | grep -oP '[0-9]+' | head -1);
            SPARE=$(echo ""$SMART"" | grep -i 'Available Spare:' | grep -oP '[0-9]+' | head -1);
            [ -n ""$TEMP"" ] && echo ""/dev/nvme0n1|Temperature_Celsius|$TEMP|OK"";
            [ -n ""$PCT"" ] && echo ""/dev/nvme0n1|Percentage_Used|$PCT|OK"";
            [ -n ""$SPARE"" ] && echo ""/dev/nvme0n1|Available_Spare|$SPARE|OK"";
            SENS=$(sensors 2>/dev/null);
            PKG=$(echo ""$SENS"" | grep 'Package id' | head -1 | grep -oP '\+[0-9.]+' | tr -d '+');
            TCTL=$(echo ""$SENS"" | grep 'Tctl' | head -1 | grep -oP '\+[0-9.]+' | tr -d '+');
            [ -n ""$PKG"" ] && echo ""CPU|CPU_Package_Temp|$PKG|OK"";
            [ -n ""$TCTL"" ] && echo ""CPU|CPU_Tctl|$TCTL|OK""
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
try {
    $disk = Get-PhysicalDisk | Select-Object -First 1
    $counter = Get-StorageReliabilityCounter -PhysicalDisk $disk -ErrorAction Stop
    Write-Output ""$($disk.FriendlyName)|Temperature_Celsius|$($counter.Temperature)|OK""
    Write-Output ""$($disk.FriendlyName)|Percentage_Used|$($counter.Wear)|OK""
} catch {
    Write-Output ""Disk|Temperature_Celsius|0|Unavailable""
}
try {
    $zone = Get-CimInstance MSAcpi_ThermalZoneTemperature -Namespace root/wmi -ErrorAction Stop | Select-Object -First 1
    $tempC = [math]::Round(($zone.CurrentTemperature - 2732) / 10, 1)
    Write-Output ""CPU|CPU_Package_Temp|$tempC|OK""
} catch {
    Write-Output ""CPU|CPU_Package_Temp|0|Unavailable""
}";
    }

    protected override IEnumerable<HardwareMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<HardwareMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;

            double.TryParse(parts[2].Trim(), out double val);

            results.Add(new HardwareMetric
            {
                Timestamp = collectedAt,
                Device = parts[0].Trim(),
                Attribute = parts[1].Trim(),
                Value = val,
                Status = parts[3].Trim()
            });
        }
        return results;
    }
}
