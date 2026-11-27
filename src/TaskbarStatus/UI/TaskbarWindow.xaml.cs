using Microsoft.UI.Dispatching;
using TaskbarStatus.Services;

namespace TaskbarStatus.UI;

public sealed partial class TaskbarWindow : Microsoft.UI.Xaml.Window
{
    private readonly HardwareMonitorService _hms;
    private readonly DispatcherQueueTimer _uiTimer;

    internal Microsoft.UI.Xaml.FrameworkElement TaskbarContent => RootGrid;

    public TaskbarWindow(HardwareMonitorService hms)
    {
        InitializeComponent();
        _hms = hms;

        // UI loop: every 1 s, copy hms.Latest onto the screen (cheap, never touches sensors).
        _uiTimer = DispatcherQueue.CreateTimer();
        _uiTimer.Interval = TimeSpan.FromSeconds(1);
        _uiTimer.Tick += (_, _) => ShowReadings(_hms.Latest);
        _uiTimer.Start();

        Closed += (_, _) => _uiTimer.Stop();
    }

    private void ShowReadings(HardwareReadings r)
    {
        CpuTempText.Text = r.CpuTempC is float t ? $"CPU {t:0}°C" : "CPU —";
    }

    private void buttonsayhiandexit(object sender, Microsoft.UI.Xaml.RoutedEventArgs args)
    {
        MessageBox.Show("hi and bye", "WTS");
        Close();
    }
}
