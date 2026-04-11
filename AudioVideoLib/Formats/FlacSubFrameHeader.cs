/*
 * Date: 2013-03-22
 * Sources used: 
 *  http://flac.sourceforge.net/format.html
 *  http://py.thoulon.free.fr/
 */
using System;
using System.IO;

using AudioVideoLib.IO;

namespace AudioVideoLib.Formats
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FlacSubFrame
    {
        private FlacSubFrameType _type;

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the sub frame type.
        /// </summary>
        /// <value>
        /// The sub frame type.
        /// </value>
        public FlacSubFrameType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Gets the true sample size.
        /// </summary>
        /// <value>
        /// The true sample size.
        /// </value>
        /// <remarks>
        /// This is the sub frame's sample size (as determined from the <see cref="FlacFrame"/>'s sample size, 
        /// amended by the <see cref="FlacFrame.ChannelAssignment"/>)
        /// and minus the <see cref="WastedBits">number of wasted bits</see>.
        /// </remarks>
        public int SampleSize { get; private set; }

        /// <summary>
        /// Gets the wasted bits-per-sample.
        /// </summary>
        /// <value>
        /// The wasted bits-per-sample.
        /// </value>
        public int WastedBits { get; private set; }

        /// <summary>
        /// Gets the header.
        /// </summary>
        /// <value>
        /// The header.
        /// </value>
        protected int Header { get; private set; }

        ////------------------------------------------------------------------------------------------------------------------------------

        private void ReadHeader(StreamBuffer sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            Header = sb.ReadBigEndianInt32();

            WastedBits = Header & 0x01;
            if (WastedBits > 0)
                WastedBits = sb.ReadUnaryInt();

            int type = (Header >> 1) & 0x7E;
            switch (type)
            {
                case 0x00:
                    _type = FlacSubFrameType.Constant;
                    break;

                case 0x01:
                    _type = FlacSubFrameType.Verbatim;
                    break;

                default:
                    if ((type >= 0x08) && (type <= 0x0C))
                        _type = FlacSubFrameType.Fixed;
                    else if (type >= 0x20)
                        _type = FlacSubFrameType.LinearPredictor;
                    else
                        _type = FlacSubFrameType.Reserved;
                    break;
            }
        }
    }
}
