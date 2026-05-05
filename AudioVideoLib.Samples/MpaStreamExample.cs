using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;
using AudioVideoLib.IO;

namespace AudioVideoLibExamples
{
    /// <summary>
    /// 
    /// </summary>
    public class MpaStreamExample
    {
        public static void ParseStream(Stream stream)
        {
            using var mpaStream = new MpaStream { MaxFrameSpacingLength = (int)stream.Length };
            mpaStream.ReadStream(stream);
            Console.WriteLine("[*] Found {0} frames", mpaStream.Frames.Count());
            Console.WriteLine("[*] Bytes per second: {0}", mpaStream.BytesPerSecond);
            Console.WriteLine("[*] Total audio length: {0}", new TimeSpan(mpaStream.TotalDuration * 10000));
            Console.WriteLine("[*] Total audio size: {0}kb", mpaStream.TotalMediaSize / 1024);
            foreach (MpaFrame frame in mpaStream.Frames)
            {
            }
        }
    }
}
