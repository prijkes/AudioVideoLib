namespace AudioVideoLib.Studio.Editors.Id3v2;

public enum Id3v2FrameCategory
{
    TextFrames, UrlFrames, Identification, CommentsAndLyrics, TimingAndSync, People,
    AudioAdjustment, CountersAndRatings, Attachments, CommerceAndRights,
    EncryptionAndCompression, Containers, System, Experimental,
}

internal static class Id3v2FrameCategoryDisplay
{
    public static string ToDisplay(this Id3v2FrameCategory c) => c switch
    {
        Id3v2FrameCategory.TextFrames                 => "Text frame",
        Id3v2FrameCategory.UrlFrames                  => "URL frame",
        Id3v2FrameCategory.Identification             => "Identification",
        Id3v2FrameCategory.CommentsAndLyrics          => "Comments & lyrics",
        Id3v2FrameCategory.TimingAndSync              => "Timing & sync",
        Id3v2FrameCategory.People                     => "People",
        Id3v2FrameCategory.AudioAdjustment            => "Audio adjustment",
        Id3v2FrameCategory.CountersAndRatings         => "Counters & ratings",
        Id3v2FrameCategory.Attachments                => "Attachments",
        Id3v2FrameCategory.CommerceAndRights          => "Commerce & rights",
        Id3v2FrameCategory.EncryptionAndCompression   => "Encryption & compression",
        Id3v2FrameCategory.Containers                 => "Containers",
        Id3v2FrameCategory.System                     => "System",
        Id3v2FrameCategory.Experimental               => "Experimental",
        _ => c.ToString(),
    };
}
