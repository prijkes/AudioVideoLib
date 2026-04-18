namespace AudioVideoLib.Studio.Playback;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

public partial class PlaybackBar : UserControl
{
    private readonly PlaybackController _controller = new();
    private bool _scrubbing;

    public PlaybackBar()
    {
        InitializeComponent();
        _controller.Opened += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                PositionSlider.Maximum = _controller.Duration.TotalSeconds;
                DurationText.Text = Format(_controller.Duration);
                PositionText.Text = Format(TimeSpan.Zero);
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
                PositionText.Text = Format(pos);
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
    }

    public void Open(string? filePath)
    {
        _controller.Stop();
        PlayPauseButton.Content = "▶";
        PositionSlider.Value = 0;
        PositionText.Text = "00:00";
        DurationText.Text = "00:00";

        if (!string.IsNullOrEmpty(filePath))
        {
            _controller.Open(filePath);
        }
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
        PositionText.Text = "00:00";
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

    private static string Format(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
