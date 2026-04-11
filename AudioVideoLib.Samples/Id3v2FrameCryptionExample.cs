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
    public static class Id3v2FrameCryptionExample
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

            // For each Id3v2 tag found
            foreach (IAudioTagOffset tagOffset in audioTags.Where(t => t.AudioTag is Id3v2Tag))
            {
                Id3v2Tag tag = tagOffset.AudioTag as Id3v2Tag;
                if (tag == null)
                    continue;

                // For each frame in the Id3v2 tag
                foreach (Id3v2Frame frame in tag.Frames)
                {
                    // Set encryption
                    frame.UseEncryption = true;

                    // Set the cryptor
                    frame.Cryptor = new Id3v2FrameCryption();
                }

                // Write the tag to a byte array into a memory stream (this will trigger encrypting each frame).
                MemoryStream ms = new MemoryStream(tag.ToByteArray());

                // Read the tag again from the memory stream (this will trigger decrypting each frame).
                Id3v2TagReader tagReader = new Id3v2TagReader();
                IAudioTagOffset decryptedTagOffset = tagReader.ReadFromStream(ms, tagOffset.TagOrigin);
                if (decryptedTagOffset != null)
                {
                    Id3v2Tag decryptedTag = decryptedTagOffset.AudioTag as Id3v2Tag;
                    if (decryptedTag != null)
                        Console.WriteLine("[*] Frames use encryption: {0}", decryptedTag.Frames.All(f => f.UseEncryption));
                }
            }
        }

        // Event called when an tag is being created and asked to try to read a tag.
        // This event can be used to add an cryptor instance to the tag, before reading a stream.
        private static void AudioTagParse(object sender, AudioTagParseEventArgs e)
        {
            // If the current tag is an Id3v2 tag, add our custom frame parser.
            if (e.AudioTagReader is Id3v2TagReader)
                (e.AudioTagReader as Id3v2TagReader).FrameParse += Id3v2FrameParse;
        }

        private static void Id3v2FrameParse(object sender, Id3v2FrameParseEventArgs e)
        {
            // If the frame is encrypted, add the frame cryptor and decrypt the frame.
            if (e.Frame.UseEncryption)
            {
                e.Frame.Cryptor = new Id3v2FrameCryption();
                e.Frame.Decrypt();
            }
        }
    }
}
