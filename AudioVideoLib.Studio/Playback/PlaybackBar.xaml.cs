namespace AudioVideoLib.Studio.Playback;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

public partial class PlaybackBar : UserControl
{
    private readonly PlaybackController _controller = new();
    private bool _scrubbing;
    private int _totalFrames;

    public PlaybackBar()
    {
        InitializeComponent();
        _controller.Opened += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                PositionSlider.Maximum = _controller.Duration.TotalSeconds;
                UpdateTimeText(TimeSpan.Zero);
            });
        };
        _controller.PositionChanged += (_, pos) =>
        {
            if (_scrubbing)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                PositionSlider.Value = pos.TotalSeconds;
                UpdateTimeText(pos);
                UpdateFrameText(pos);
            });
        };
        _controller.Ended += (_, _) =>
        {
            Dispatcher.Invoke(() => PlayPauseButton.Content = "▶");
        };
        _controller.Failed += (_, ex) =>
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex?.Message ?? "Playback failed", "Playback",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PlayPauseButton.Content = "▶";
            });
        };

        // Sync the slider to whatever the underlying MediaPlayer was initialized to (default 0.5).
        VolumeSlider.Value = _controller.Volume * 100;
        UpdateVolumeUi(_controller.Volume);
    }

    private string _frameUnit = "Frame";

    public void Open(string? filePath, int totalFrames = 0, string unit = "Frame")
    {
        _controller.Stop();
        PlayPauseButton.Content = "▶";
        PositionSlider.Value = 0;
        UpdateTimeText(TimeSpan.Zero);

        _totalFrames = totalFrames;
        _frameUnit = unit;
        if (_totalFrames > 0)
        {
            FrameText.Visibility = Visibility.Visible;
            FrameText.Text = $"{_frameUnit} 0 / {_totalFrames:N0}";
        }
        else
        {
            FrameText.Visibility = Visibility.Collapsed;
            FrameText.Text = string.Empty;
        }

        if (!string.IsNullOrEmpty(filePath))
        {
            _controller.Open(filePath);
        }
    }

    private void UpdateTimeText(TimeSpan position)
    {
        TimeText.Text = $"{Format(position)} / {Format(_controller.Duration)}";
    }

    private void UpdateFrameText(TimeSpan position)
    {
        if (_totalFrames <= 0)
        {
            return;
        }

        var duration = _controller.Duration.TotalSeconds;
        var current = duration > 0
            ? System.Math.Clamp((int)(position.TotalSeconds / duration * _totalFrames), 0, _totalFrames)
            : 0;
        FrameText.Text = $"{_frameUnit} {current:N0} / {_totalFrames:N0}";
    }

    public void Close() => _controller.Dispose();

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_controller.IsPlaying)
        {
            _controller.Pause();
            PlayPauseButton.Content = "▶";
        }
        else
        {
            _controller.Play();
            PlayPauseButton.Content = "❚❚";
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _controller.Stop();
        PlayPauseButton.Content = "▶";
        PositionSlider.Value = 0;
        UpdateTimeText(TimeSpan.Zero);
        if (_totalFrames > 0)
        {
            FrameText.Text = $"{_frameUnit} 0 / {_totalFrames:N0}";
        }
    }

    private void PositionSlider_DragStarted(object sender, DragStartedEventArgs e) => _scrubbing = true;

    private void PositionSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _scrubbing = false;
        _controller.Position = TimeSpan.FromSeconds(PositionSlider.Value);
    }

    private void PositionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        // Click-to-seek (bar area, not thumb): IsMoveToPointEnabled handles the slider value change;
        // we propagate it to the controller.
        if (!_scrubbing)
        {
            _controller.Position = TimeSpan.FromSeconds(PositionSlider.Value);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var linear = e.NewValue / 100.0;
        _controller.Volume = linear;
        UpdateVolumeUi(linear);
    }

    private void UpdateVolumeUi(double linear)
    {
        VolumeText.Text = ((int)System.Math.Round(linear * 100)).ToString(CultureInfo.InvariantCulture) + "%";
        VolumeButton.Content = linear switch
        {
            <= 0.0  => "🔇",
            < 0.34  => "🔈",
            < 0.67  => "🔉",
            _       => "🔊",
        };
    }

    private static string Format(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
