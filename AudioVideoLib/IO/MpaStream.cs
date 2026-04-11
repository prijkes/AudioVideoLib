/*
 * Date: 2010-02-12
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://www.datavoyage.com/mpgscript/mpeghdr.htm
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AudioVideoLib.Formats;

namespace AudioVideoLib.IO
{
    /// <summary>
    /// The stream containing MPEG Audio <see cref="MpaFrame"/>s.
    /// </summary>
    public sealed class MpaStream : IAudioStream
    {
        private readonly List<MpaFrame> _frames = new List<MpaFrame>();

        // Max length of spacing, in bytes, between 2 frames. If there is spacing between frames, this means that a frame is corrupted.
        private int _maxFrameSpacingLength = 2048;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <inheritdoc/>
        public long StartOffset
        {
            get
            {
                return _frames.Any() ? _frames.First().StartOffset : 0;
            }
        }

        /// <inheritdoc/>
        public long EndOffset
        {
            get
            {
                return _frames.Any() ? _frames.Last().EndOffset : 0;
            }
        }

        /// <summary>
        /// Gets the <see cref="VbrHeader"/> found in the first frame.
        /// </summary>
        /// <value>
        /// The <see cref="VbrHeader"/>, this can be one of the types defined in <see cref="VbrHeaderType"/>.
        /// </value>
        public VbrHeader VbrHeader
        {
            get
            {
                MpaFrame firstFrame = _frames.FirstOrDefault();
                return ((firstFrame != null) && (firstFrame.AudioData != null)) ? VbrHeader.FindHeader(firstFrame) : null;
            }
        }

        /// <summary>
        /// Gets the audio frames in the stream.
        /// </summary>
        /// <value>
        /// A list of <see cref="MpaFrame"/>s in the stream.
        /// </value>
        public IEnumerable<MpaFrame> Frames
        {
            get { return _frames.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the total length of audio in milliseconds.
        /// </summary>
        /// <value>
        /// The total length of audio, in milliseconds.
        /// </value>
        /// <remarks>
        /// It calculates the audio length by adding up the audio length of each frame.
        /// Note that this method is slow but accurate.
        /// A faster but less accurate method is to calculate the total file size in bits per second, and dividing this by the bitrate.
        /// </remarks>
        /// <example>
        /// // This method does a rough estimation of audio length of a CBR audio file.
        /// public long EstimateFrameLengthCBR()
        /// {
        ///     FileInfo file = new FileInfo("audio.mp3");
        ///     using (FileStream fileStream = file.OpenRead())
        ///     {
        ///         MPAStream mpaStream = MPAStream.ReadStream(fileStream);
        ///         <para/>
        ///         // You can only use this method on CBR files, not on those that have a VBR header included.
        ///         if ((mpaStream.vbrHeader != null) || (mpaStream.FirstFrame == null))
        ///             return 0;
        ///         <para/>
        ///         MPAFrame frame = mpaStream.FirstFrame;
        ///         <para/>
        ///         // Calculate the audio size, in bits per second.
        ///         long totalAudioSize = TotalAudioSize * 8;
        ///         <para/>
        ///         // Return the divided total audio size by the bitrate of the first frame,
        ///         // this results in a roughly estimated audio length in milliseconds.
        ///         return Convert.ToInt64(totalAudioSize / (float)frame.Bitrate);
        ///     }
        /// }
        /// </example>
        /// <para/>
        /// <example>
        /// // This method does a rough estimation of audio length of a VBR audio file.
        /// public long EstimateFrameLengthVBR()
        /// {
        ///     FileInfo file = new FileInfo("audio.mp3");
        ///     using (FileStream fileStream = file.OpenRead())
        ///     {
        ///         MPAStream mpaStream = MPAStream.ReadStream(fileStream);
        ///         <para/>
        ///         // You can only use this method on VBR files, not on CBR files because those don't have a VBR header.
        ///         if ((mpaStream.vbrHeader == null) || (mpaStream.FirstFrame == null))
        ///             return 0;
        ///         <para/>
        ///         VBRHeader vbrHeader = mpaStream.VbrHeader;
        ///         <para/>
        ///         // Store the frame count from the VBR header.
        ///         long frameCount = vbrHeader.FrameCount;
        ///         <para/>
        ///         // If the VBRHeader is a XING header, we need to add one frame to the frame count,
        ///         // as the frame count in the XING header excludes it's own frame.
        ///         if (vbrHeader.HeaderType == VBRHeaderType.XING)
        ///             frameCount++;
        ///         <para/>
        ///         MPAFrame firstFrame = mpaStream.FirstFrame;
        ///         <para/>
        ///         // Return the length of the first frame multiplied with the total amount of frames in the stream,
        ///         // as indicated by the VBR header.
        ///         return frameCount * firstFrame.AudioLength;
        ///     }
        /// }
        /// </example>
        public long TotalAudioLength
        {
            get
            {
                return Frames.Sum(f => f.AudioLength);
            }
        }

        /// <inheritdoc/>
        public long TotalAudioSize
        {
            get
            {
                return Frames.Sum(f => f.FrameLength);
            }
        }

        /// <inheritdoc/>
        public int MaxFrameSpacingLength
        {
            get
            {
                return _maxFrameSpacingLength;
            }

            set
            {
                _maxFrameSpacingLength = value;
            }
        }

        /// <summary>
        /// Gets the bytes per second over the whole <see cref="MpaStream"/> (all <see cref="MpaFrame"/>s).
        /// </summary>
        /// <value>
        /// The bytes per second rate over the whole <see cref="MpaStream"/>.
        /// </value>
        public int BytesPerSecond
        {
            get
            {
                MpaFrame firstFrame = Frames.FirstOrDefault();
                if (firstFrame == null)
                    return 0;

                // Try to guess the bitrate of the whole file as good as possible.
                // In case of a VBR header, we know the bitrate, if at least the number of frames is known.
                if ((VbrHeader != null) && (VbrHeader.FrameCount != 0))
                {
                    long fileSize = VbrHeader.FileSize;
                    ////if (fileSize == 0)
                    ////vbrHeader.fileSize = Length;

                    long totalFrameSize = VbrHeader.FrameCount * firstFrame.FrameSize;
                    long averageSamplingRate = totalFrameSize / firstFrame.SamplingRate;
                    if (averageSamplingRate == 0)
                        return 0;

                    long bytesPerSec = fileSize / averageSamplingRate;
                    return (int)bytesPerSec;
                        //// (fileSize / ((firstFrame.GetAudioLength() / 1000) * vbrHeader.GetFrameCount()));
                }

                // Otherwise, we have to guess it.
                // Find a frame with a bitrate higher than 48kbit.
                // Use the first frame if no frame with a bitrate higher than 48kbit was found.
                return (Frames.FirstOrDefault(f => f.Bitrate > 48) ?? firstFrame).BitrateInBytesPerSecond;
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Reads the MPEG Audio stream from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>
        /// true if one or more frames are read from the stream; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown if stream is null.</exception>
        public bool ReadStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long streamLength = stream.Length;
            long startPosition = stream.Position;
            long spacing = 0;
            bool foundFirstFrame = false;
            MpaFrame prevFrame = null;
            while ((stream.Position + MpaFrame.FrameHeaderSize <= streamLength) && (spacing < MaxFrameSpacingLength))
            {
                MpaFrame frame = MpaFrame.ReadFrame(stream);
                if (frame != null)
                {
                    spacing = 0;
                    startPosition = stream.Position;
                    if (!foundFirstFrame)
                    {
                        if (prevFrame == null)
                        {
                            prevFrame = frame;
                            continue;
                        }

                        if (!IsValidFirstFrame(prevFrame, frame))
                        {
                            prevFrame = frame;
                            continue;
                        }
                        _frames.Add(prevFrame);
                        foundFirstFrame = true;
                    }
                    _frames.Add(frame);
                    continue;
                }
                stream.Position = ++startPosition;
                spacing++;
            }
            return _frames.Any();
        }

        /// <inheritdoc />
        public byte[] ToByteArray()
        {
            using (StreamBuffer sb = new StreamBuffer())
            {
                for (int i = 0, l = Frames.Count(); i < l; i++)
                {
                    MpaFrame frame = Frames.ElementAt(i);
                    if (frame.AudioData != null)
                        sb.Write(frame.ToByteArray());
                }
                return sb.ToByteArray();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static bool IsValidFirstFrame(MpaFrame firstFrame, MpaFrame secondFrame)
        {
            // The free bitrate must remain constant
            // We need to calculate the bitrate manually to see if its constant
            // (If VBR is used, the bitrate can be different for each frame, this check is only for free bitrate frames)
            if (secondFrame.Bitrate == 0)
                return (firstFrame.Bitrate == secondFrame.Bitrate);

            // See if the next frame is directly after the current frame
            // FrameLength is calculated using the bitrate as a calculation factor
            // This will fail if free bitrate is used as its not implemented yet
            ////if ((curFrame.GetOffset() + curFrame.GetFrameLength() != nextFrame.GetOffset()) && (curFrameHeader.BitrateIndex != 0))
            ////{
            ////curFrame = nextFrame;
            ////nextFrame = GetNextFrame(curFrame);
            ////continue;
            ////}

            // Check the values of the next header, some have to be the same as the previous:
            // ** version
            // ** layer
            // ** samples (per sec)
            // ** channel type (can only be mono or stereo)
            // ** emphasis
            return ((secondFrame.AudioVersion == firstFrame.AudioVersion) && (secondFrame.LayerVersion == firstFrame.LayerVersion)
                    && (secondFrame.SamplingRate == firstFrame.SamplingRate) && (secondFrame.IsMono == firstFrame.IsMono)
                    && (secondFrame.Emphasis == firstFrame.Emphasis));
        }
    }
}
