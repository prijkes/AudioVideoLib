namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using AudioVideoLib.Tags;

public abstract class TagTabViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Header
    {
        get;
        protected init;
    } = string.Empty;

    public string SourceBadge
    {
        get;
        protected init;
    } = string.Empty;

    public bool IsDirty
    {
        get;
        protected set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Notify();
        }
    }

    public virtual bool IsEditable => false;

    public void ResetDirty()
    {
        IsDirty = false;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    protected void Notify([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void SetField<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
        {
            return;
        }

        backingField = value;
        Notify(propertyName);
        IsDirty = true;
    }
}

public sealed class Id3v2TabViewModel : TagTabViewModel
{
    public Id3v2TabViewModel(Id3v2Tag tag)
    {
        Tag = tag;
        Header = $"ID3{tag.Version.ToString().Replace("Id3v", "v")}";
        SourceBadge = Header;

        var yearId = tag.Version >= Id3v2Version.Id3v240 ? "TDRC" : "TYER";

        Title       = GetText(tag, "TIT2") ?? GetText(tag, "TT2");
        Artist      = GetText(tag, "TPE1") ?? GetText(tag, "TP1");
        Album       = GetText(tag, "TALB") ?? GetText(tag, "TAL");
        AlbumArtist = GetText(tag, "TPE2") ?? GetText(tag, "TP2");
        Year        = GetText(tag, yearId) ?? GetText(tag, "TYER") ?? GetText(tag, "TYE");
        Track       = GetText(tag, "TRCK") ?? GetText(tag, "TRK");
        Disc        = GetText(tag, "TPOS") ?? GetText(tag, "TPA");
        Genre       = GetText(tag, "TCON") ?? GetText(tag, "TCO");
        Composer    = GetText(tag, "TCOM") ?? GetText(tag, "TCM");
        Comment     = tag.GetFrame<Id3v2CommentFrame>()?.Text;

        foreach (var frame in tag.Frames)
        {
            AdvancedFrames.Add(new Id3v2FrameRow(frame, () => IsDirty = true));
        }

        ResetDirty();
    }

    public void RemoveFrameRow(Id3v2FrameRow row)
    {
        if (row == null)
        {
            return;
        }

        Tag.RemoveFrame(row.Frame);
        AdvancedFrames.Remove(row);
        IsDirty = true;
    }

    public Id3v2FrameRow AddTextFrame(string identifier, string value = "")
    {
        var frame = new Id3v2TextFrame(Tag.Version, identifier)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
        };
        if (!string.IsNullOrEmpty(value))
        {
            frame.Values.Add(value);
        }

        Tag.SetFrame(frame);
        var row = new Id3v2FrameRow(frame, () => IsDirty = true);
        AdvancedFrames.Add(row);
        IsDirty = true;
        return row;
    }

    public Id3v2FrameRow AddUrlFrame(string identifier, string url = "")
    {
        var frame = new Id3v2UrlLinkFrame(Tag.Version, identifier);
        if (!string.IsNullOrEmpty(url))
        {
            frame.Url = url;
        }

        Tag.SetFrame(frame);
        var row = new Id3v2FrameRow(frame, () => IsDirty = true);
        AdvancedFrames.Add(row);
        IsDirty = true;
        return row;
    }

    public Id3v2FrameRow AddPictureFrame()
    {
        var frame = new Id3v2AttachedPictureFrame(Tag.Version)
        {
            TextEncoding = Id3v2FrameEncodingType.UTF8,
            ImageFormat = Tag.Version < Id3v2Version.Id3v230 ? "JPG" : "image/jpeg",
            PictureType = Id3v2AttachedPictureType.CoverFront,
            Description = string.Empty,
            PictureData = [],
        };
        Tag.SetFrame(frame);
        var row = new Id3v2FrameRow(frame, () => IsDirty = true);
        AdvancedFrames.Add(row);
        IsDirty = true;
        return row;
    }

    public void RefreshRow(Id3v2FrameRow row)
    {
        if (row == null)
        {
            return;
        }

        var index = AdvancedFrames.IndexOf(row);
        if (index < 0)
        {
            return;
        }

        // Re-wrap so the row picks up the fresh Describe() summary after a per-type
        // editor mutates the underlying frame.
        var replacement = new Id3v2FrameRow(row.Frame, () => IsDirty = true);
        AdvancedFrames[index] = replacement;
        IsDirty = true;
    }

    public override bool IsEditable => true;

    public string? Title       { get; set => SetFieldFor(ref field, value); }
    public string? Artist      { get; set => SetFieldFor(ref field, value); }
    public string? Album       { get; set => SetFieldFor(ref field, value); }
    public string? AlbumArtist { get; set => SetFieldFor(ref field, value); }
    public string? Year        { get; set => SetFieldFor(ref field, value); }
    public string? Track       { get; set => SetFieldFor(ref field, value); }
    public string? Disc        { get; set => SetFieldFor(ref field, value); }
    public string? Genre       { get; set => SetFieldFor(ref field, value); }
    public string? Composer    { get; set => SetFieldFor(ref field, value); }
    public string? Comment     { get; set => SetFieldFor(ref field, value); }

    public ObservableCollection<Id3v2FrameRow> AdvancedFrames { get; } = [];

    public Id3v2Tag Tag { get; }

    private void SetFieldFor(ref string? backing, string? value, [CallerMemberName] string? propertyName = null)
    {
        if (backing == value)
        {
            return;
        }

        backing = value;
        Notify(propertyName);
        IsDirty = true;
    }

    public void CommitToTag()
    {
        var yearId = Tag.Version >= Id3v2Version.Id3v240 ? "TDRC" : "TYER";
        SetOrRemoveText(Tag, "TIT2", Title);
        SetOrRemoveText(Tag, "TPE1", Artist);
        SetOrRemoveText(Tag, "TALB", Album);
        SetOrRemoveText(Tag, "TPE2", AlbumArtist);
        SetOrRemoveText(Tag, yearId, Year);
        SetOrRemoveText(Tag, "TRCK", Track);
        SetOrRemoveText(Tag, "TPOS", Disc);
        SetOrRemoveText(Tag, "TCON", Genre);
        SetOrRemoveText(Tag, "TCOM", Composer);
        SetOrRemoveComment(Tag, Comment);
    }

    private static string? GetText(Id3v2Tag tag, string identifier)
    {
        return tag.GetFrame<Id3v2TextFrame>(identifier)?.Values.FirstOrDefault();
    }

    private static void SetOrRemoveText(Id3v2Tag tag, string identifier, string? value)
    {
        var existing = tag.GetFrame<Id3v2TextFrame>(identifier);
        if (string.IsNullOrEmpty(value))
        {
            if (existing != null)
            {
                tag.RemoveFrame(existing);
            }

            return;
        }

        if (existing != null)
        {
            existing.Values.Clear();
            existing.TextEncoding = Id3v2FrameEncodingType.UTF8;
            existing.Values.Add(value);
        }
        else
        {
            var frame = new Id3v2TextFrame(tag.Version, identifier)
            {
                TextEncoding = Id3v2FrameEncodingType.UTF8,
            };
            frame.Values.Add(value);
            tag.SetFrame(frame);
        }
    }

    private static void SetOrRemoveComment(Id3v2Tag tag, string? comment)
    {
        var existing = tag.GetFrame<Id3v2CommentFrame>();
        if (string.IsNullOrEmpty(comment))
        {
            if (existing != null)
            {
                tag.RemoveFrame(existing);
            }

            return;
        }

        if (existing != null)
        {
            existing.TextEncoding = Id3v2FrameEncodingType.UTF8;
            if (string.IsNullOrEmpty(existing.Language))
            {
                existing.Language = "eng";
            }

            existing.Text = comment;
        }
        else
        {
            var frame = new Id3v2CommentFrame(tag.Version)
            {
                TextEncoding = Id3v2FrameEncodingType.UTF8,
                Language = "eng",
                ShortContentDescription = string.Empty,
                Text = comment,
            };
            tag.SetFrame(frame);
        }
    }
}

