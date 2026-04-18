namespace AudioVideoLib.Studio;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using AudioVideoLib.Tags;

public static class TagJsonExporter
{
    public static string Export(string filePath, IEnumerable<IAudioTag> tags, VorbisComments? vorbis)
    {
        var root = new JsonObject
        {
            ["file"] = filePath,
            ["size"] = new FileInfo(filePath).Exists ? new FileInfo(filePath).Length : 0,
        };

        var array = new JsonArray();
        foreach (var tag in tags)
        {
            var node = Convert(tag);
            if (node != null)
            {
                array.Add(node);
            }
        }

        if (vorbis != null)
        {
            array.Add(ConvertVorbis(vorbis));
        }

        root["tags"] = array;

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonNode? Convert(IAudioTag tag) => tag switch
    {
        Id3v2Tag v2 => ConvertId3v2(v2),
        Id3v1Tag v1 => ConvertId3v1(v1),
        ApeTag ape => ConvertApe(ape),
        Lyrics3v2Tag l3 => ConvertLyrics3v2(l3),
        Lyrics3Tag l3v1 => ConvertLyrics3v1(l3v1),
        MusicMatchTag mm => ConvertMusicMatch(mm),
        _ => new JsonObject { ["kind"] = tag.GetType().Name },
    };

    private static JsonNode ConvertId3v2(Id3v2Tag tag)
    {
        var frames = new JsonArray();
        foreach (var frame in tag.Frames)
        {
            var obj = new JsonObject
            {
                ["id"] = frame.Identifier,
                ["type"] = frame.GetType().Name,
            };

            switch (frame)
            {
                case Id3v2TextFrame text:
                    obj["values"] = new JsonArray([.. (text.Values ?? []).Select(v => JsonValue.Create(v))]);
                    break;
                case Id3v2UrlLinkFrame url:
                    obj["url"] = url.Url;
                    break;
                case Id3v2CommentFrame comm:
                    obj["language"] = comm.Language;
                    obj["description"] = comm.ShortContentDescription;
                    obj["text"] = comm.Text;
                    break;
                case Id3v2AttachedPictureFrame pic:
                    obj["mime"] = pic.ImageFormat;
                    obj["pictureType"] = pic.PictureType.ToString();
                    obj["description"] = pic.Description;
                    obj["size"] = pic.PictureData?.Length ?? 0;
                    break;
                case Id3v2UnsynchronizedLyricsFrame uslt:
                    obj["language"] = uslt.Language;
                    obj["description"] = uslt.ContentDescriptor;
                    obj["lyrics"] = uslt.Lyrics;
                    break;
                case Id3v2PrivateFrame priv:
                    obj["owner"] = priv.OwnerIdentifier;
                    obj["size"] = priv.PrivateData?.Length ?? 0;
                    break;
                case Id3v2UniqueFileIdentifierFrame ufid:
                    obj["owner"] = ufid.OwnerIdentifier;
                    obj["size"] = ufid.Identifier?.Length ?? 0;
                    break;
            }

            frames.Add(obj);
        }

        return new JsonObject
        {
            ["kind"] = "Id3v2",
            ["version"] = tag.Version.ToString(),
            ["unsynchronization"] = tag.UseUnsynchronization,
            ["extendedHeader"] = tag.UseExtendedHeader,
            ["footer"] = tag.UseFooter,
            ["frames"] = frames,
        };
    }

    private static JsonNode ConvertId3v1(Id3v1Tag tag) => new JsonObject
    {
        ["kind"] = "Id3v1",
        ["version"] = tag.Version.ToString(),
        ["title"] = tag.TrackTitle,
        ["artist"] = tag.Artist,
        ["album"] = tag.AlbumTitle,
        ["year"] = tag.AlbumYear,
        ["comment"] = tag.TrackComment,
        ["track"] = tag.TrackNumber,
    };

    private static JsonNode ConvertApe(ApeTag tag)
    {
        var items = new JsonArray();
        foreach (var item in tag.Items)
        {
            items.Add(new JsonObject
            {
                ["key"] = item.Key,
                ["type"] = item.ItemType.ToString(),
                ["value"] = item is ApeUtf8Item u ? string.Join("; ", u.Values) : null,
            });
        }

        return new JsonObject
        {
            ["kind"] = "Ape",
            ["version"] = tag.Version.ToString(),
            ["items"] = items,
        };
    }

    private static JsonNode ConvertLyrics3v2(Lyrics3v2Tag tag)
    {
        var fields = new JsonArray();
        foreach (var field in tag.Fields)
        {
            var fieldData = field.Data;
            var text = fieldData != null ? System.Text.Encoding.UTF8.GetString(fieldData) : null;
            fields.Add(new JsonObject
            {
                ["id"] = field.Identifier,
                ["value"] = text,
            });
        }

        return new JsonObject
        {
            ["kind"] = "Lyrics3v2",
            ["fields"] = fields,
        };
    }

    private static JsonNode ConvertLyrics3v1(Lyrics3Tag tag) => new JsonObject
    {
        ["kind"] = "Lyrics3",
        ["lyrics"] = tag.Lyrics,
    };

    private static JsonNode ConvertMusicMatch(MusicMatchTag tag) => new JsonObject
    {
        ["kind"] = "MusicMatch",
        ["version"] = tag.Version,
        ["title"] = tag.SongTitle,
        ["artist"] = tag.ArtistName,
        ["album"] = tag.AlbumTitle,
        ["genre"] = tag.Genre,
        ["tempo"] = tag.Tempo,
        ["mood"] = tag.Mood,
        ["duration"] = tag.SongDuration,
        ["lyrics"] = tag.Lyrics,
    };

    private static JsonNode ConvertVorbis(VorbisComments comments)
    {
        var list = new JsonArray();
        foreach (var c in comments.Comments)
        {
            list.Add(new JsonObject
            {
                ["name"] = c.Name,
                ["value"] = c.Value,
            });
        }

        return new JsonObject
        {
            ["kind"] = "VorbisComments",
            ["vendor"] = comments.Vendor,
            ["comments"] = list,
        };
    }
}
