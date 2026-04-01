using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SpotifyDock.ViewModels;

namespace SpotifyDock;

public partial class MainWindow : Window
{
    private const int SnapDistance = 30;

    private enum DockMode { Floating, AboveTaskbar, OnTaskbar }
    private DockMode _dockMode = DockMode.Floating;
    private double _normalHeight = 80;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private System.Windows.Threading.DispatcherTimer? _topmostTimer;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ForceTopmost();
            SnapAboveTaskbar();

            _topmostTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _topmostTimer.Tick += (_, _) => ForceTopmost();
            _topmostTimer.Start();
        };

        Deactivated += (_, _) => ForceTopmost();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = HwndSource.FromHwnd(
            new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
    }

    private void ForceTopmost()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    // Taskbar measurements

    private double TaskbarHeight =>
        SystemParameters.PrimaryScreenHeight
        - SystemParameters.WorkArea.Bottom;

    // Drag and snap

    private void Border_MouseLeftButtonDown(
        object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
            CheckSnap();
        }
    }

    private void CheckSnap()
    {
        var workArea = SystemParameters.WorkArea;
        var screenBottom = SystemParameters.PrimaryScreenHeight;
        var windowBottom = Top + Height;

        if (windowBottom >= screenBottom - SnapDistance
            && Top >= workArea.Bottom - SnapDistance)
        {
            SnapOnTaskbar();
        }
        else if (windowBottom >= workArea.Bottom - SnapDistance)
        {
            SnapAboveTaskbar();
        }
        else
        {
            Undock();
        }

        // Horizontal edge snapping
        if (_dockMode != DockMode.Floating)
        {
            if (Left <= workArea.Left + SnapDistance)
                Left = workArea.Left;
            else if (Left + Width >= workArea.Right - SnapDistance)
                Left = workArea.Right - Width;
        }
    }

    private void SnapAboveTaskbar()
    {
        var workArea = SystemParameters.WorkArea;
        Height = _normalHeight;
        Top = workArea.Bottom - Height;
        if (Left == 0 && _dockMode == DockMode.Floating)
            Left = workArea.Right - Width - 10;

        _dockMode = DockMode.AboveTaskbar;
        UpdateStyle();
    }

    private void SnapOnTaskbar()
    {
        var workArea = SystemParameters.WorkArea;
        Height = TaskbarHeight;
        Top = workArea.Bottom;

        _dockMode = DockMode.OnTaskbar;
        UpdateStyle();

        ForceTopmost();
    }

    private void Undock()
    {
        if (_dockMode == DockMode.Floating) return;
        Height = _normalHeight;
        _dockMode = DockMode.Floating;
        UpdateStyle();
    }

    private LinearGradientBrush MakeGradient(Color c1, Color c2)
    {
        var brush = new LinearGradientBrush(c1, c2, 45);
        brush.Freeze();
        return brush;
    }

    private static readonly Color BgStart =
        Color.FromRgb(0x1E, 0x1E, 0x36);
    private static readonly Color BgEnd =
        Color.FromRgb(0x16, 0x16, 0x2B);
    private static readonly SolidColorBrush DefaultBorderBrush =
        new(Color.FromRgb(0x2A, 0x2A, 0x48));
    private static readonly DropShadowEffect DefaultShadow = new()
    {
        Color = Colors.Black, BlurRadius = 15,
        ShadowDepth = 2, Opacity = 0.4, Direction = 270
    };

    private void SetNormalLayout()
    {
        AlbumArtBorder.Width = 44;
        AlbumArtBorder.Height = 44;
        AlbumArtClip.Rect = new Rect(0, 0, 44, 44);
        ProgressRow.Height = new GridLength(6);

        BtnPrev.Width = BtnPrev.Height = 30;
        BtnPrev.FontSize = 13;
        BtnNext.Width = BtnNext.Height = 30;
        BtnNext.FontSize = 13;
        BtnPlayPause.Width = BtnPlayPause.Height = 34;
        BtnPlayPause.FontSize = 16;

        TxtTrack.FontSize = 12.5;
        TxtArtist.FontSize = 10.5;
        TxtProgress.Visibility = Visibility.Visible;
        TxtCenterTime.Visibility = Visibility.Collapsed;
        ProgressDot.Visibility = Visibility.Visible;
    }

    private void UpdateStyle()
    {
        switch (_dockMode)
        {
            case DockMode.AboveTaskbar:
                MainBorder.CornerRadius =
                    new CornerRadius(12, 12, 0, 0);
                MainBorder.BorderThickness =
                    new Thickness(1, 1, 1, 0);
                MainBorder.BorderBrush = DefaultBorderBrush;
                MainBorder.Background =
                    MakeGradient(BgStart, BgEnd);
                MainBorder.Effect = DefaultShadow;
                MainBorder.Margin = new Thickness(0);
                MainBorder.Padding = new Thickness(10, 8, 10, 8);
                SetNormalLayout();
                break;

            case DockMode.OnTaskbar:
                MainBorder.CornerRadius = new CornerRadius(8);
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.BorderBrush = new SolidColorBrush(
                    Color.FromArgb(30, 255, 255, 255));
                MainBorder.Background = new SolidColorBrush(
                    Color.FromArgb(180, 32, 32, 32));
                MainBorder.Padding = new Thickness(6, 2, 6, 2);
                MainBorder.Margin = new Thickness(0, 4, 0, 4);
                MainBorder.Effect = null;

                AlbumArtBorder.Width = 28;
                AlbumArtBorder.Height = 28;
                AlbumArtClip.Rect = new Rect(0, 0, 28, 28);
                ProgressRow.Height = new GridLength(2);

                BtnPrev.Width = BtnPrev.Height = 24;
                BtnPrev.FontSize = 10;
                BtnNext.Width = BtnNext.Height = 24;
                BtnNext.FontSize = 10;
                BtnPlayPause.Width = BtnPlayPause.Height = 26;
                BtnPlayPause.FontSize = 12;

                TxtTrack.FontSize = 11;
                TxtArtist.FontSize = 9;
                TxtProgress.Visibility = Visibility.Collapsed;
                TxtCenterTime.Visibility = Visibility.Visible;
                ProgressDot.Visibility = Visibility.Collapsed;
                break;

            case DockMode.Floating:
                MainBorder.CornerRadius = new CornerRadius(12);
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.BorderBrush = DefaultBorderBrush;
                MainBorder.Background =
                    MakeGradient(BgStart, BgEnd);
                MainBorder.Effect = DefaultShadow;
                MainBorder.Margin = new Thickness(0);
                MainBorder.Padding = new Thickness(10, 8, 10, 8);
                SetNormalLayout();
                break;
        }
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
                var percent = Math.Clamp(
                    clickX / totalWidth, 0, 1);
                vm.SeekCommand.Execute(percent);
            }
        }
        e.Handled = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg,
        IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_DISPLAYCHANGE = 0x007E;
        if (msg == WM_DISPLAYCHANGE
            && _dockMode != DockMode.Floating)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_dockMode == DockMode.OnTaskbar)
                    SnapOnTaskbar();
                else
                    SnapAboveTaskbar();
            });
        }
        return IntPtr.Zero;
    }
}