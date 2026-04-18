namespace AudioVideoLib.Tags;

using System;
using System.Collections.Generic;
using System.Linq;

using AudioVideoLib.Collections;

public sealed partial class Id3v2Tag
{
    /// <summary>
    /// Gets the first frame of type T.
    /// </summary>
    /// <typeparam name="T">The frame type.</typeparam>
    /// <returns>
    /// The first frame of type T if found; otherwise, null.
    /// </returns>
    public T? GetFrame<T>() where T : Id3v2Frame
    {
        return _frames.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Gets the <see cref="Id3v2TextFrame"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The <see cref="Id3v2TextFrame"/> if found; otherwise, null.</returns>
    public Id3v2TextFrame? GetTextFrame(Id3v2TextFrameIdentifier identifier)
    {
        var id = Id3v2TextFrame.GetIdentifier(Version, identifier);
        return _frames.OfType<Id3v2TextFrame>().FirstOrDefault(f => string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the <see cref="Id3v2UrlLinkFrame"/>.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The <see cref="Id3v2UrlLinkFrame"/> if found; otherwise, null.</returns>
    public Id3v2UrlLinkFrame? GetUrlLinkFrame(Id3v2UrlLinkFrameIdentifier identifier)
    {
        var id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
        return _frames.OfType<Id3v2UrlLinkFrame>().FirstOrDefault(f => string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first frame of type T with a matching frame identifier.
    /// </summary>
    /// <typeparam name="T">The frame type.</typeparam>
    /// <param name="identifier">The identifier of the frame.</param>
    /// <returns>
    /// The first frame of type T with a matching frame identifier if found; otherwise, null.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="identifier"/> is null.</exception>
    public T? GetFrame<T>(string identifier) where T : Id3v2Frame
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return _frames.OfType<T>().FirstOrDefault(f => string.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all <see cref="Id3v2UrlLinkFrame"/>s.
    /// </summary>
    /// <param name="identifier">The identifier.</param>
    /// <returns>
    /// The <see cref="Id3v2UrlLinkFrame"/>s.
    /// </returns>
    public IEnumerable<Id3v2UrlLinkFrame> GetUrlLinkFrames(Id3v2UrlLinkFrameIdentifier identifier)
    {
        var id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
        return _frames.OfType<Id3v2UrlLinkFrame>().Where(f => string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all frames of type T.
    /// </summary>
    /// <typeparam name="T">The frame type.</typeparam>
    /// <returns>
    /// The frames of type T.
    /// </returns>
    public IEnumerable<T> GetFrames<T>() where T : Id3v2Frame
    {
        return _frames.OfType<T>();
    }

    /// <summary>
    /// Gets all frames of type T and with a matching frame identifier.
    /// </summary>
    /// <typeparam name="T">The frame type.</typeparam>
    /// <param name="identifier">The identifier of the frame.</param>
    /// <returns>
    /// A list of frames of type T with a matching frame identifier.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="identifier"/> is null.</exception>
    public IEnumerable<T> GetFrames<T>(string identifier) where T : Id3v2Frame
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return _frames.OfType<T>().Where(f => string.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates the first matching frame if found; else, adds a new frame.
    /// </summary>
    /// <param name="frame">Frame to add to the <see cref="Id3v2Tag" />.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="frame"/> is null.</exception>
    /// <exception cref="InvalidVersionException">Thrown if the version of the tag does not match the version of the <paramref name="frame"/>.</exception>
    /// <remarks>
    /// The frame needs to have the same version set as the <see cref="Id3v2Tag" />, otherwise an <see cref="InvalidVersionException" /> will be thrown.
    /// </remarks>
    public void SetFrame(Id3v2Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Version != Version)
        {
            throw new InvalidVersionException("The version of the frame needs to to match the version of the tag.");
        }

        int i, frameCount = _frames.Count;
        for (i = 0; i < frameCount; i++)
        {
            if (!ReferenceEquals(_frames[i], frame)
                && (!string.Equals(_frames[i].Identifier, frame.Identifier, StringComparison.OrdinalIgnoreCase) || !_frames[i].Equals(frame)))
            {
                continue;
            }

            _frames[i] = frame;
            break;
        }

        if (i == frameCount)
        {
            _frames.Add(frame);
        }
    }

    /// <summary>
    /// Updates a list of frames with a matching identifier if found; else, adds it.
    /// </summary>
    /// <param name="frames">The frames.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="frames"/> is null.</exception>
    /// <remarks>
    /// The frames needs to have the same version set as the <see cref="Id3v2Tag" />, otherwise an <see cref="InvalidVersionException" /> will be thrown.
    /// </remarks>
    public void SetFrames(IEnumerable<Id3v2Frame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        UnbindFrameEvents();
        foreach (var frame in frames)
        {
            SetFrame(frame);
        }

        BindFrameEvents();
        ValidateFrames();
    }

    /// <summary>
    /// Removes the frame.
    /// </summary>
    /// <param name="frame">The frame.</param>
    public void RemoveFrame(Id3v2Frame? frame)
    {
        // Try to remove by reference first before trying to remove by calling the Equal() on all frames
        if ((frame != null) && !_frames.Remove(_frames.FirstOrDefault(f => ReferenceEquals(f, frame))!))
        {
            _frames.Remove(frame);
        }
    }

    /// <summary>
    /// Removes all frames of type T.
    /// </summary>
    /// <typeparam name="T">A class of type <see cref="Id3v2Frame" />.</typeparam>
    public void RemoveFrames<T>() where T : Id3v2Frame
    {
        RemoveFrames<T>(true);
    }

    /// <summary>
    /// Removes the frames.
    /// </summary>
    /// <param name="frames">The frames.</param>
    /// <exception cref="System.ArgumentNullException">Thrown if frames is null.</exception>
    public void RemoveFrames(IEnumerable<Id3v2Frame> frames)
    {
        RemoveFrames(frames, true);
    }

    private Id3v2TextFrame? GetVersionedTextFrame(Id3v2TextFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        return Version >= minVersion ? GetTextFrame(identifier) : null;
    }

    private void SetVersionedTextFrame(Id3v2TextFrame? value, Id3v2TextFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        if (Version < minVersion)
        {
            return;
        }

        if (value == null)
        {
            RemoveFrame(GetTextFrame(identifier));
        }
        else
        {
            SetFrame(value);
        }
    }

    private Id3v2UrlLinkFrame? GetVersionedUrlLinkFrame(Id3v2UrlLinkFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        return Version >= minVersion ? GetUrlLinkFrame(identifier) : null;
    }

    private void SetVersionedUrlLinkFrame(Id3v2UrlLinkFrame? value, Id3v2UrlLinkFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        if (Version < minVersion)
        {
            return;
        }

        if (value == null)
        {
            RemoveFrame(GetUrlLinkFrame(identifier));
        }
        else
        {
            SetFrame(value);
        }
    }

    private Id3v2FrameCollection<Id3v2UrlLinkFrame> GetVersionedUrlFrameCollection(Id3v2UrlLinkFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        return Version >= minVersion ? GetFrameCollection(identifier) : GetFrameCollection<Id3v2UrlLinkFrame>([]);
    }

    private void SetVersionedUrlFrameCollection(Id3v2FrameCollection<Id3v2UrlLinkFrame>? value, Id3v2UrlLinkFrameIdentifier identifier, Id3v2Version minVersion = Id3v2Version.Id3v220)
    {
        if (Version < minVersion)
        {
            return;
        }

        RemoveFrames<Id3v2UrlLinkFrame>(identifier);
        if (value != null)
        {
            SetFrames(value);
        }

        ValidateFrames();
    }

    private TFrame? GetVersionedSingleFrame<TFrame>(Id3v2Version minVersion = Id3v2Version.Id3v220) where TFrame : Id3v2Frame
    {
        return Version >= minVersion ? GetFrame<TFrame>() : null;
    }

    private void SetVersionedSingleFrame<TFrame>(TFrame? value, Id3v2Version minVersion = Id3v2Version.Id3v220) where TFrame : Id3v2Frame
    {
        if (Version < minVersion)
        {
            return;
        }

        if (value == null)
        {
            RemoveFrame(GetFrame<TFrame>());
        }
        else
        {
            SetFrame(value);
        }
    }

    private Id3v2FrameCollection<TFrame> GetVersionedFrameCollection<TFrame>(Id3v2Version minVersion = Id3v2Version.Id3v220) where TFrame : Id3v2Frame
    {
        return Version >= minVersion ? GetFrameCollection<TFrame>() : GetFrameCollection<TFrame>([]);
    }

    private void SetVersionedFrameCollection<TFrame>(Id3v2FrameCollection<TFrame>? value, Id3v2Version minVersion = Id3v2Version.Id3v220) where TFrame : Id3v2Frame
    {
        if (Version < minVersion)
        {
            return;
        }

        RemoveFrames<TFrame>(false);
        if (value != null)
        {
            SetFrames(value);
        }

        ValidateFrames();
    }

    private void ItemAdd<T>(object? sender, ListItemAddEventArgs<T> e) where T : Id3v2Frame
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.Item == null)
        {
            throw new NullReferenceException("e.Item may not be null");
        }

        SetFrame(e.Item);
    }

    private void ItemRemove<T>(object? sender, ListItemRemoveEventArgs<T> e) where T : Id3v2Frame
    {
        if (e != null && e.Item != null)
        {
            RemoveFrame(e.Item);
        }
    }

    private void AddFrameCollectionEvents<T>(NotifyingList<T> list) where T : Id3v2Frame
    {
        // Remove a possible already added ItemAdd delegate
        list.ItemAdd -= ItemAdd;

        // Add it
        list.ItemAdd += ItemAdd;

        // Remove a possible already added ItemRemove delegate
        list.ItemRemove -= ItemRemove;

        // Add it
        list.ItemRemove += ItemRemove;
    }

    private Id3v2FrameCollection<T> GetFrameCollection<T>(IEnumerable<T> items) where T : Id3v2Frame
    {
        Id3v2FrameCollection<T> list = [.. items];
        AddFrameCollectionEvents(list);
        return list;
    }

    private Id3v2FrameCollection<T> GetFrameCollection<T>() where T : Id3v2Frame
    {
        var frames = GetFrames<T>();
        return GetFrameCollection(frames);
    }

    private Id3v2FrameCollection<Id3v2UrlLinkFrame> GetFrameCollection(Id3v2UrlLinkFrameIdentifier identifier)
    {
        var frames = GetUrlLinkFrames(identifier);
        return GetFrameCollection(frames);
    }

    private void RemoveFrames<T>(bool validateFrames) where T : Id3v2Frame
    {
        UnbindFrameEvents();
        _frames.RemoveAll(f => f is T);
        BindFrameEvents();
        if (validateFrames)
        {
            ValidateFrames();
        }
    }

    private void RemoveFrames(IEnumerable<Id3v2Frame> frames, bool validateFrames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        UnbindFrameEvents();
        foreach (var frame in frames)
        {
            RemoveFrame(frame);
        }

        BindFrameEvents();
        if (validateFrames)
        {
            ValidateFrames();
        }
    }

    private void RemoveFrames<T>(Id3v2UrlLinkFrameIdentifier identifier)
    {
        var id = Id3v2UrlLinkFrame.GetIdentifier(Version, identifier);
        _frames.RemoveAll(f => f is Id3v2UrlLinkFrame && string.Equals(f.Identifier, id, StringComparison.OrdinalIgnoreCase));
    }
}
