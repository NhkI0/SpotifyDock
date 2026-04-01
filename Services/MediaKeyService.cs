using WindowsInput;
using WindowsInput.Native;

namespace SpotifyDock.Services;

public class MediaKeyService
{
    private readonly InputSimulator _simulator = new();
    
    public void PlayPause() =>
        _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
    
    public void NextTrack() => 
        _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_NEXT_TRACK);
    
    public void PreviousTrack() =>
        _simulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PREV_TRACK);
}