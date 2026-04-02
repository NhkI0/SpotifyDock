using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpotifyDock.Models;
using SpotifyDock.Services;


namespace SpotifyDock.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly SpotifyAuthService _authService = new();
    private readonly SpotifyPlayerService _playerService = new();
    private readonly MediaKeyService _mediaKeyService = new();
    private readonly DispatcherTimer _interpolateTimer;
    private readonly Stopwatch _sinceLastPoll = new();
    private string? _currentArtUrl;
    private int _durationMs;
    private int _lastKnownProgressMs;


    [ObservableProperty] private string _trackName = "Not Connected";
    [ObservableProperty] private string _artistName = "Click to connect";
    [ObservableProperty] private string _progressText = "0:00 / 0:00";
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private BitmapImage? _albumArt;

    public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768";
    
    partial void OnIsPlayingChanged(bool value) =>
        OnPropertyChanged(nameof(PlayPauseIcon));

    public PlayerViewModel()
    {
        _playerService.TrackChanged += OnTrackChanged;
        
        _interpolateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _interpolateTimer.Tick += InterpolateTick;
        _interpolateTimer.Start();
    }

    private void InterpolateTick(object? sender, EventArgs e)
    {
        if (!IsPlaying || _durationMs <= 0) return;
        
        var currentMs = _lastKnownProgressMs
            + (int)_sinceLastPoll.Elapsed.TotalMilliseconds;
        if (currentMs > _durationMs) currentMs = _durationMs;
        
        ProgressPercent = (double)currentMs / _durationMs * 100;
        ProgressText = FormatProgress(currentMs, _durationMs);
    }

    private static string FormatProgress(int currentMs, int durationMs)
    {
        return $"{FormatTime(currentMs)} / {FormatTime(durationMs)}";
    }
    
    private static string FormatTime(int ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }

    private void OnTrackChanged(TrackInfo? track)
    {
        App.Current.Dispatcher.Invoke (() =>
        {
            if (track == null)
            {
                TrackName = "Nothing Playing";
                ArtistName = "Play something in Spotify";
                ProgressText = "0:00 / 0:00";
                ProgressPercent = 0;
                IsPlaying = false;
                AlbumArt = null;
                _currentArtUrl = null;
                _lastKnownProgressMs = 0;
                _durationMs = 0;
                _sinceLastPoll.Reset();
                return;
            }

            TrackName = track.TrackName;
            ArtistName = track.ArtistName;
            IsPlaying = track.IsPlaying;
            _durationMs = track.DurationMs;
            _lastKnownProgressMs = track.ProgressMs;
            _sinceLastPoll.Restart();
            
            ProgressPercent = track.ProgressPercent;
            ProgressText = track.ProgressText;

            if (track.AlbumArtUrl != _currentArtUrl)
            {
                _currentArtUrl = track.AlbumArtUrl;
                if (track.AlbumArtUrl != null)
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(track.AlbumArtUrl);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.DecodePixelWidth = 50;
                        bmp.EndInit();
                        AlbumArt = bmp;
                    }
                    catch
                    {
                        AlbumArt = null;
                    }
                }
                else
                {
                    AlbumArt = null;
                }
            }
        });
    }
    
    [RelayCommand]
    public async Task ConnectAsync()
    {
        if (IsConnected) return;
        TrackName = "Connecting...";
        ArtistName = "Opening browser for auth";

        var client = await _authService.AuthenticateAsync();
        if (client != null)
        {
            _playerService.SetClient(client);
            IsConnected = true;
        }
        else
        {
            TrackName = "Connection Failed";
            ArtistName = "Click to try again";
        }
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        _mediaKeyService.PlayPause();
        IsPlaying = !IsPlaying;
        await _playerService.RefreshNow();
    }

    [RelayCommand]
    private async Task NextTrackAsync()
    {
        _mediaKeyService.NextTrack();
        await _playerService.RefreshNow();
    }
    
    [RelayCommand]
    private async Task  PreviousTrackAsync()
    {
        _mediaKeyService.PreviousTrack();
        await _playerService.RefreshNow();
    }

    [RelayCommand]
    private async Task SeekAsync(double percent)
    {
        if (_durationMs <= 0) return;
        var positionMs = (int)(percent * _durationMs);
        _lastKnownProgressMs = positionMs;
        _sinceLastPoll.Restart();
        ProgressPercent = percent * 100;
        ProgressText = FormatProgress(positionMs, _durationMs);
        await _playerService.SeekTo(positionMs);
    }
}