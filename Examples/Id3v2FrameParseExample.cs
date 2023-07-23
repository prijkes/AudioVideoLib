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
    public static class Id3v2FrameParseExample
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

            foreach (IAudioTagOffset tagOffset in audioTags.Where(t => t.AudioTag is Id3v2Tag))
            {
                Id3v2Tag tag = tagOffset.AudioTag as Id3v2Tag;
                if (tag == null)
                    continue;

                Id3v2ExperimentalTestFrame testFrame = tag.GetFrame<Id3v2ExperimentalTestFrame>();
                if (testFrame == null)
                {
                    testFrame = new Id3v2ExperimentalTestFrame(tag.Version)
                                    {
                                        TextEncodingType = Id3v2FrameEncodingType.UTF16BigEndian,
                                        TaggingLibraryUsed = "This one",
                                        TaggingLibraryAuthor = "Me",
                                        TaggingLibraryWebsite = "http://www.google.com/",
                                        TaggingLibrarySupportsFrame = true,
                                        DateOfTag = DateTime.MaxValue
                                    };
                    tag.SetFrame(testFrame);
                }
            }
        }

        // Event called when an tag is being created and asked to try to read a tag.
        // This event can be used to add custom event handlers to a tag, before reading a stream.
        private static void AudioTagParse(object sender, AudioTagParseEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            // If the current tag is an Id3v2 tag, add our custom frame parser.
            if (e.AudioTagReader is Id3v2TagReader)
                (e.AudioTagReader as Id3v2TagReader).FrameParse += Id3v2FrameParse;
        }

        // Event called when the Id3v2 tag reads a frame.
        // This event can be used to parse your own custom frames, as shown here.
        private static void Id3v2FrameParse(object sender, Id3v2FrameParseEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            if (((e.Frame.Version < Id3v2Version.Id3v230) && String.Equals(e.Frame.Identifier, "XFT", StringComparison.OrdinalIgnoreCase))
                || ((e.Frame.Version >= Id3v2Version.Id3v230) && String.Equals(e.Frame.Identifier, "XFTS", StringComparison.OrdinalIgnoreCase)))
            {
                byte[] frameData = e.Frame.ToByteArray();
                e.Frame = Id3v2Frame.ReadFromStream<Id3v2ExperimentalTestFrame>(e.Frame.Version, new MemoryStream(frameData), frameData.Length);
            }
        }
    }
}