public sealed class Id3v2FrameRow(Id3v2Frame frame, Action markDirty) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Id3v2Frame Frame { get; } = frame;

    public string Identifier { get; } = frame.Identifier ?? string.Empty;

    public string FrameType { get; } = frame.GetType().Name.Replace("Id3v2", string.Empty).Replace("Frame", string.Empty);

    public int Size => Frame.Data?.Length ?? 0;

    public bool IsEditable { get; } = frame is
        Id3v2TextFrame or
        Id3v2UrlLinkFrame or
        Id3v2UserDefinedTextInformationFrame or
        Id3v2UserDefinedUrlLinkFrame or
        Id3v2CommentFrame;

    public bool IsReadOnlyInGrid => !IsEditable;

    public string Value
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (!IsEditable)
            {
                return;
            }

            var applied = value ?? string.Empty;
            try
            {
                ApplyValue(Frame, applied);
            }
            catch
            {
                return;
            }

            field = applied;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Size)));
            markDirty();
        }
    } = Describe(frame);

    private static void ApplyValue(Id3v2Frame frame, string value)
    {
        switch (frame)
        {
            case Id3v2TextFrame text:
                text.TextEncoding = Id3v2FrameEncodingType.UTF8;
                text.Values.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    text.Values.Add(value);
                }

                break;
            case Id3v2UrlLinkFrame url:
                url.Url = value;
                break;
            case Id3v2UserDefinedTextInformationFrame u:
                u.TextEncoding = Id3v2FrameEncodingType.UTF8;
                u.Value = value;
                break;
            case Id3v2UserDefinedUrlLinkFrame uurl:
                uurl.Url = value;
                break;
            case Id3v2CommentFrame comm:
                comm.TextEncoding = Id3v2FrameEncodingType.UTF8;
                if (string.IsNullOrEmpty(comm.Language))
                {
                    comm.Language = "eng";
                }

                comm.Text = value;
                break;
        }
    }

    private static string Describe(Id3v2Frame f)
    {
        return f switch
        {
            Id3v2TextFrame text => string.Join(" / ", text.Values),
            Id3v2UserDefinedTextInformationFrame u => u.Value ?? string.Empty,
            Id3v2UrlLinkFrame url => url.Url ?? string.Empty,
            Id3v2UserDefinedUrlLinkFrame uurl => uurl.Url ?? string.Empty,
            Id3v2CommentFrame comm => comm.Text ?? string.Empty,
            Id3v2UnsynchronizedLyricsFrame u => $"[{u.Language}:{u.ContentDescriptor}] {u.Lyrics}",
            Id3v2AttachedPictureFrame p => $"{p.ImageFormat} {p.PictureType} {p.PictureData?.Length ?? 0:N0} bytes",
            Id3v2PrivateFrame p => $"[{p.OwnerIdentifier}] {p.PrivateData?.Length ?? 0:N0} bytes",
            Id3v2UniqueFileIdentifierFrame u => $"[{u.OwnerIdentifier}] {u.IdentifierData?.Length ?? 0:N0} bytes",
            _ => $"<{f.Data?.Length ?? 0:N0} bytes>",
        };
    }
}

