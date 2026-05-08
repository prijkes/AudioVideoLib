namespace AudioVideoLib.Studio.Mvvm;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Shared INotifyPropertyChanged plumbing for view-models.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Setter helper: short-circuits on equality, writes the field, raises
    /// PropertyChanged for the caller-member-name'd property.
    /// </summary>
    protected void Set<T>(ref T storage, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(storage, value))
        {
            return;
        }
        storage = value;
        OnPropertyChanged(prop);
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for <paramref name="prop"/>. Marked
    /// <c>virtual</c> so subclasses can also raise change notifications for
    /// computed projections.
    /// </summary>
    protected virtual void OnPropertyChanged(string? prop)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}
