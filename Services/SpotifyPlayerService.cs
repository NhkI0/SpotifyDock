using SpotifyAPI.Web;
using SpotifyDock.Models;

namespace SpotifyDock.Services;

public class SpotifyPlayerService
{
    private SpotifyClient? _client;
    private System.Timers.Timer? _pollTimer;

    public event Action<TrackInfo?>? TrackChanged;
    
    public bool isConnected =>  _client != null;

    public void SetClient(SpotifyClient client)
    {
        _client = client;
        StartPolling();
    }

    private void StartPolling()
    {
        _pollTimer?.Dispose();
        _pollTimer = new System.Timers.Timer(2000);
        _pollTimer.Elapsed += async (_, _) => await PollCurrentTrack();
        _pollTimer.AutoReset = true;
        _pollTimer.Start();

        _ = PollCurrentTrack();
    }

    public async Task RefreshNow()
    {
        await Task.Delay(300);
        await PollCurrentTrack();
    }

    public async Task SeekTo(int positionMs)
    {
        if (_client == null) return;
        try
        {
            await _client.Player.SeekTo(
                new SpotifyAPI.Web.PlayerSeekToRequest(positionMs));
            await Task.Delay(200);
            await PollCurrentTrack();
        }
        catch
        {
            // Nuh uh
        }
    }

    private async Task PollCurrentTrack()
    {
        if (_client == null) return;

        try
        {
            var playback = await _client.Player.GetCurrentPlayback();

            if (playback?.Item is FullTrack track)
            {
                var info = new TrackInfo()
                {
                    TrackName = track.Name,
                    ArtistName = string.Join(", ",
                        track.Artists.Select(a => a.Name)),
                    AlbumName = track.Album.Name,
                    AlbumArtUrl = track.Album.Images
                        .FirstOrDefault()?.Url,
                    IsPlaying = playback.IsPlaying,
                    ProgressMs = playback.ProgressMs,
                    DurationMs = track.DurationMs,
                };
                TrackChanged?.Invoke(info);
            }
            else
            {
                TrackChanged?.Invoke(null);
            }
        }
        catch
        {
            // Network error or token issue
        }
    }

    public void Dispose()
    {
        _pollTimer?.Dispose();
    }
}