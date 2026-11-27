using TaskbarStatus.Platform;
using TaskbarStatus.Services;
using TaskbarStatus.UI;

namespace TaskbarStatus;

public partial class App : Microsoft.UI.Xaml.Application
{
    private TaskbarWindow? _window;
    private readonly HardwareMonitorService _hms = new();

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _hms.Start(TimeSpan.FromSeconds(1));

        _window = new TaskbarWindow(_hms);
        _window.Closed += (_, _) => _hms.Dispose();
        await TaskbarHost.AttachAsync(_window, _window.TaskbarContent);
        _window.Activate();
    }
}
