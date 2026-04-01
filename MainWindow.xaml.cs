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
}