public sealed class Id3v1TabViewModel : TagTabViewModel
{
    public Id3v1TabViewModel(Id3v1Tag tag)
    {
        Tag = tag;
        Header = tag.Version.ToString().Replace("Id3v", "v");
        SourceBadge = $"ID3{Header}";
        Title = tag.TrackTitle;
        Artist = tag.Artist;
        Album = tag.AlbumTitle;
        Year = tag.AlbumYear;
        Comment = tag.TrackComment;
        Genre = tag.Genre.ToString();
        TrackNumber = tag.TrackNumber;
        ResetDirty();
    }

    public override bool IsEditable => true;

    public string? Title   { get; set => SetStringField(ref field, value); }
    public string? Artist  { get; set => SetStringField(ref field, value); }
    public string? Album   { get; set => SetStringField(ref field, value); }
    public string? Year    { get; set => SetStringField(ref field, value); }
    public string? Comment { get; set => SetStringField(ref field, value); }
    public string? Genre   { get; set => SetStringField(ref field, value); }

    public int TrackNumber
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            Notify();
            IsDirty = true;
        }
    }

    public static IReadOnlyList<string> GenreValues { get; } =
        [.. Enum.GetNames<Id3v1Genre>().OrderBy(n => n)];

    public Id3v1Tag Tag { get; }

    public void CommitToTag()
    {
        Tag.TrackTitle = Title ?? string.Empty;
        Tag.Artist = Artist ?? string.Empty;
        Tag.AlbumTitle = Album ?? string.Empty;
        Tag.AlbumYear = Year ?? string.Empty;
        Tag.TrackComment = Comment ?? string.Empty;
        Tag.TrackNumber = (byte)Math.Clamp(TrackNumber, 0, 255);
        if (!string.IsNullOrEmpty(Genre) && Enum.TryParse<Id3v1Genre>(Genre, true, out var g))
        {
            Tag.Genre = g;
        }
    }

    private void SetStringField(ref string? backing, string? value, [CallerMemberName] string? propertyName = null)
    {
        if (backing == value)
        {
            return;
        }

        backing = value;
        Notify(propertyName);
        IsDirty = true;
    }
}

