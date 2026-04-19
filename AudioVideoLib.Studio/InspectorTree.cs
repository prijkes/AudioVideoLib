namespace AudioVideoLib.Studio;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;
using AudioVideoLib.Tags;

public sealed class InspectorNode
{
    public string Label { get; init; } = string.Empty;

    public long StartOffset { get; init; }

    public long EndOffset { get; init; }

    public long Size => EndOffset - StartOffset;

    public string SizeLabel => Size > 0 ? $"({FormatSize(Size)})" : string.Empty;

    public string OffsetTooltip => $"0x{StartOffset:X8} .. 0x{EndOffset:X8}  ({Size:N0} bytes)";

    public ObservableCollection<InspectorNode> Children { get; } = [];

    public ObservableCollection<InspectorProperty> Properties { get; } = [];

    public bool IsExpanded { get; set; }

    public object? Payload { get; init; }

    private static string FormatSize(long bytes) => bytes < 1024
        ? $"{bytes:N0} B"
        : bytes < 1024 * 1024
            ? $"{bytes / 1024.0:0.#} KB"
            : $"{bytes / (1024.0 * 1024.0):0.##} MB";
}

public sealed class InspectorProperty
{
    public string Name { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public long? HighlightStart { get; init; }

    public int? HighlightLength { get; init; }
}

public static class InspectorTreeBuilder
{
    private static int MaxAudioFramesInTree => AppSettings.Current.MaxAudioFramesInTree;

