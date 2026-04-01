using System.Windows;
using System.Windows.Input;
using SpotifyDock.ViewModels;

namespace SpotifyDock;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonDown(
        object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void AlbumArt_Click(
        object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PlayerViewModel vm && !vm.IsConnected)
            vm.ConnectCommand.Execute(null);
        e.Handled = true;
    }

    private void CloseButton_Click(
        object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ProgressBar_Click(
        object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PlayerViewModel vm && vm.IsConnected)
        {
            var clickX = e.GetPosition(ProgressBarGrid).X;
            var totalWidth = ProgressBarGrid.ActualWidth;
            if (totalWidth > 0)
            {
                var percent = Math.Clamp(clickX / totalWidth, 0, 1);
                vm.SeekCommand.Execute(percent);
            }
        }
        e.Handled = true;
    }
}