public sealed class ApeTabViewModel : TagTabViewModel
{
    public ApeTabViewModel(ApeTag tag)
    {
        Tag = tag;
        Header = tag.Version.ToString().Replace("Version", "APEv");
        SourceBadge = Header;

        foreach (var item in tag.Items)
        {
            Items.Add(new ApeItemRow(item, () => IsDirty = true));
        }

        ResetDirty();
    }

    public override bool IsEditable => true;

    public ApeTag Tag { get; }

    public ObservableCollection<ApeItemRow> Items { get; } = [];

    public ApeItemRow AddTextItem(string key, string value = "")
    {
        var item = new ApeUtf8Item(Tag.Version, key);
        if (!string.IsNullOrEmpty(value))
        {
            item.Values.Add(value);
        }

        Tag.SetItem(item);
        var row = new ApeItemRow(item, () => IsDirty = true);
        Items.Add(row);
        IsDirty = true;
        return row;
    }

    public void RemoveItem(ApeItemRow row)
    {
        if (row == null || !Items.Contains(row))
        {
            return;
        }

        var target = Tag.GetItem(row.Key);
        if (target != null)
        {
            Tag.RemoveItem(target);
        }

        Items.Remove(row);
        IsDirty = true;
    }
}

public sealed class ApeItemRow(ApeItem item, Action markDirty) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; } = item.Key;

    public string Type { get; } = item switch
    {
        ApeLocatorItem => "Locator",
        ApeUtf8Item => "UTF-8",
        ApeBinaryItem => "Binary",
        _ => item.GetType().Name,
    };

    public bool IsEditable { get; } = item is ApeUtf8Item or ApeLocatorItem;

    public string Value
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (!IsEditable)
            {
                return;
            }

            field = value ?? string.Empty;

            switch (item)
            {
                case ApeLocatorItem loc:
                    loc.Values.Clear();
                    loc.Values.Add(field);
                    break;
                case ApeUtf8Item utf8:
                    utf8.Values.Clear();
                    utf8.Values.Add(field);
                    break;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            markDirty();
        }
    } = item switch
    {
        ApeLocatorItem loc => string.Join(" / ", loc.Values),
        ApeUtf8Item u => string.Join(" / ", u.Values),
        ApeBinaryItem b => $"<binary {b.Data?.Length ?? 0:N0} bytes>",
        _ => string.Empty,
    };
}

public sealed class Lyrics3v2TabViewModel : TagTabViewModel
{
    public Lyrics3v2TabViewModel(Lyrics3v2Tag tag)
    {
        Tag = tag;
        Header = "Lyrics3v2";
        SourceBadge = "Lyrics3v2";

        foreach (var field in tag.Fields)
        {
            Fields.Add(new Lyrics3v2FieldRow(field, () => IsDirty = true));
        }

        ResetDirty();
    }

