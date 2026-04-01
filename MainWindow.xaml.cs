using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpotifyDock.Services;

namespace SpotifyDock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MediaKeyService _mediaKey = new();
    private readonly SpotifyAuthService _authService = new();
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void Previous_Click(object sender, RoutedEventArgs e)
    {
        _mediaKey.PreviousTrack();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        _mediaKey.NextTrack();
    }
    
    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        _mediaKey.PlayPause();
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Connecting...";

        var client = await _authService.AuthenticateAsync();

        if (client != null)
        {
            StatusText.Text = "Connected";
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1D, 0xB9, 0x54));
        }
        else
        {
            StatusText.Text = "Connection failed. Try again.";
        }
    }
}