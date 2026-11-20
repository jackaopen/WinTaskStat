using TaskbarStatus.Platform;
using TaskbarStatus.UI;

namespace TaskbarStatus;

public partial class App : Microsoft.UI.Xaml.Application
{
    private TaskbarWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new TaskbarWindow();
        await TaskbarHost.AttachAsync(_window, _window.TaskbarContent);
        _window.Activate();
    }
}