    public override bool IsEditable => true;

    public Lyrics3v2Tag Tag { get; }

    public ObservableCollection<Lyrics3v2FieldRow> Fields { get; } = [];

    public Lyrics3v2FieldRow AddTextField(string identifier, string value = "")
    {
        var field = new Lyrics3v2TextField(identifier)
        {
            Value = value ?? string.Empty,
        };
        Tag.SetField(field);
        var row = new Lyrics3v2FieldRow(field, () => IsDirty = true);
        Fields.Add(row);
        IsDirty = true;
        return row;
    }

    public void RemoveField(Lyrics3v2FieldRow row)
    {
        if (row == null || !Fields.Contains(row))
        {
            return;
        }

        var existing = Tag.GetField<Lyrics3v2Field>(row.Identifier);
        if (existing != null)
        {
            Tag.RemoveField(existing);
        }

        Fields.Remove(row);
        IsDirty = true;
    }
}

public sealed class Lyrics3v2FieldRow(Lyrics3v2Field source, Action markDirty) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Identifier { get; } = source.Identifier;

    public bool IsEditable { get; } = source is Lyrics3v2TextField;

    public string Value
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (!IsEditable)
            {
                return;
            }

            field = value ?? string.Empty;

            if (source is Lyrics3v2TextField textField)
            {
                textField.Value = field;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            markDirty();
        }
    } = source switch
    {
        Lyrics3v2TextField t => t.Value ?? string.Empty,
        _ when source.Data is { Length: > 0 } d => $"<{d.Length:N0} bytes>",
        _ => string.Empty,
    };
}

public sealed class VorbisTabViewModel : TagTabViewModel
{
    public VorbisTabViewModel(VorbisComments comments)
    {
        Comments = comments;
        Header = "Vorbis";
        SourceBadge = "Vorbis";

        foreach (var comment in comments.Comments)
        {
            Entries.Add(new VorbisCommentRow(comment, () => IsDirty = true));
        }

        ResetDirty();
    }

    public override bool IsEditable => true;

    public VorbisComments Comments { get; }

    public ObservableCollection<VorbisCommentRow> Entries { get; } = [];

    public VorbisCommentRow AddComment(string name, string value = "")
    {
        var comment = new VorbisComment { Name = name, Value = value ?? string.Empty };
        Comments.Comments.Add(comment);
        var row = new VorbisCommentRow(comment, () => IsDirty = true);
        Entries.Add(row);
        IsDirty = true;
        return row;
    }

    public void RemoveComment(VorbisCommentRow row)
    {
        if (row == null || !Entries.Contains(row))
        {
            return;
        }

        var target = Comments.Comments.FirstOrDefault(c =>
            string.Equals(c.Name, row.Name, StringComparison.OrdinalIgnoreCase) &&
            c.Value == row.Value);
        if (target != null)
        {
            Comments.Comments.Remove(target);
        }

        Entries.Remove(row);
        IsDirty = true;
    }
}

public sealed class VorbisCommentRow(VorbisComment comment, Action markDirty) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value ?? string.Empty;
            try
            {
                comment.Name = field;
            }
            catch
            {
                // Validation rejects invalid chars; keep the VM value anyway so the
                // grid doesn't revert silently, but don't mark dirty.
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            markDirty();
        }
    } = comment.Name ?? string.Empty;

    public string Value
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value ?? string.Empty;
            comment.Value = field;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            markDirty();
        }
    } = comment.Value ?? string.Empty;
}

public sealed class MusicMatchTabViewModel : TagTabViewModel
{
    public MusicMatchTabViewModel(MusicMatchTag tag)
    {
        Tag = tag;
        Header = $"MusicMatch {tag.Version.Trim()}";
        SourceBadge = "MusicMatch";
        Version = tag.Version.Trim();
        XingEncoderVersion = tag.XingEncoderVersion ?? string.Empty;
        UseHeader = tag.UseHeader;
    }

    public MusicMatchTag Tag { get; }

    public string Version { get; }

    public string XingEncoderVersion { get; }

    public bool UseHeader { get; }
}
