/*
 * Date: 2012-12-01
 * Sources used: 
 */

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// This class is an example on how to use the <see cref="Id3v2Tag.FrameParsed"/> event.
    /// It implements the <see cref="Id3v2iTunesNormalizationFrame"/>, which is based on the <see cref="Id3v2CommentFrame"/>, 
    /// by parsing the <see cref="Id3v2CommentFrame"/> when it's been read, 
    /// and setting the <see cref="Id3v2iTunesNormalizationFrame"/> as frame parsed in the <see cref="Id3v2FrameParsedEventArgs"/>.
    /// </summary>
    public static class Id3v2FrameParsedExample
    {
        public static void ParseStream(Stream stream)
        {
            AudioTags audioTags = new AudioTags();
            audioTags.AudioTagParse += AudioTagParse;
            try
            {
                audioTags.ReadTags(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Exception thrown: {0}", ex);
                return;
            }

            // Display all found iTunes normalization frames.
            foreach (Id3v2iTunesNormalizationFrame normalizationFrame in
                audioTags.OfType<Id3v2Tag>().Select(audioTag => audioTag.GetFrame<Id3v2iTunesNormalizationFrame>()))
            {
                Console.WriteLine("[+] iTunes normalization frame found in an Id3v2 tag.");
                Console.WriteLine("[+] Volume adjustment1 left: {0}", normalizationFrame.VolumeAdjustment1Left);
                Console.WriteLine("[+] Volume adjustment1 right: {0}", normalizationFrame.VolumeAdjustment1Right);
                Console.WriteLine("[+] Volume adjustment2 left: {0}", normalizationFrame.VolumeAdjustment2Left);
                Console.WriteLine("[+] Volume adjustment2 right: {0}", normalizationFrame.VolumeAdjustment2Right);
                Console.WriteLine("[+] Unknown1 left: {0}", normalizationFrame.Unknown1Left);
                Console.WriteLine("[+] Unknown1 right: {0}", normalizationFrame.Unknown1Right);
                Console.WriteLine("[+] Peak value left: {0}", normalizationFrame.PeakValueLeft);
                Console.WriteLine("[+] Peak value right: {0}", normalizationFrame.PeakValueRight);
                Console.WriteLine("[+] Unknown2 left: {0}", normalizationFrame.Unknown2Left);
                Console.WriteLine("[+] Unknown2 right: {0}", normalizationFrame.Unknown2Right);
                Console.WriteLine();
            }
        }

        // Event called when an tag is being created and asked to try to read a tag.
        // This event can be used to add custom event handlers to a tag, before reading a stream.
        private static void AudioTagParse(object sender, AudioTagParseEventArgs e)
        {
            // If the current tag is an Id3v2 tag, add our custom frame parser.
            if (e.AudioTagReader is Id3v2TagReader)
                (e.AudioTagReader as Id3v2TagReader).FrameParsed += Id3v2FrameParsed;
        }

        // Event called when the Id3v2 tag has read a frame.
        // This event can be used to modify a read frame, as we do here, to further parse known frames.
        private static void Id3v2FrameParsed(object sender, Id3v2FrameParsedEventArgs e)
        {
            // See if the frame is a comment frame.
            if (!(e.Frame is Id3v2CommentFrame))
                return;

            // Safe-cast the frame to a comment frame.
            Id3v2CommentFrame frame = e.Frame as Id3v2CommentFrame;

            // See if the frame is an iTunes normalization frame.
            if (!String.Equals(frame.ShortContentDescription, "iTunNORM", StringComparison.OrdinalIgnoreCase))
                return;

            byte[] frameData = e.Frame.ToByteArray();

            // Parse the frame as an iTunes normalization frame.
            Id3v2iTunesNormalizationFrame normalizationFrame = Id3v2Frame.ReadFromStream<Id3v2iTunesNormalizationFrame>(
                e.Frame.Version,
                new MemoryStream(frameData),
                frameData.Length);

            // Set the new normalization frame as the new frame; this will replace the comment frame.
            e.Frame = normalizationFrame;
        }
    }
}