    public static InspectorNode Build(string filePath, byte[] fileBytes, IReadOnlyList<IAudioTagOffset> offsets, IAudioStream? audioStream)
    {
        var root = new InspectorNode
        {
            Label = Path.GetFileName(filePath),
            StartOffset = 0,
            EndOffset = fileBytes.Length,
            IsExpanded = true,
        };

        root.Properties.Add(Prop("Path", filePath));
        root.Properties.Add(Prop("Size", $"{fileBytes.Length:N0} bytes"));
        root.Properties.Add(Prop("Modified", File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm:ss")));
        root.Properties.Add(Prop("Offset", $"0x{0:X8} .. 0x{fileBytes.Length:X8}"));

        var sorted = offsets.OrderBy(o => o.StartOffset).ToList();
        long cursor = 0;

        foreach (var offset in sorted)
        {
            if (offset.StartOffset > cursor)
            {
                root.Children.Add(BuildAudioRegion(cursor, offset.StartOffset, audioStream));
            }

            root.Children.Add(BuildTagNode(fileBytes, offset));
            cursor = offset.EndOffset;
        }

        if (cursor < fileBytes.Length)
        {
            root.Children.Add(BuildAudioRegion(cursor, fileBytes.Length, audioStream));
        }

        // Container-only formats: when no tag offsets are detected, surface the container's structure.
        if (sorted.Count == 0)
        {
            switch (audioStream)
            {
                case FlacStream flac:
                    root.Children.Clear();
                    BuildFlacTree(root, fileBytes, flac);
                    break;
                case RiffStream riff:
                    root.Children.Clear();
                    BuildRiffTree(root, riff);
                    break;
                case AiffStream aiff:
                    root.Children.Clear();
                    BuildAiffTree(root, aiff);
                    break;
                case OggStream ogg:
                    root.Children.Clear();
                    BuildOggTree(root, ogg);
                    break;
                case DsfStream dsf:
                    root.Children.Clear();
                    BuildDsfTree(root, dsf);
                    break;
                case DffStream dff:
                    root.Children.Clear();
                    BuildDffTree(root, dff);
                    break;
                case Mp4Stream mp4:
                    root.Children.Clear();
                    BuildMp4Tree(root, mp4);
                    break;
                case AsfStream asf:
                    root.Children.Clear();
                    BuildAsfTree(root, asf);
                    break;
                case MatroskaStream mkv:
                    root.Children.Clear();
                    BuildMatroskaTree(root, mkv);
                    break;
            }
        }

        return root;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// RIFF / WAV
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildRiffTree(InspectorNode root, RiffStream riff)
    {
        root.Properties.Add(Prop("Format", $"RIFF / {riff.FormatType}", 0, 12));
        if (riff.SampleRate > 0)
        {
            root.Properties.Add(Prop("Sample rate", $"{riff.SampleRate} Hz"));
            root.Properties.Add(Prop("Channels", riff.Channels.ToString()));
            root.Properties.Add(Prop("Bits/sample", riff.BitsPerSample.ToString()));
            root.Properties.Add(Prop("Audio format", $"0x{riff.AudioFormat:X4}"));
        }

        root.Properties.Add(Prop("Chunk count", riff.Chunks.Count.ToString()));

        foreach (var chunk in riff.Chunks)
        {
            var node = new InspectorNode
            {
                Label = chunk.Id,
                StartOffset = chunk.StartOffset,
                EndOffset = chunk.EndOffset,
            };

            node.Properties.Add(Prop("ID", chunk.Id, chunk.StartOffset, 4));
            node.Properties.Add(Prop("Data size", $"{chunk.EndOffset - chunk.StartOffset - 8:N0} bytes", chunk.StartOffset + 4, 4));

            if (chunk.Id == "fmt " && chunk.Data.Length >= 16)
            {
                node.Properties.Add(Prop("Audio format", $"0x{riff.AudioFormat:X4}", chunk.StartOffset + 8, 2));
                node.Properties.Add(Prop("Channels", riff.Channels.ToString(), chunk.StartOffset + 10, 2));
                node.Properties.Add(Prop("Sample rate", $"{riff.SampleRate} Hz", chunk.StartOffset + 12, 4));
                node.Properties.Add(Prop("Byte rate", $"{riff.ByteRate:N0} B/s", chunk.StartOffset + 16, 4));
                node.Properties.Add(Prop("Block align", riff.BlockAlign.ToString(), chunk.StartOffset + 20, 2));
                node.Properties.Add(Prop("Bits/sample", riff.BitsPerSample.ToString(), chunk.StartOffset + 22, 2));
            }
            else if (chunk.Id == "data")
            {
                node.Properties.Add(Prop("Sample data", $"{riff.DataSize:N0} bytes", riff.DataOffset, (int)System.Math.Min(riff.DataSize, int.MaxValue)));
            }

            root.Children.Add(node);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// AIFF
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildAiffTree(InspectorNode root, AiffStream aiff)
    {
        root.Properties.Add(Prop("Format", aiff.FormatType, 0, 12));
        if (aiff.SampleRate > 0)
        {
            root.Properties.Add(Prop("Sample rate", $"{aiff.SampleRate:0} Hz"));
            root.Properties.Add(Prop("Channels", aiff.Channels.ToString()));
            root.Properties.Add(Prop("Sample size", $"{aiff.SampleSize}-bit"));
            root.Properties.Add(Prop("Sample frames", aiff.SampleFrames.ToString("N0")));
            if (aiff.Compression != null)
            {
                root.Properties.Add(Prop("Compression", aiff.Compression));
            }
        }

        foreach (var chunk in aiff.Chunks)
        {
            var node = new InspectorNode
            {
                Label = chunk.Id,
                StartOffset = chunk.StartOffset,
                EndOffset = chunk.EndOffset,
            };

            node.Properties.Add(Prop("ID", chunk.Id, chunk.StartOffset, 4));
            node.Properties.Add(Prop("Data size", $"{chunk.EndOffset - chunk.StartOffset - 8:N0} bytes", chunk.StartOffset + 4, 4));

            if (chunk.Id == "COMM" && chunk.Data.Length >= 18)
            {
                node.Properties.Add(Prop("Channels", aiff.Channels.ToString(), chunk.StartOffset + 8, 2));
                node.Properties.Add(Prop("Sample frames", aiff.SampleFrames.ToString("N0"), chunk.StartOffset + 10, 4));
                node.Properties.Add(Prop("Sample size", $"{aiff.SampleSize}-bit", chunk.StartOffset + 14, 2));
                node.Properties.Add(Prop("Sample rate", $"{aiff.SampleRate:0} Hz", chunk.StartOffset + 16, 10));
            }
            else if (chunk.Id == "SSND")
            {
                node.Properties.Add(Prop("Sound data", $"{aiff.SsndSize:N0} bytes", aiff.SsndOffset, (int)System.Math.Min(aiff.SsndSize, int.MaxValue)));
            }

            root.Children.Add(node);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// OGG
    ////------------------------------------------------------------------------------------------------------------------------------

    private static int MaxOggPagesInTree => AppSettings.Current.MaxOggPagesInTree;

    private static void BuildOggTree(InspectorNode root, OggStream ogg)
    {
        root.Properties.Add(Prop("Format", "OGG container"));
        root.Properties.Add(Prop("Page count", ogg.PageCount.ToString("N0")));
        root.Properties.Add(Prop("Total granule", ogg.TotalGranulePosition.ToString("N0")));

        var shown = System.Math.Min(ogg.Pages.Count, MaxOggPagesInTree);
        for (var i = 0; i < shown; i++)
        {
            var page = ogg.Pages[i];
            var node = new InspectorNode
            {
                Label = $"Page {i + 1} (seq {page.SequenceNumber})",
                StartOffset = page.StartOffset,
                EndOffset = page.EndOffset,
            };

            var flags = new List<string>();
            if (page.IsContinuation)
            {
                flags.Add("continuation");
            }

            if (page.IsBeginningOfStream)
            {
                flags.Add("BOS");
            }

            if (page.IsEndOfStream)
            {
                flags.Add("EOS");
            }

            node.Properties.Add(Prop("Serial", $"0x{page.SerialNumber:X8}", page.StartOffset + 14, 4));
            node.Properties.Add(Prop("Sequence", page.SequenceNumber.ToString(), page.StartOffset + 18, 4));
            node.Properties.Add(Prop("Granule", page.GranulePosition.ToString("N0"), page.StartOffset + 6, 8));
            node.Properties.Add(Prop("Flags", flags.Count > 0 ? string.Join(", ", flags) : "none", page.StartOffset + 5, 1));
            node.Properties.Add(Prop("Segments", page.SegmentCount.ToString(), page.StartOffset + 26, 1));
            node.Properties.Add(Prop("Payload size", $"{page.PayloadSize:N0} bytes", page.StartOffset + 27, page.SegmentCount));
            node.Properties.Add(Prop("Checksum", $"0x{page.Checksum:X8}", page.StartOffset + 22, 4));
            root.Children.Add(node);
        }

        if (ogg.Pages.Count > shown)
        {
            root.Children.Add(SimpleNode(
                $"... {ogg.Pages.Count - shown:N0} more pages",
                ogg.Pages[shown].StartOffset,
                ogg.Pages[^1].EndOffset));
        }
    }

    private static InspectorNode BuildTagNode(byte[] fileBytes, IAudioTagOffset offset)
    {
        return offset.AudioTag switch
        {
            Id3v2Tag v2 => BuildId3v2(fileBytes, offset.StartOffset, offset.EndOffset, v2),
            Id3v1Tag v1 => BuildId3v1(fileBytes, offset.StartOffset, offset.EndOffset, v1),
            ApeTag ape => BuildApe(fileBytes, offset.StartOffset, offset.EndOffset, ape),
            Lyrics3Tag l3v1 => BuildLyrics3v1(offset.StartOffset, offset.EndOffset, l3v1),
            Lyrics3v2Tag l3 => BuildLyrics3v2(offset.StartOffset, offset.EndOffset, l3),
            MusicMatchTag mm => BuildMusicMatch(offset.StartOffset, offset.EndOffset, mm),
            _ => SimpleNode(offset.AudioTag.GetType().Name, offset.StartOffset, offset.EndOffset),
        };
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// ID3v2
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildId3v2(byte[] fileBytes, long start, long end, Id3v2Tag tag)
    {
        var versionLabel = tag.Version.ToString().Replace("Id3v", "v");
        var node = new InspectorNode
        {
            Label = $"ID3{versionLabel}",
            StartOffset = start,
            EndOffset = end,
            IsExpanded = true,
        };

        node.Properties.Add(Prop("Version", versionLabel, start + 3, 2));
        node.Properties.Add(Prop("Flags", $"0x{(start + 5 < fileBytes.Length ? fileBytes[start + 5] : 0):X2}", start + 5, 1));
        node.Properties.Add(Prop("Tag size", $"{end - start:N0} bytes", start + 6, 4));
        node.Properties.Add(Prop("Unsynchronization", tag.UseUnsynchronization.ToString()));
        node.Properties.Add(Prop("Extended header", tag.UseExtendedHeader.ToString()));
        node.Properties.Add(Prop("Footer", tag.UseFooter.ToString()));
        node.Properties.Add(Prop("Frame count", tag.Frames.Count().ToString()));

        // Header node
        node.Children.Add(new InspectorNode
        {
            Label = "Header",
            StartOffset = start,
            EndOffset = start + 10,
            Properties =
            {
                Prop("Magic", "ID3", start, 3),
                Prop("Version", versionLabel, start + 3, 2),
                Prop("Flags", $"0x{(start + 5 < fileBytes.Length ? fileBytes[start + 5] : 0):X2}", start + 5, 1),
                Prop("Size (synchsafe)", $"{end - start - 10:N0} bytes", start + 6, 4),
            },
        });

        // Walk frames from raw bytes
        var idLen = tag.Version < Id3v2Version.Id3v230 ? 3 : 4;
        var sizeLen = tag.Version < Id3v2Version.Id3v230 ? 3 : 4;
        var flagsLen = tag.Version < Id3v2Version.Id3v230 ? 0 : 2;
        var frameHeaderLen = idLen + sizeLen + flagsLen;
        var isSynchsafe = tag.Version >= Id3v2Version.Id3v240;

        var frameCursor = start + 10;
        // Skip extended header if present
        if (tag.UseExtendedHeader && frameCursor + 4 <= end)
        {
            var extSize = ReadBE32(fileBytes, frameCursor);
            if (tag.Version >= Id3v2Version.Id3v240)
            {
                extSize = DecodeSynchsafe(extSize);
            }

            var extEnd = frameCursor + (tag.Version >= Id3v2Version.Id3v240 ? extSize : extSize + 4);
            node.Children.Add(new InspectorNode
            {
                Label = "Extended Header",
                StartOffset = frameCursor,
                EndOffset = extEnd,
                Properties =
                {
                    Prop("Size", $"{extEnd - frameCursor:N0} bytes", frameCursor, 4),
                    Prop("CRC present", tag.ExtendedHeader?.CrcDataPresent.ToString() ?? "?"),
                },
            });
            frameCursor = extEnd;
        }

        while (frameCursor + frameHeaderLen <= end)
        {
            // Check for padding (0x00 bytes)
            if (fileBytes[frameCursor] == 0x00)
            {
                if (frameCursor < end)
                {
                    // Count padding
                    var padEnd = frameCursor;
                    while (padEnd < end && fileBytes[padEnd] == 0x00)
                    {
                        padEnd++;
                    }

                    node.Children.Add(new InspectorNode
                    {
                        Label = $"Padding",
                        StartOffset = frameCursor,
                        EndOffset = padEnd,
                        Properties = { Prop("Size", $"{padEnd - frameCursor:N0} bytes") },
                    });
                }

                break;
            }

            // Read frame identifier
            var frameId = Encoding.ASCII.GetString(fileBytes, (int)frameCursor, idLen);

            // Read frame size
            var rawSize = sizeLen == 3
                ? (fileBytes[frameCursor + idLen] << 16) | (fileBytes[frameCursor + idLen + 1] << 8) | fileBytes[frameCursor + idLen + 2]
                : ReadBE32(fileBytes, frameCursor + idLen);
            var dataSize = isSynchsafe ? DecodeSynchsafe(rawSize) : rawSize;

            var frameStart = frameCursor;
            var frameEnd = Math.Min(frameCursor + frameHeaderLen + dataSize, end);

            // Read flags if present
            var flagsValue = flagsLen == 2
                ? (fileBytes[frameCursor + idLen + sizeLen] << 8) | fileBytes[frameCursor + idLen + sizeLen + 1]
                : 0;

            // Find the matching library frame for the value summary
            var libFrame = tag.Frames.FirstOrDefault(f =>
                string.Equals(f.Identifier, frameId, StringComparison.OrdinalIgnoreCase));

            var frameNode = new InspectorNode
            {
                Label = frameId,
                StartOffset = frameStart,
                EndOffset = frameEnd,
            };

            frameNode.Properties.Add(Prop("Identifier", frameId, frameCursor, idLen));
            frameNode.Properties.Add(Prop("Data size", $"{dataSize:N0} bytes", frameCursor + idLen, sizeLen));
            if (flagsLen > 0)
            {
                frameNode.Properties.Add(Prop("Flags", $"0x{flagsValue:X4}", frameCursor + idLen + sizeLen, flagsLen));
            }

            var dataStart = frameCursor + frameHeaderLen;
            if (dataSize > 0)
            {
                frameNode.Properties.Add(Prop("Data", DescribeFrame(libFrame, dataSize), dataStart, (int)dataSize));
            }

            if (libFrame is Id3v2TextFrame text)
            {
                frameNode.Properties.Add(Prop("Encoding", text.TextEncoding.ToString(), dataStart, 1));
                frameNode.Properties.Add(Prop("Value", string.Join(" / ", text.Values)));
            }
            else if (libFrame is Id3v2UrlLinkFrame url)
            {
                frameNode.Properties.Add(Prop("URL", url.Url ?? string.Empty));
            }
            else if (libFrame is Id3v2CommentFrame comm)
            {
                frameNode.Properties.Add(Prop("Encoding", comm.TextEncoding.ToString(), dataStart, 1));
                frameNode.Properties.Add(Prop("Language", comm.Language ?? string.Empty, dataStart + 1, 3));
                frameNode.Properties.Add(Prop("Description", comm.ShortContentDescription ?? string.Empty));
                frameNode.Properties.Add(Prop("Text", comm.Text ?? string.Empty));
            }
            else if (libFrame is Id3v2AttachedPictureFrame pic)
            {
                frameNode.Properties.Add(Prop("Encoding", pic.TextEncoding.ToString(), dataStart, 1));
                frameNode.Properties.Add(Prop("Image format", pic.ImageFormat ?? string.Empty));
                frameNode.Properties.Add(Prop("Picture type", pic.PictureType.ToString()));
                frameNode.Properties.Add(Prop("Description", pic.Description ?? string.Empty));
                frameNode.Properties.Add(Prop("Picture size", $"{pic.PictureData?.Length ?? 0:N0} bytes"));
            }

            node.Children.Add(frameNode);
            frameCursor = frameEnd;
        }

        // Footer (v2.4 only)
        if (tag.UseFooter && end - frameCursor >= 10)
        {
            node.Children.Add(new InspectorNode
            {
                Label = "Footer",
                StartOffset = end - 10,
                EndOffset = end,
                Properties =
                {
                    Prop("Magic", "3DI", end - 10, 3),
                    Prop("Size", "10 bytes"),
                },
            });
        }

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// ID3v1
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildId3v1(byte[] fileBytes, long start, long end, Id3v1Tag tag)
    {
        var vLabel = tag.Version.ToString().Replace("Id3v", "v");
        var node = new InspectorNode
        {
            Label = $"ID3{vLabel}",
            StartOffset = start,
            EndOffset = end,
            IsExpanded = true,
        };

        node.Properties.Add(Prop("Magic", "TAG", start, 3));
        node.Properties.Add(Prop("Title", tag.TrackTitle ?? string.Empty, start + 3, 30));
        node.Properties.Add(Prop("Artist", tag.Artist ?? string.Empty, start + 33, 30));
        node.Properties.Add(Prop("Album", tag.AlbumTitle ?? string.Empty, start + 63, 30));
        node.Properties.Add(Prop("Year", tag.AlbumYear ?? string.Empty, start + 93, 4));
        node.Properties.Add(Prop("Comment", tag.TrackComment ?? string.Empty, start + 97, 28));
        node.Properties.Add(Prop("Zero byte", $"0x{(start + 125 < fileBytes.Length ? fileBytes[start + 125] : 0):X2}", start + 125, 1));
        node.Properties.Add(Prop("Track", tag.TrackNumber.ToString(), start + 126, 1));
        node.Properties.Add(Prop("Genre", $"{tag.Genre} ({(int)tag.Genre})", start + 127, 1));

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// APE
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildApe(byte[] fileBytes, long start, long end, ApeTag tag)
    {
        var vLabel = tag.Version.ToString().Replace("Version", "APEv");
        var node = new InspectorNode
        {
            Label = vLabel,
            StartOffset = start,
            EndOffset = end,
            IsExpanded = true,
        };

        node.Properties.Add(Prop("Version", vLabel));
        node.Properties.Add(Prop("Use header", tag.UseHeader.ToString()));
        node.Properties.Add(Prop("Use footer", tag.UseFooter.ToString()));
        node.Properties.Add(Prop("Read only", tag.IsReadOnly.ToString()));
        node.Properties.Add(Prop("Item count", tag.Items.Count().ToString()));

        // Walk APE items from raw bytes
        var cursor = start;
        if (tag.UseHeader)
        {
            node.Children.Add(new InspectorNode
            {
                Label = "Header",
                StartOffset = cursor,
                EndOffset = cursor + 32,
                Properties =
                {
                    Prop("Preamble", "APETAGEX", cursor, 8),
                    Prop("Version", ReadLE32(fileBytes, cursor + 8).ToString(), cursor + 8, 4),
                    Prop("Tag size", $"{ReadLE32(fileBytes, cursor + 12):N0} bytes", cursor + 12, 4),
                    Prop("Item count", ReadLE32(fileBytes, cursor + 16).ToString(), cursor + 16, 4),
                    Prop("Flags", $"0x{ReadLE32(fileBytes, cursor + 20):X8}", cursor + 20, 4),
                },
            });
            cursor += 32;
        }

        foreach (var item in tag.Items)
        {
            var itemData = item.Data;
            var valueSize = itemData?.Length ?? 0;
            var keyBytes = Encoding.UTF8.GetByteCount(item.Key);
            // APE item: 4 value-size + 4 flags + key + 0x00 + value
            var itemTotalSize = 4 + 4 + keyBytes + 1 + valueSize;
            var itemEnd = Math.Min(cursor + itemTotalSize, end - (tag.UseFooter ? 32 : 0));

            var itemNode = new InspectorNode
            {
                Label = item.Key,
                StartOffset = cursor,
                EndOffset = itemEnd,
            };

            itemNode.Properties.Add(Prop("Value size", $"{valueSize:N0} bytes", cursor, 4));
            itemNode.Properties.Add(Prop("Flags", $"0x{ReadLE32(fileBytes, cursor + 4):X8}", cursor + 4, 4));
            itemNode.Properties.Add(Prop("Key", item.Key, cursor + 8, keyBytes));

            var valueSummary = item switch
            {
                ApeLocatorItem loc => string.Join(" / ", loc.Values),
                ApeUtf8Item u => string.Join(" / ", u.Values),
                ApeBinaryItem b => $"<binary {b.Data?.Length ?? 0:N0} bytes>",
                _ => string.Empty,
            };
            itemNode.Properties.Add(Prop("Value", valueSummary, cursor + 8 + keyBytes + 1, valueSize));
            itemNode.Properties.Add(Prop("Type", item.ItemType.ToString()));

            node.Children.Add(itemNode);
            cursor = itemEnd;
        }

        if (tag.UseFooter)
        {
            var footerStart = end - 32;
            node.Children.Add(new InspectorNode
            {
                Label = "Footer",
                StartOffset = footerStart,
                EndOffset = end,
                Properties =
                {
                    Prop("Preamble", "APETAGEX", footerStart, 8),
                    Prop("Size", "32 bytes"),
                },
            });
        }

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// Lyrics3v2
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildLyrics3v1(long start, long end, Lyrics3Tag tag)
    {
        var node = new InspectorNode
        {
            Label = "Lyrics3",
            StartOffset = start,
            EndOffset = end,
            IsExpanded = true,
        };

        var headerLen = 11; // "LYRICSBEGIN"
        var footerLen = 9;  // "LYRICSEND"
        node.Properties.Add(Prop("Size", $"{end - start:N0} bytes"));
        node.Properties.Add(Prop("Header", "LYRICSBEGIN", start, headerLen));
        node.Properties.Add(Prop("Footer", "LYRICSEND", end - footerLen, footerLen));

        var lyricsLen = (int)(end - start - headerLen - footerLen);
        if (lyricsLen > 0)
        {
            var preview = tag.Lyrics ?? string.Empty;
            if (preview.Length > 200)
            {
                preview = preview[..200] + "...";
            }

            node.Properties.Add(Prop("Lyrics", preview, start + headerLen, lyricsLen));
            node.Properties.Add(Prop("Lyrics length", $"{lyricsLen:N0} bytes"));
        }

        return node;
    }

    private static InspectorNode BuildLyrics3v2(long start, long end, Lyrics3v2Tag tag)
    {
        var node = new InspectorNode
        {
            Label = "Lyrics3v2",
            StartOffset = start,
            EndOffset = end,
            IsExpanded = true,
        };

        node.Properties.Add(Prop("Field count", tag.Fields.Count().ToString()));
        node.Properties.Add(Prop("Size", $"{end - start:N0} bytes"));

        foreach (var field in tag.Fields)
        {
            var fieldData = field.Data;
            var value = field switch
            {
                Lyrics3v2TextField t => t.Value ?? string.Empty,
                _ when fieldData is { Length: > 0 } => Encoding.ASCII.GetString(fieldData),
                _ => string.Empty,
            };
            node.Children.Add(new InspectorNode
            {
                Label = field.Identifier,
                StartOffset = start,
                EndOffset = end,
                Properties =
                {
                    Prop("Identifier", field.Identifier),
                    Prop("Value", value),
                    Prop("Data size", $"{fieldData?.Length ?? 0:N0} bytes"),
                },
            });
        }

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// MusicMatch
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildMusicMatch(long start, long end, MusicMatchTag tag)
    {
        var node = new InspectorNode
        {
            Label = $"MusicMatch {tag.Version.Trim()}",
            StartOffset = start,
            EndOffset = end,
        };

        node.Properties.Add(Prop("Version", tag.Version.Trim()));
        node.Properties.Add(Prop("Xing encoder", tag.XingEncoderVersion ?? string.Empty));
        node.Properties.Add(Prop("Use header", tag.UseHeader.ToString()));
        node.Properties.Add(Prop("Size", $"{end - start:N0} bytes"));

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// Audio region (MPEG)
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode BuildAudioRegion(long start, long end, IAudioStream? audioStream)
    {
        var node = new InspectorNode
        {
            Label = "audio",
            StartOffset = start,
            EndOffset = end,
        };

        node.Properties.Add(Prop("Size", $"{end - start:N0} bytes"));

        if (audioStream is MpaStream mpa)
        {
            var framesInRange = mpa.Frames
                .Where(f => f.StartOffset >= start && f.EndOffset <= end)
                .ToList();

            if (framesInRange.Count > 0)
            {
                var first = framesInRange[0];
                node.Properties.Add(Prop("Format", $"{first.AudioVersion} {first.LayerVersion}"));
                node.Properties.Add(Prop("Bitrate", $"{first.Bitrate} kbps"));
                node.Properties.Add(Prop("Sample rate", $"{first.SamplingRate} Hz"));
                node.Properties.Add(Prop("Channels", first.ChannelMode.ToString()));
                node.Properties.Add(Prop("Frame count", framesInRange.Count.ToString()));
                node.Properties.Add(Prop("Duration", FormatDuration(mpa.TotalAudioLength)));

                if (mpa.VbrHeader is { } vbr)
                {
                    node.Properties.Add(Prop("VBR type", vbr.HeaderType.ToString()));
                    node.Properties.Add(Prop("VBR quality", vbr.Quality.ToString()));
                    if (vbr.LameTag is { } lame)
                    {
                        node.Properties.Add(Prop("LAME encoder", lame.EncoderVersion ?? string.Empty));
                        node.Properties.Add(Prop("LAME revision", lame.InfoTagRevision.ToString()));
                    }
                }

                var shown = Math.Min(framesInRange.Count, MaxAudioFramesInTree);
                for (var i = 0; i < shown; i++)
                {
                    var frame = framesInRange[i];
                    // EndOffset is 0 when the frame has no audio data (e.g. Xing info frame)
                    var frameEnd = frame.EndOffset > frame.StartOffset
                        ? frame.EndOffset
                        : frame.StartOffset + frame.FrameLength;
                    var frameNode = new InspectorNode
                    {
                        Label = $"Frame {i + 1}",
                        StartOffset = frame.StartOffset,
                        EndOffset = frameEnd,
                    };

                    var fh = frame.StartOffset;
                    frameNode.Properties.Add(Prop("Offset", $"0x{fh:X8}", fh, 4));
                    frameNode.Properties.Add(Prop("Length", $"{frame.FrameLength} bytes"));

                    // MPEG frame header (4 bytes): sync(11)+version(2)+layer(2)+protection(1)
                    //   +bitrate(4)+samplerate(2)+padding(1)+private(1)+channelmode(2)+...
                    var headerNode = new InspectorNode
                    {
                        Label = "Header",
                        StartOffset = fh,
                        EndOffset = fh + MpaFrame.FrameHeaderSize,
                    };
                    headerNode.Properties.Add(Prop("Size", "4 bytes", fh, 4));
                    // Byte 0: sync
                    headerNode.Properties.Add(Prop("Sync", "0xFFF", fh, 1));
                    // Byte 1: sync-tail(3) + version(2) + layer(2) + protected(1)
                    headerNode.Properties.Add(Prop("Version", frame.AudioVersion.ToString(), fh + 1, 1));
                    headerNode.Properties.Add(Prop("Layer", frame.LayerVersion.ToString(), fh + 1, 1));
                    headerNode.Properties.Add(Prop("Protected", frame.IsCrcProtected ? "CRC present" : "none", fh + 1, 1));
                    // Byte 2: bitrate(4) + samplerate(2) + padded(1) + private(1)
                    headerNode.Properties.Add(Prop("Bitrate", $"{frame.Bitrate} kbps", fh + 2, 1));
                    headerNode.Properties.Add(Prop("Sample rate", $"{frame.SamplingRate} Hz", fh + 2, 1));
                    headerNode.Properties.Add(Prop("Padded", frame.IsPadded.ToString(), fh + 2, 1));
                    headerNode.Properties.Add(Prop("Private", frame.IsPrivateBitSet.ToString(), fh + 2, 1));
                    // Byte 3: channels(2) + ext(2) + copyright(1) + original(1) + emphasis(2)
                    headerNode.Properties.Add(Prop("Channels", frame.ChannelMode.ToString(), fh + 3, 1));
                    headerNode.Properties.Add(Prop("Mode extension", frame.ModeExtension.ToString(), fh + 3, 1));
                    headerNode.Properties.Add(Prop("Copyrighted", frame.IsCopyrighted.ToString(), fh + 3, 1));
                    headerNode.Properties.Add(Prop("Original", frame.IsOriginalMedia.ToString(), fh + 3, 1));
                    headerNode.Properties.Add(Prop("Emphasis", frame.Emphasis.ToString(), fh + 3, 1));
                    frameNode.Children.Add(headerNode);

                    // Audio payload = everything after the 4-byte header (and the 2-byte CRC if present).
                    var payloadStart = fh + MpaFrame.FrameHeaderSize + (frame.IsCrcProtected ? 2 : 0);
                    var payloadLen = frameEnd - payloadStart;
                    if (payloadLen > 0)
                    {
                        if (frame.IsCrcProtected)
                        {
                            var crcNode = new InspectorNode
                            {
                                Label = "CRC",
                                StartOffset = fh + MpaFrame.FrameHeaderSize,
                                EndOffset = fh + MpaFrame.FrameHeaderSize + 2,
                            };
                            crcNode.Properties.Add(Prop("Size", "2 bytes", fh + MpaFrame.FrameHeaderSize, 2));
                            frameNode.Children.Add(crcNode);
                        }

                        var dataNode = new InspectorNode
                        {
                            Label = "Data",
                            StartOffset = payloadStart,
                            EndOffset = frameEnd,
                        };
                        dataNode.Properties.Add(Prop("Data", $"{payloadLen:N0} bytes", payloadStart, (int)payloadLen));
                        frameNode.Children.Add(dataNode);
                    }

                    if (i == 0 && mpa.VbrHeader is { } vbrH)
                    {
                        var vbrNode = new InspectorNode
                        {
                            Label = $"VBR ({vbrH.HeaderType})",
                            StartOffset = frame.StartOffset + vbrH.Offset,
                            EndOffset = Math.Min(frame.StartOffset + vbrH.Offset + 120, frameEnd),
                        };

                        var vbrStart = frame.StartOffset + vbrH.Offset;
                        vbrNode.Properties.Add(Prop("Name", vbrH.Name ?? string.Empty, vbrStart, 4));
                        vbrNode.Properties.Add(Prop("Flags", $"0x{vbrH.Flags:X8}", vbrStart + 4, 4));
                        // Xing: fields are conditional on flags; compute positions
                        var xCursor = vbrStart + 8;
                        if ((vbrH.Flags & 0x01) != 0) // FrameCountFlag
                        {
                            vbrNode.Properties.Add(Prop("Frame count", vbrH.FrameCount.ToString(), xCursor, 4));
                            xCursor += 4;
                        }
                        else
                        {
                            vbrNode.Properties.Add(Prop("Frame count", "(not present)"));
                        }

                        if ((vbrH.Flags & 0x02) != 0) // FileSizeFlag
                        {
                            vbrNode.Properties.Add(Prop("File size", $"{vbrH.FileSize:N0} bytes", xCursor, 4));
                            xCursor += 4;
                        }
                        else
                        {
                            vbrNode.Properties.Add(Prop("File size", "(not present)"));
                        }

                        if ((vbrH.Flags & 0x04) != 0) // TocFlag
                        {
                            vbrNode.Properties.Add(Prop("TOC", "100 entries", xCursor, 100));
                            xCursor += 100;
                        }

                        if ((vbrH.Flags & 0x08) != 0) // VbrScaleFlag
                        {
                            vbrNode.Properties.Add(Prop("Quality", vbrH.Quality.ToString(), xCursor, 4));
                        }
                        else
                        {
                            vbrNode.Properties.Add(Prop("Quality", "(not present)"));
                        }

                        vbrNode.Properties.Add(Prop("Type", vbrH.HeaderType.ToString()));

                        if (vbrH.LameTag is { } lame)
                        {
                            var lameNode = new InspectorNode
                            {
                                Label = "LAME tag",
                                StartOffset = frame.StartOffset + vbrH.Offset + 120,
                                EndOffset = frameEnd,
                            };

                            var ls = lameNode.StartOffset;
                            // LAME tag layout (36 bytes): encoder(9) rev+vbr(1) lowpass(1) replaygain(8) flags(1) bitrate(1) delays(3) misc(1) gain(1) preset(2) musiclen(4) musiccrc(2) infocrc(2)
                            lameNode.Properties.Add(Prop("Encoder", lame.EncoderVersion ?? string.Empty, ls, 9));
                            lameNode.Properties.Add(Prop("Revision", lame.InfoTagRevision.ToString(), ls + 9, 1));
                            lameNode.Properties.Add(Prop("VBR method", $"{lame.VbrMethod} ({lame.VbrMethodName})", ls + 9, 1));
                            lameNode.Properties.Add(Prop("Lowpass", $"{lame.LowpassFilterValue} Hz", ls + 10, 1));
                            lameNode.Properties.Add(Prop("Peak amplitude", lame.PeakSignalAmplitude.ToString("F6"), ls + 11, 4));
                            lameNode.Properties.Add(Prop("Radio replay gain", $"0x{lame.RadioReplayGain:X4}", ls + 15, 2));
                            lameNode.Properties.Add(Prop("Audiophile replay gain", $"0x{lame.AudiophileReplayGain:X4}", ls + 17, 2));
                            lameNode.Properties.Add(Prop("Encoding flags", $"0x{lame.EncodingFlags:X2}", ls + 19, 1));
                            lameNode.Properties.Add(Prop("ATH type", lame.AthType.ToString(), ls + 19, 1));
                            lameNode.Properties.Add(Prop("Bitrate", $"{lame.BitRate} kbps", ls + 20, 1));
                            lameNode.Properties.Add(Prop("Encoder delay", $"{lame.EncoderDelaySamples} / {lame.EncoderDelayPaddingSamples} samples", ls + 21, 3));
                            lameNode.Properties.Add(Prop("Misc", $"0x{lame.Misc:X2}", ls + 24, 1));
                            lameNode.Properties.Add(Prop("MP3 gain", $"{lame.Mp3Gain} dB", ls + 25, 1));
                            lameNode.Properties.Add(Prop("Preset", $"0x{lame.PresetSurroundInfo:X4}", ls + 26, 2));
                            lameNode.Properties.Add(Prop("Music length", $"{lame.MusicLength:N0} bytes", ls + 28, 4));
                            lameNode.Properties.Add(Prop("Music CRC", $"0x{lame.MusicCrc:X4}", ls + 32, 2));
                            lameNode.Properties.Add(Prop("Info CRC", $"0x{lame.InfoTagCrc:X4}", ls + 34, 2));

                            vbrNode.Children.Add(lameNode);
                        }

                        frameNode.Children.Add(vbrNode);
                    }

                    node.Children.Add(frameNode);
                }

                if (framesInRange.Count > MaxAudioFramesInTree)
                {
                    node.Children.Add(SimpleNode(
                        $"... {framesInRange.Count - MaxAudioFramesInTree:N0} more frames",
                        framesInRange[shown].StartOffset,
                        framesInRange.Last().EndOffset));
                }
            }
        }

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// FLAC
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildFlacTree(InspectorNode root, byte[] fileBytes, FlacStream flac)
    {
        root.Children.Add(new InspectorNode
        {
            Label = "fLaC marker",
            StartOffset = 0,
            EndOffset = 4,
            Properties = { Prop("Magic", "fLaC", 0, 4) },
        });

        long cursor = 4;
        foreach (var block in flac.MetadataBlocks)
        {
            var blockData = block.Data;
            var blockSize = 4L + (blockData?.Length ?? 0);
            var blockNode = new InspectorNode
            {
                Label = block.BlockType.ToString(),
                StartOffset = cursor,
                EndOffset = cursor + blockSize,
                Payload = block,
            };

            blockNode.Properties.Add(Prop("Block type", block.BlockType.ToString(), cursor, 1));
            blockNode.Properties.Add(Prop("Data size", $"{blockData?.Length ?? 0:N0} bytes", cursor + 1, 3));
            blockNode.Properties.Add(Prop("Is last block", block.IsLastBlock.ToString()));

            if (block is FlacStreamInfoMetadataBlock info)
            {
                // StreamInfo data layout: minBlock(2) maxBlock(2) minFrame(3) maxFrame(3) sampleRate+ch+bps+samples(8) MD5(16)
                var d = cursor + 4; // data starts after 4-byte block header
                blockNode.Properties.Add(Prop("Min block size", string.Empty, d, 2));
                blockNode.Properties.Add(Prop("Max block size", string.Empty, d + 2, 2));
                blockNode.Properties.Add(Prop("Min frame size", string.Empty, d + 4, 3));
                blockNode.Properties.Add(Prop("Max frame size", string.Empty, d + 7, 3));
                blockNode.Properties.Add(Prop("Sample rate", $"{info.SampleRate} Hz", d + 10, 3));
                blockNode.Properties.Add(Prop("Channels", info.Channels.ToString(), d + 12, 1));
                blockNode.Properties.Add(Prop("Bits/sample", info.BitsPerSample.ToString(), d + 12, 2));
                blockNode.Properties.Add(Prop("Total samples", info.TotalSamples.ToString(), d + 13, 5));
                var md5Hex = info.MD5 != null && info.MD5.Length == 16 ? Convert.ToHexString(info.MD5) : string.Empty;
                blockNode.Properties.Add(Prop("MD5 (unencoded PCM)", md5Hex, d + 18, 16));
            }
            else if (block is FlacVorbisCommentsMetadataBlock vc && vc.VorbisComments != null)
            {
                blockNode.Properties.Add(Prop("Vendor", vc.VorbisComments.Vendor ?? string.Empty));
                blockNode.Properties.Add(Prop("Comment count", vc.VorbisComments.Comments.Count.ToString()));
                foreach (var comment in vc.VorbisComments.Comments)
                {
                    blockNode.Children.Add(new InspectorNode
                    {
                        Label = comment.Name ?? "?",
                        StartOffset = cursor,
                        EndOffset = cursor + blockSize,
                        Properties =
                        {
                            Prop("Name", comment.Name ?? string.Empty),
                            Prop("Value", comment.Value ?? string.Empty),
                        },
                    });
                }
            }
            else if (block is FlacPictureMetadataBlock pic)
            {
                // Picture data layout: type(4) + mimeLen(4) + mime + descLen(4) + desc + w(4) + h(4) + depth(4) + colors(4) + dataLen(4) + data
                var pd = cursor + 4;
                blockNode.Properties.Add(Prop("Picture type", pic.PictureType.ToString(), pd, 4));
                var mimeLen = pic.MimeType?.Length ?? 0;
                blockNode.Properties.Add(Prop("MIME", pic.MimeType ?? string.Empty, pd + 8, mimeLen));
                var descOffset = pd + 8 + mimeLen + 4;
                var descLen = pic.Description?.Length ?? 0;
                blockNode.Properties.Add(Prop("Description", pic.Description ?? string.Empty, descOffset, descLen));
                var dimsOffset = descOffset + descLen;
                blockNode.Properties.Add(Prop("Width", pic.Width.ToString(), dimsOffset, 4));
                blockNode.Properties.Add(Prop("Height", pic.Height.ToString(), dimsOffset + 4, 4));
                blockNode.Properties.Add(Prop("Color depth", pic.ColorDepth.ToString(), dimsOffset + 8, 4));
                blockNode.Properties.Add(Prop("Color count", pic.ColorCount.ToString(), dimsOffset + 12, 4));
                blockNode.Properties.Add(Prop("Picture size", $"{pic.PictureData?.Length ?? 0:N0} bytes", dimsOffset + 16, 4));
            }

            root.Children.Add(blockNode);
            cursor += blockSize;
        }

        if (flac.StartOffset > 0 && flac.StartOffset < fileBytes.Length)
        {
            var audioNode = new InspectorNode
            {
                Label = "audio frames",
                StartOffset = flac.StartOffset,
                EndOffset = fileBytes.Length,
            };

            audioNode.Properties.Add(Prop("Size", $"{fileBytes.Length - flac.StartOffset:N0} bytes"));
            audioNode.Properties.Add(Prop("Frame count", flac.Frames.Count().ToString()));
            root.Children.Add(audioNode);
        }
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// Helpers
    ////------------------------------------------------------------------------------------------------------------------------------

    private static InspectorNode SimpleNode(string label, long start, long end)
    {
        return new InspectorNode
        {
            Label = label,
            StartOffset = start,
            EndOffset = end,
            Properties = { Prop("Size", $"{end - start:N0} bytes") },
        };
    }

    private static InspectorProperty Prop(string name, string value, long? hlStart = null, int? hlLen = null)
    {
        return new InspectorProperty
        {
            Name = name,
            Value = value,
            HighlightStart = hlStart,
            HighlightLength = hlLen,
        };
    }

    private static string DescribeFrame(Id3v2Frame? frame, long dataSize)
    {
        return frame switch
        {
            null => $"<{dataSize:N0} bytes>",
            Id3v2TextFrame text => string.Join(" / ", text.Values),
            Id3v2UrlLinkFrame url => url.Url ?? string.Empty,
            Id3v2CommentFrame comm => comm.Text ?? string.Empty,
            Id3v2AttachedPictureFrame pic => $"{pic.ImageFormat} {pic.PictureType} {pic.PictureData?.Length ?? 0:N0} bytes",
            _ => $"<{dataSize:N0} bytes>",
        };
    }

    private static int ReadBE32(byte[] data, long offset)
    {
        return offset + 4 > data.Length
            ? 0
            : (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static int ReadLE32(byte[] data, long offset)
    {
        return offset + 4 > data.Length
            ? 0
            : data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static int DecodeSynchsafe(int value)
    {
        return ((value >> 3) & 0x0FE00000)
             | ((value >> 2) & 0x001FC000)
             | ((value >> 1) & 0x00003F80)
             | (value & 0x0000007F);
    }

    private static string FormatDuration(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// DSF / DFF
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildDsfTree(InspectorNode root, DsfStream dsf)
    {
        root.Properties.Add(Prop("Format", "DSF / DSD"));
        root.Properties.Add(Prop("Sample rate", $"{dsf.SampleRate:N0} Hz"));
        root.Properties.Add(Prop("Channels", dsf.Channels.ToString()));
        root.Properties.Add(Prop("Bits/sample", dsf.BitsPerSample.ToString()));
        root.Properties.Add(Prop("Sample count", dsf.SampleCount.ToString("N0")));
        root.Properties.Add(Prop("Metadata pointer", $"0x{dsf.MetadataPointer:X16}"));

        foreach (var chunk in dsf.Chunks)
        {
            root.Children.Add(new InspectorNode
            {
                Label = $"{chunk.Id} ({chunk.EndOffset - chunk.StartOffset:N0} B)",
                StartOffset = chunk.StartOffset,
                EndOffset = chunk.EndOffset,
            });
        }

        if (dsf.EmbeddedId3v2 is { } dsfId3)
        {
            root.Children.Add(BuildEmbeddedId3Node(dsfId3));
        }
    }

    private static void BuildDffTree(InspectorNode root, DffStream dff)
    {
        root.Properties.Add(Prop("Format", "DFF / DSDIFF"));
        root.Properties.Add(Prop("Sample rate", $"{dff.SampleRate:N0} Hz"));
        root.Properties.Add(Prop("Channels", dff.Channels.ToString()));
        root.Properties.Add(Prop("Sample count", dff.SampleCount.ToString("N0")));
        root.Properties.Add(Prop("Chunk count", dff.Chunks.Count.ToString()));

        foreach (var chunk in dff.Chunks)
        {
            root.Children.Add(new InspectorNode
            {
                Label = $"{chunk.Id} ({chunk.EndOffset - chunk.StartOffset:N0} B)",
                StartOffset = chunk.StartOffset,
                EndOffset = chunk.EndOffset,
            });
        }

        if (dff.EmbeddedId3v2 is { } dffId3)
        {
            root.Children.Add(BuildEmbeddedId3Node(dffId3));
        }
    }

    private static InspectorNode BuildEmbeddedId3Node(Id3v2Tag tag)
    {
        var node = new InspectorNode { Label = "Embedded ID3v2" };
        node.Properties.Add(Prop("Version", tag.Version.ToString()));
        node.Properties.Add(Prop("Frame count", tag.Frames.Count().ToString()));
        foreach (var frame in tag.Frames)
        {
            node.Children.Add(new InspectorNode { Label = frame.Identifier ?? "?" });
        }

        return node;
    }

    ////------------------------------------------------------------------------------------------------------------------------------
    //// MP4 / M4A
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildMp4Tree(InspectorNode root, Mp4Stream mp4)
    {
        root.Properties.Add(Prop("Format", "MP4 / ISO base media"));
        root.Properties.Add(Prop("Top-level boxes", mp4.Boxes.Count.ToString()));
        root.Properties.Add(Prop("Duration (ms)", mp4.TotalAudioLength.ToString("N0")));

        foreach (var box in mp4.Boxes)
        {
            root.Children.Add(new InspectorNode
            {
                Label = $"{box.Type} ({box.EndOffset - box.StartOffset:N0} B)",
                StartOffset = box.StartOffset,
                EndOffset = box.EndOffset,
            });
        }

        var tag = mp4.Tag;
        if (tag.Items.Count > 0)
        {
            var ilst = new InspectorNode { Label = $"ilst items ({tag.Items.Count})" };
            foreach (var item in tag.Items)
            {
                ilst.Children.Add(new InspectorNode
                {
                    Label = $"{item.AtomType}: {Mp4ItemDisplay(item)}",
                });
            }

            root.Children.Add(ilst);
        }
    }

    private static string Mp4ItemDisplay(Mp4MetaItem item) => item.AsUtf8String() switch
    {
        { } s when !string.IsNullOrEmpty(s) => s,
        _ => $"{item.DataType}, {item.Payload.Length:N0} B",
    };

    ////------------------------------------------------------------------------------------------------------------------------------
    //// ASF / WMA
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildAsfTree(InspectorNode root, AsfStream asf)
    {
        root.Properties.Add(Prop("Format", "ASF / WMA"));
        root.Properties.Add(Prop("Header objects", asf.Objects.Count.ToString()));
        root.Properties.Add(Prop("Duration (ms)", asf.TotalAudioLength.ToString("N0")));

        foreach (var obj in asf.Objects)
        {
            root.Children.Add(new InspectorNode
            {
                Label = $"{AsfGuidLabel(obj.Id)} ({obj.EndOffset - obj.StartOffset:N0} B)",
                StartOffset = obj.StartOffset,
                EndOffset = obj.EndOffset,
            });
        }
    }

    private static string AsfGuidLabel(Guid g) =>
        g == AsfMetadataTag.HeaderObjectGuid ? "Header"
        : g == AsfMetadataTag.FilePropertiesObjectGuid ? "File Properties"
        : g == AsfMetadataTag.ContentDescriptionObjectGuid ? "Content Description"
        : g == AsfMetadataTag.ExtendedContentDescriptionObjectGuid ? "Extended Content Description"
        : g == AsfMetadataTag.HeaderExtensionObjectGuid ? "Header Extension"
        : g == AsfMetadataTag.MetadataObjectGuid ? "Metadata"
        : g == AsfMetadataTag.MetadataLibraryObjectGuid ? "Metadata Library"
        : g.ToString();

    ////------------------------------------------------------------------------------------------------------------------------------
    //// Matroska / WebM
    ////------------------------------------------------------------------------------------------------------------------------------

    private static void BuildMatroskaTree(InspectorNode root, MatroskaStream mkv)
    {
        root.Properties.Add(Prop("Format", $"Matroska / {mkv.DocType}"));
        root.Properties.Add(Prop("Tag entries", mkv.Tag.Entries.Count.ToString()));
        root.Properties.Add(Prop("Duration (ms)", mkv.TotalAudioLength.ToString("N0")));

        foreach (var entry in mkv.Tag.Entries)
        {
            var node = new InspectorNode
            {
                Label = $"Tag (target {entry.Targets.TargetTypeValue})",
            };
            foreach (var st in entry.SimpleTags)
            {
                node.Children.Add(new InspectorNode
                {
                    Label = $"{st.Name}: {st.Value ?? string.Empty}",
                });
            }

            root.Children.Add(node);
        }
    }
}
