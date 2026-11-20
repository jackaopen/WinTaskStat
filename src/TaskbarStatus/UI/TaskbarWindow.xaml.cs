namespace TaskbarStatus.UI;

public sealed partial class TaskbarWindow : Microsoft.UI.Xaml.Window
{
    internal Microsoft.UI.Xaml.FrameworkElement TaskbarContent => RootGrid;

    public TaskbarWindow()
    {
        InitializeComponent();
    }

    private void buttonsayhiandexit(object sender, Microsoft.UI.Xaml.RoutedEventArgs args)
    {
        MessageBox.Show("hi and bye", "WTS");
        Close();
    }
}
