/*
 * Date: 2010-05-25
 * Sources used: 
 *  http://www.codeproject.com/KB/audio-video/mpegaudioinfo.aspx
 *  http://gabriel.mp3-tech.org/mp3infotag.html
 */
using System;
using System.IO;
using System.Text;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// The LAME tag used to store more VBR information, usually it's located at the XING header.
    /// </summary>
    /// <remarks>
    /// The purpose of this tag is to provide extra information about the mp3 bit stream, encoder and parameters used.
    /// This tag should, as much as possible, be meaningful for as many encoders as possible,
    /// even if it is unlikely that other encoders than Lame will implement it.
    /// </remarks>
    //// NOTE: class is incomplete - not all values are extracted and stored from the LAME tag.
    /*
        In the Info Tag, the "Xing" identification string (mostly at 0x24) of the header is replaced by "Info" in case of a CBR file.  
        This was done to avoid CBR files to be recognized as traditional Xing VBR files by some decoders.
        Although the two identification strings "Xing" and "Info" are both valid,
        it is suggested that you keep the identification string "Xing" in case of VBR bit stream in order to keep compatibility.
        now:
            LAME VBR & ABR: "Xing" 
            LAME CBR: "Info"
    */
    public sealed class LameTag
    {
        /*
        LAME < 3.90 Header - LAME prior to 3.90 writes only a 20 byte encoder string
        size    description
        20      initial LAME info, 20 bytes for LAME tag. for example, "LAME3.12 (beta 6)"
        
        LAME >= 3.90 Header
        size    description
        9       Encoder short version, for example, "LAME3.90a" or "GOGO3.02b"
        1       Info Tag revision + VBR method
        1       Low pass filter value
        8       Replay Gain
        |---4       Peak signal amplitude
        |---2       Radio Replay Gain
        |---2       Audiophile Replay Gain
        1       Encoding flags + ATH Type
        1       if ABR {specified bitrate} else {minimal bitrate}
        3       Encoder delays
        1       Misc
        1       MP3 Gain
        2       Preset and surround info
        4       MusicLength
        2       MusicCRC
        2       CRC-16 of Info Tag
        --------- +
        36 bytes
        */

        // Indicates the VBR method used for encoding.
        private readonly string[] _vbrMethods = {
            "Unknown",
            "CBR",
            "ABR",
            "VBR1",
            "VBR2",
            "VBR3",
            "VBR4",
            "Reserved",
            "CBR2Pass",
            "ABR2Pass"
        };

        /// <summary>
        /// The musicCRC
        /// </summary>
        /// <remarks>
        /// If an application like mp3gain changes the main music frames of the mp3 (like the <see cref="Mp3Gain"/> field),
        /// then the musicCRC should be invalid.
        /// The Lame Tag CRC should still be valid however (it could be updated by mp3gain).
        /// </remarks>
        private readonly short _musicCrc;

        private readonly short _infoTagCrc;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="LameTag"/> class.
        /// </summary>
        /// <param name="firstFrameBuffer">The first frame buffer containing a <see cref="LameTag"/>.</param>
        public LameTag(StreamBuffer firstFrameBuffer)
        {
            // string lameTag = "LAME"
            firstFrameBuffer.ReadString(4);
            float ver;
            float.TryParse(firstFrameBuffer.ReadString(4), out ver);
            Version = ver;

            firstFrameBuffer.Seek(-8, SeekOrigin.Current);
            if (Version < 3.90f)
            {
                // Initial LAME info, 20 bytes for LAME tag. for example, "LAME3.12 (beta 6)"
                // LAME prior to 3.90 writes only a 20 byte encoder string.
                EncoderVersion = firstFrameBuffer.ReadString(20);
            }
            else
            {
                EncoderVersion = firstFrameBuffer.ReadString(9);

                // Revision Information Tag + VBR Info
                int infoAndVbr = firstFrameBuffer.ReadByte();

                // Revision information in 4 MSB
                InfoTagRevision = infoAndVbr >> 4;
                if (InfoTagRevision == Formats.InfoTagRevision.Reserved)
                    throw new ArgumentException("InfoTagRevision bit is set to reserved (0xF)");

                // VBR info in 4 LSB
                VbrMethod = infoAndVbr & 0x0F;

                // lowpass information, multiply by 100 to get hz
                LowpassFilterValue = firstFrameBuffer.ReadByte() * 100;

                // Radio replay gain fields
                // Peak signal amplitude
                PeakSignalAmplitude = firstFrameBuffer.ReadFloat();

                // Radio Replay Gain
                RadioReplayGain = firstFrameBuffer.ReadInt16();

                // Audiophile Replay Gain
                AudiophileReplayGain = firstFrameBuffer.ReadInt16();

                // Encoding Flags + ATH type
                int encodingFlagsAndAthType = firstFrameBuffer.ReadByte();

                // Encoding Flags in 4 MSB
                EncodingFlags = encodingFlagsAndAthType >> 4;

                // LAME ATH Type in 4 LSB
                AthType = encodingFlagsAndAthType & 0x0F;

                // If ABR, this will be the specified bitrate
                // Otherwise (CBR/VBR), the minimal bitrate (255 means 255 or bigger)
                BitRate = firstFrameBuffer.ReadByte();

                // the 12 bit values (0-4095) of how many samples were added at start (encoder delay)
                // in X and how many 0-samples were padded at the end in Y to complete the last frame.
                EncoderDelays = firstFrameBuffer.ReadInt(3);
                EncoderDelaySamples = EncoderDelays & 0xFFF;
                EncoderDelayPaddingSamples = EncoderDelays >> 12;
                ////int numberSamplesInOriginalWav = frameCount

                Misc = firstFrameBuffer.ReadByte();

                // Any mp3 can be amplified by a factor 2 ^ ( x * 0.25) in a lossless manner by a tool like eg. mp3gain
                // if done so, this 8-bit field can be used to log such transformation happened so that any given time it can be undone.
                Mp3Gain = firstFrameBuffer.ReadByte();

                // Preset and surround info
                PresetSurroundInfo = firstFrameBuffer.ReadInt16();

                // 32 bit integer filed containing the exact length in bytes of the mp3 file originally made by LAME excluded ID3 tag info at the end.
                MusicLength = firstFrameBuffer.ReadBigEndianInt32();

                // Contains a CRC-16 of the complete mp3 music data as made originally by LAME. 
                _musicCrc = (short)firstFrameBuffer.ReadBigEndianInt16();

                // contains a CRC-16 of the first 190 bytes (0x00 - 0xBD) of the Info header frame.
                // This field is calculated at the end, once all other fields are completed. 
                _infoTagCrc = (short)firstFrameBuffer.ReadBigEndianInt16();
            }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        //// <summary>
        //// Gets the VBR quality.
        //// </summary>
        //// <remarks>
        //// This field is there to indicate a quality level, although the scale was not precised in the original Xing specifications.
        //// In case of Lame, the meaning is the following:
        //// int Quality = (100 - 10 * gfp->VBR_q - gfp->quality)h
        //// <para/>
        //// examples:
        //// V0 and q0 = 100 - 10 * 0 - 0 = 100 => 64h
        //// V0 and q2 = 100 - 10 * 0 - 2 = 98 => 62h
        //// V2 and q5 = 100 - 10 * 2 - 5 = 75 => 4Bh
        //// V9 and q9 = 100 - 10 * 9 - 9 = 1 => 01h
        //// </remarks>
        //// This overrides the Quality field of the XING header if it's used. This field is not part of the LAMETag itself.
        ////public int VBRQuality { get; private set; }

        /// <summary>
        /// Gets the encoder version.
        /// </summary>
        /// LAME prior to 3.90 writes only a 20 byte encoder string, later versions write a 9 character encoder string.
        public string EncoderVersion { get; private set; }

        /// <summary>
        /// Gets the LAME version.
        /// </summary>
        public float Version { get; private set; }

        /// <summary>
        /// Gets the info tag revision.
        /// </summary>
        /// <remarks>
        /// Info Tag revision.
        /// Possible values:
        /// 0: rev0
        /// 1: rev1
        /// 15: reserved
        /// </remarks>
        public int InfoTagRevision { get; private set; }

        /// <summary>
        /// Gets the VBR method.
        /// </summary>
        /// <remarks>
        /// Indicate the VBR method used for encoding.
        /// Possible values:
        /// 0d unknown 
        /// 1d constant bitrate 
        /// 2d restricted VBR targeting a given average bitrate (ABR) 
        /// 3d full VBR method1 
        /// 4d full VBR method2 
        /// 5d full VBR method3 
        /// 6d full VBR method4 
        /// 7d  
        /// 8d constant bitrate 2 pass 
        /// 9d abr 2 pass 
        /// 10d  
        /// 11d  
        /// 12d  
        /// 13d  
        /// 14d  
        /// 15d reserved 
        /// </remarks>
        public int VbrMethod { get; private set; }

        /// <summary>
        /// Gets the low pass filter value, in Hz.
        /// </summary>
        /// <remarks>
        /// low pass = (low pass value) / 100
        /// <para/>
        /// range: 01h = 01d : 100Hz -> FFh = 255d : 25500Hz
        /// value 00h => unknown
        /// <para/>
        /// examples:
        /// <para/>
        /// byte 0xA6 = C3h
        /// C3h = 195d : 19500Hz
        /// <para/>
        /// byte 0xA6 = 78h
        /// 78h = 120d : 12000Hz
        /// </remarks>
        //// (in Hz)
        public int LowpassFilterValue { get; private set; }

        /// <summary>
        /// Gets the peak signal amplitude.
        /// </summary>
        /// <remarks>
        /// 32 bit floating point "Peak signal amplitude"
        /// 1.0 is maximal signal amplitude storable in decoding format.
        /// 0.8 is 80% of maximal signal amplitude storable in decoding format.
        /// 1.5 is 150% of maximal signal amplitude storable in decoding format.
        /// </remarks>
        //// Replay Gain field
        public float PeakSignalAmplitude { get; private set; }

        /// <summary>
        /// Gets the radio replay gain.
        /// </summary>
        /// <remarks>
        /// 16 bit "Radio Replay Gain" field, required to make all tracks equal loudness.
        /// from http://privatewww.essex.ac.uk/~djmrob/replaygain/rg_data_format.html
        /// <para/>
        /// bits 0h-2h: NAME of Gain adjustment:
        /// 000 = not set
        /// 001 = radio
        /// 010 = audiophile
        /// (see - room for plenty more!)
        /// <para/>
        /// bits 3h-5h: ORIGINATOR of Gain adjustment:
        /// 000 = not set
        /// 001 = set by artist
        /// 010 = set by user
        /// 011 = set by my model
        /// 100 = set by simple RMS average
        /// etc etc (see - room for plenty more again!)
        /// <para/>
        /// bit 6h: Sign bit
        /// bits 7h-Fh: ABSOLUTE GAIN ADJUSTMENT
        /// storing 10x the adjustment (to give the extra decimal place).
        /// </remarks>
        //// Replay Gain field
        public short RadioReplayGain { get; private set; }

        /// <summary>
        /// Gets the audiophile replay gain.
        /// </summary>
        /// <remarks>
        /// 16 bit "Audiophile Replay Gain" field, required to give ideal listening loudness
        /// from http://privatewww.essex.ac.uk/~djmrob/replaygain/rg_data_format.html
        /// <para/>
        /// bits 0h-2h: NAME of Gain adjustment:
        /// 000 = not set
        /// 001 = radio
        /// 010 = audiophile
        /// (see - room for plenty more!)
        /// <para/>
        /// bits 3h-5h: ORIGINATOR of Gain adjustment:
        /// 000 = not set
        /// 001 = set by artist
        /// 010 = set by user
        /// 011 = set by my model
        /// 100 = set by simple RMS average
        /// etc etc (see - room for plenty more again!)
        /// <para/>
        /// bit 6h: Sign bit
        /// bits 7h-Fh: ABSOLUTE GAIN ADJUSTMENT
        /// storing 10x the adjustment (to give the extra decimal place).
        /// </remarks>
        //// Replay Gain field
        public short AudiophileReplayGain { get; private set; }

        /// <summary>
        /// Gets the encoding flags.
        /// </summary>
        /// <remarks>
        /// 4 MSB: 4 encoding flags
        /// <para/>
        /// 000? LAME uses "--nspsytune",
        /// ? = 0 : false
        /// ? = 1 : true
        /// <para/>
        /// 00?0 LAME uses "--nssafejoint"
        /// ? = 0 : false
        /// ? = 1 : true
        /// <para/>
        /// 0?00 This track is --nogap continued in a next track
        /// ? = 0 : false
        /// ? = 1 : true
        /// is true for all but the last track in a --nogap album
        /// <para/>
        /// ?000 This track is the --nogap continuation of an earlier one
        /// ? = 0 : false
        /// ? = 1 : true
        /// is true for all but the first track in a --nogap album
        /// </remarks>
        public int EncodingFlags { get; private set; }

        /// <summary>
        /// Gets the LAME ATH Type.
        /// </summary>
        /// <remarks>
        /// 4 LSB: LAME ATH Type
        /// <para/>
        /// examples:
        /// <para/>
        /// byte 0xAF = 03h
        /// = 0000 0011b =>
        /// 4 MSB = 0000b = 0d : LAME does NOT use --nspsytune, LAME does NOT use --nssafejoint
        /// 4 LSB = 0011b = 3d : ATH type 3 used
        /// <para/>
        /// byte 0xAF = 15h
        /// = 0001 0101b =>
        /// 4 MSB = 0001b = 3d : LAME does use --nspsytune, LAME does NOT use --nssafejoint
        /// 4 LSB = 0101b = 5d : ATH type 5 used
        /// </remarks>
        public int AthType { get; private set; }

        /// <summary>
        /// Gets the bitrate.
        /// </summary>
        /// <remarks>
        /// if ABR {specified bitrate} else {minimal bitrate}
        /// <para/>
        /// IF the file is an ABR file:
        /// range: 01h = 01d : 1 kbit/s (--abr 1)  -> FFh = 255d : 255 kbit/s or larger (--abr 255)
        /// value 00h => unknown
        /// <para/>
        /// examples:
        /// byte 0xB0 = C3h
        /// C3h = 195d : --abr 195
        /// <para/>
        /// byte 0xB0 = 78h
        /// 78h = 128d : --abr 128
        /// <para/>
        /// byte 0xB0 = FEh
        /// FEh = 254d : --abr 254
        /// <para/>
        /// byte 0xB0 = FFh
        /// FEh = 255d : --abr 255 or higher, eg: --abr 280
        /// <para/>
        /// <para/>
        /// IF the file is NOT an ABR file: (CBR/VBR)
        /// the (CBR)/(minimal VBR (-b)) bitrate is stored here 8-255. 255 if bigger.
        /// <para/>
        /// examples:
        /// "LAME -V0 -b224" will store (224)d=(E0)h
        /// "LAME -b320" will store (255)d=(FF)h
        /// </remarks>
        public int BitRate { get; private set; }

        /// <summary>
        /// Gets the encoder delays.
        /// </summary>
        /// <remarks>
        /// Encoder delays
        /// store in 3 bytes:
        /// [xxxxxxxx][xxxxyyyy][yyyyyyyy]
        /// the 12 bit values (0-4095) of how many samples were added at start (encoder delay) in X
        /// and how many 0-samples were padded at the end in Y to complete the last frame.
        /// <para/>
        /// so ideally you could do: #frames * (#samples / frame) - (these two values) = exact number of samples in original wav.
        /// <para/>
        /// so worst case scenario you'd have a 48kHz file which would give it a range of 0.085s at the end and at the start.
        /// <para/>
        /// example:
        /// [01101100][00010010][11010010]
        /// <para/>
        /// X = (011011000001)b = (1729)d, so 1729 samples is the encoder delay
        /// Y = (001011010010)b = (722)d, so 722 samples have been padded at the end of the file
        /// </remarks>
        public int EncoderDelays { get; private set; }

        /// <summary>
        /// Gets the encoder delay samples.
        /// </summary>
        //// Encoder Delay MSB - 12 bits
        public int EncoderDelaySamples { get; private set; }

        /// <summary>
        /// Gets the encoder delay padding samples.
        /// </summary>
        //// Encoder Delay LSB - 12 bits
        public int EncoderDelayPaddingSamples { get; private set; }

        /// <summary>
        /// Gets the misc.
        /// </summary>
        /// <remarks>
        /// 2 lsb
        /// I'd like to add the different noise shapings also in a 2-bit field (0-3)
        /// (00)b: noise shaping: 0
        /// (01)b: noise shaping: 1
        /// (10)b: noise shaping: 2
        /// (11)b: noise shaping: 3
        /// <para/>
        /// 3 bits
        /// Stereo mode
        /// msb fist:
        /// (000)b: (m)ono
        /// (001)b: (s)tereo
        /// (010)b: (d)ual
        /// (011)b: (j)oint
        /// (100)b: (f)orce
        /// (101)b: (a)uto
        /// (110)b: (i)ntensity
        /// (111)b: (x)undefined / different
        /// <para/>
        /// 1 bit
        /// unwise settings used
        /// (0)b: no
        /// (1)b: yes (definition encoder side(*))
        /// (*)some settings were used which would likely damage quality in normal circumstances.
        /// (like disabling all use of the ATH or forcing only short blocks, -b192 ...)
        /// <para/>
        /// 2 msb
        /// Source (not mp3) sample frequency
        /// (00)b: 32kHz or smaller
        /// (01)b: 44.1kHz
        /// (10)b: 48kHz
        /// (11)b: higher than 48kHz
        /// </remarks>
        public int Misc { get; private set; }

        /// <summary>
        /// Gets the MP3 gain.
        /// </summary>
        /// <remarks>
        /// any mp3 can be amplified by a factor 2 ^ ( x * 0.25) in a lossless manner by a tool like eg. mp3gain
        /// byte 0xB5 is set to (00)h by default.
        /// if done so, this 8-bit field can be used to log such transformation happened so that any given time it can be undone.
        /// </remarks>
        public int Mp3Gain { get; private set; }

        /// <summary>
        /// Gets the preset surround info.
        /// </summary>
        /// <remarks>
        /// 2 most significant bits: unused
        /// <para/>
        /// 3 bits: surround info
        /// 0: no surround info
        /// 1: DPL encoding
        /// 2: DPL2 encoding
        /// 3: Ambisonic encoding
        /// 8: reserved
        /// <para/>
        /// 11 least significant bits: Preset used.
        /// 0: unknown/ no preset used
        /// This allows a range of 2047 presets. With Lame we would use the value of the internal preset enum.
        /// </remarks>
        public short PresetSurroundInfo { get; private set; }

        /// <summary>
        /// Gets the length of the music, in bytes. This is not the length of audio in MS, but rather the size of the audio in bytes.
        /// </summary>
        /// <value>
        /// The length of the music.
        /// </value>
        /// <remarks>
        /// 32 bit integer filed containing the exact length in bytes of the mp3 file
        /// originally made by LAME excluded ID3 tag info at the end.
        /// <para/>
        /// The first byte it counts is the first byte of this LAME Tag and
        /// the last byte it counts is the last byte of the last mp3 frame containing music.
        /// <para/>
        /// Should be file length at the time of LAME encoding, except when using ID3 tags.
        /// <para/>
        /// practical example: 
        /// {misc+Id3v2 tag info}[LAME Tag frame][complete mp3 music data]{misc+Id3v1/2 tag info}
        /// where contents between {} are not included in the Music Length.
        /// <para/>
        /// remark: applying any (Id3v2) kind of tagging or information in FRONT of the LAME/Xing Tag frame is a very bad idea.
        /// You will disable the functionality of all decoders to read the tag info correctly.
        /// (for example: VBR mp3 seek info will no longer be usable)
        /// <para/>
        /// range (1)d-(4,294,967,295)d [ or about 4294967295/(650*1024*1024)/320*1411 = 27.79 hours of 44.1kHz 320kbit/s music. ]
        /// <para/>
        /// Music length not set / unknown / larger than 4G:
        /// 0xB8 0xB9 0xBA 0xBB
        /// 00h 00h 00h 00h
        /// <para/>
        /// use of this field: together with the next field deliver
        /// an effective verification of music's integrity
        /// and effective shield from TAGs.
        /// </remarks>
        //// (in Bytes)
        public int MusicLength { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether this instance is VBR.
        /// </summary>
        /// <value><c>true</c> if this instance is VBR; otherwise, <c>false</c>.</value>
        public bool IsVbr
        {
            get { return (VbrMethod >= 3) && (VbrMethod <= 6); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is ABR.
        /// </summary>
        /// <value><c>true</c> if this instance is ABR; otherwise, <c>false</c>.</value>
        public bool IsAbr
        {
            get { return (VbrMethod == 2) || (VbrMethod == 9); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is CBR.
        /// </summary>
        /// <value><c>true</c> if this instance is CBR; otherwise, <c>false</c>.</value>
        public bool IsCbr
        {
            get { return (VbrMethod == 1) || (VbrMethod == 8); }
        }

        /// <summary>
        /// Gets the name of the VBR method.
        /// </summary>
        /// <value>
        /// The name of the VBR method.
        /// </value>
        public string VbrMethodName
        {
            get { return VbrMethod > _vbrMethods.Length ? _vbrMethods[0] : _vbrMethods[VbrMethod]; }
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds a <see cref="LameTag"/> in the given stream.
        /// </summary>
        /// <param name="firstFrameBuffer">The first frame buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The <see cref="LameTag"/> when found; otherwise, null.</returns>
        public static LameTag FindTag(StreamBuffer firstFrameBuffer, long offset)
        {
            if (firstFrameBuffer == null)
                throw new ArgumentNullException("firstFrameBuffer");

            // If limiting the LAME string to 9 bytes "LAME X.YZu", the extension revision 0 could take 27 bytes and it would still fit a 64 kbit 48kHz frame.
            firstFrameBuffer.Seek(offset, SeekOrigin.Begin);
            string tagName = firstFrameBuffer.ReadString(4, false, false);
            return String.Compare(tagName, "LAME", StringComparison.OrdinalIgnoreCase) == 0 ? new LameTag(firstFrameBuffer) : null;
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Places the <see cref="LameTag"/> into a byte array.
        /// </summary> 
        /// <returns>A byte array that represents the <see cref="LameTag"/> instance.</returns>
        public byte[] ToByteArray()
        {
            using (StreamBuffer buffer = new StreamBuffer())
            {
                if (Version < 3.09f)
                {
                    buffer.WriteString(EncoderVersion, Encoding.ASCII, 20);
                }
                else
                {
                    buffer.WriteString(EncoderVersion, Encoding.ASCII, 9);
                    buffer.WriteByte((byte)((InfoTagRevision & 0xF0) | (VbrMethod & 0x0F)));
                    buffer.WriteByte((byte)LowpassFilterValue);
                    buffer.WriteFloat(PeakSignalAmplitude);
                    buffer.WriteShort(RadioReplayGain);
                    buffer.WriteShort(AudiophileReplayGain);
                    buffer.WriteByte((byte)EncodingFlags);
                    buffer.WriteByte((byte)BitRate);
                    buffer.WriteBytes(EncoderDelays, 3);
                    buffer.WriteByte((byte)Misc);
                    buffer.WriteByte((byte)Mp3Gain);
                    buffer.WriteShort(PresetSurroundInfo);
                    buffer.WriteBigEndianInt32(MusicLength);
                    buffer.WriteBigEndianInt16(_musicCrc);
                    buffer.WriteBigEndianInt16(_infoTagCrc);
                }
                return buffer.ToByteArray();
            }
        }
    }
}
