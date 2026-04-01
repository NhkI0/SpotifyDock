namespace SpotifyDock.Models;

public class TrackInfo
{
    public string TrackName { get; set; } =  string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string? AlbumArtUrl { get; set; }
    public bool IsPlaying { get; set; }
    public int ProgressMs { get; set; }
    public int DurationMs { get; set; }
    
    public double ProgressPercent =>
        DurationMs > 0 ? (double)ProgressMs / DurationMs * 100 : 0;

    public string ProgressText =>
        $"{FormatTime(ProgressMs)} / {FormatTime(DurationMs)}";

    private static string FormatTime(int ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}