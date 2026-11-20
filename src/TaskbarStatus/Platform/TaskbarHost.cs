using Deskband11Lib.Core;
using Deskband11Lib.WinUI;
using Microsoft.UI.Xaml;

namespace TaskbarStatus.Platform;

internal static class TaskbarHost
{
    public static async Task AttachAsync(Window window, FrameworkElement root)
    {
        var host = new TaskbarContentHost(window, root, new TaskbarContentHostOptions
        {
            PreferredWidth = 280,
            PreferredHeight = 60
        });

        window.Closed += (_, _) => host.Dispose();
        await host.AttachWhenLayoutReadyAsync();
    }
}
