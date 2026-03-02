using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects GPU metrics for NVIDIA, AMD (ROCm), and Intel integrated graphics.
/// Output format: Vendor|Device|GpuUtil|MemUtil|Temp
/// </summary>
public class GpuCollector : ShellCollector<GpuMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
# NVIDIA
if command -v nvidia-smi &>/dev/null; then
    nvidia-smi --query-gpu=name,utilization.gpu,utilization.memory,temperature.gpu --format=csv,noheader,nounits 2>/dev/null | \
    while IFS=',' read -r name gpu mem temp; do
        echo ""NVIDIA|$name|$gpu|$mem|$temp""
    done
fi

# AMD (ROCm)
if command -v rocm-smi &>/dev/null; then
    rocm-smi --showproductname --showuse --showmemuse --showtemp --json 2>/dev/null | \
    jq -r 'to_entries[] | ""AMD|"" + .value.CardModel + ""|"" + (.value.GPUuse // ""0"") + ""|"" + (.value.VRAMuse // ""0"") + ""|"" + (.value.Temperature // ""0"")'
fi

# Intel Integrated
for card in /sys/class/drm/card*/device; do
    if [ -f ""$card/vendor"" ]; then
        vendor=$(cat $card/vendor)
        if [ ""$vendor"" = ""0x8086"" ]; then
            name=$(basename $(dirname $card))
            echo ""INTEL|$name|0|0|0""
        fi
    fi
done
";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
# NVIDIA
if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
    nvidia-smi --query-gpu=name,utilization.gpu,utilization.memory,temperature.gpu --format=csv,noheader,nounits 2>$null | ForEach-Object {
        $parts = $_ -split ','
        if ($parts.Count -ge 4) { ""NVIDIA|$($parts[0].Trim())|$($parts[1].Trim())|$($parts[2].Trim())|$($parts[3].Trim())"" }
    }
}

# Generic Windows GPU (AMD + Intel fallback)
Get-CimInstance Win32_VideoController | ForEach-Object {
    $name = $_.Name
    if ($name) {
        ""GENERIC|$name|0|0|0""
    }
}
";
    }

    protected override IEnumerable<GpuMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<GpuMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 5) continue;

            double.TryParse(parts[2].Trim(), out double gpu);
            double.TryParse(parts[3].Trim(), out double mem);
            double.TryParse(parts[4].Trim(), out double temp);

            results.Add(new GpuMetric
            {
                Timestamp = collectedAt,
                Vendor = parts[0].Trim(),
                Device = parts[1].Trim(),
                GpuUtil = gpu,
                MemUtil = mem,
                Temp = temp
            });
        }
        return results;
    }
}
