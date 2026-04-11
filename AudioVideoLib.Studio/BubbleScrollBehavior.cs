namespace AudioVideoLib.Studio;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// Forces MouseWheel events on an inner control (typically a multi-line TextBox) to
// bubble up to the nearest scroll-aware parent instead of being consumed by the
// inner control's own scroll handling. Used on dossier fields that sit inside the
// outer ScrollViewer so the wheel keeps scrolling the dossier when the cursor is
// over the field.
public static class BubbleScrollBehavior
{
    public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
        "Enable",
        typeof(bool),
        typeof(BubbleScrollBehavior),
        new PropertyMetadata(false, OnEnableChanged));

    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not UIElement element)
        {
            return;
        }

        e.Handled = true;

        var parent = VisualTreeHelper.GetParent(element) as UIElement;
        var newEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender,
        };
        parent?.RaiseEvent(newEvent);
    }
}
