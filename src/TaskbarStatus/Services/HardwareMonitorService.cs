using LibreHardwareMonitor.Hardware;

namespace TaskbarStatus.Services;

/// <summary>One snapshot of sensor values. null = sensor not available.</summary>
public sealed record HardwareReadings(float? CpuTempC);

/// <summary>
/// Reads sensors on a background loop and stores the newest snapshot in <see cref="Latest"/>.
/// The UI never calls into the sensors; it only reads <see cref="Latest"/> ("pull" model).
/// </summary>
public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer = new() { IsCpuEnabled = true };
    private readonly CancellationTokenSource _cts = new();
    private volatile HardwareReadings _latest = new(CpuTempC: null);

    public HardwareReadings Latest => _latest;

    public void Start(TimeSpan interval)
    {
        _ = Task.Run(() => LoopAsync(interval, _cts.Token));
    }

    private async Task LoopAsync(TimeSpan interval, CancellationToken token)
    {
        _computer.Open();
        using var timer = new PeriodicTimer(interval);
        do
        {
            _latest = new HardwareReadings(CpuTempC: ReadCpuTemp());
        }
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
    }

    private float? ReadCpuTemp()
    {
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Cpu) continue;
            hw.Update(); // slow part: talks to the hardware driver

            var temps = hw.Sensors
                .Where(s => s.SensorType == SensorType.Temperature && s.Value is not null)
                .ToList();
            if (temps.Count == 0) return null;

            // Intel: "CPU Package"; AMD: "Core (Tctl/Tdie)". Otherwise take the hottest sensor.
            var preferred = temps.FirstOrDefault(s =>
                s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase));
            return preferred?.Value ?? temps.Max(s => s.Value);
        }
        return null;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _computer.Close();
    }
}
