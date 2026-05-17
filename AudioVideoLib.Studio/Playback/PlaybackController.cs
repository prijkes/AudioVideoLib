namespace AudioVideoLib.Studio.Playback;

using System;
using System.Windows.Media;
using System.Windows.Threading;

public sealed class PlaybackController : IDisposable
{
    private readonly MediaPlayer _player = new();
    private readonly DispatcherTimer _tick;

    public PlaybackController()
    {
        _tick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _tick.Tick += (_, _) => PositionChanged?.Invoke(this, Position);
        _player.MediaOpened += (_, _) => Opened?.Invoke(this, EventArgs.Empty);
        _player.MediaEnded += (_, _) =>
        {
            _tick.Stop();
            IsPlaying = false;
            Ended?.Invoke(this, EventArgs.Empty);
        };
        _player.MediaFailed += (_, e) => Failed?.Invoke(this, e.ErrorException);
    }

    public event EventHandler? Opened;

    public event EventHandler<TimeSpan>? PositionChanged;

    public event EventHandler? Ended;

    public event EventHandler<Exception>? Failed;

    public bool IsPlaying { get; private set; }

    public TimeSpan Position
    {
        get => _player.Position;
        set => _player.Position = value;
    }

    public TimeSpan Duration =>
        _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : TimeSpan.Zero;

    /// <summary>Linear playback volume in [0, 1]. Mirrors <see cref="MediaPlayer.Volume"/>.</summary>
    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    public void Open(string path)
    {
        _tick.Stop();
        IsPlaying = false;
        _player.Open(new Uri(path));
    }

    public void Play()
    {
        _player.Play();
        _tick.Start();
        IsPlaying = true;
    }

    public void Pause()
    {
        _player.Pause();
        _tick.Stop();
        IsPlaying = false;
    }

    public void Stop()
    {
        _player.Stop();
        _tick.Stop();
        IsPlaying = false;
    }

    public void Dispose()
    {
        _tick.Stop();
        _player.Close();
    }
}
