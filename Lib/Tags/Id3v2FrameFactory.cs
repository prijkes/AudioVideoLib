/*
 * Date: 2012-12-08
 * Sources used:
 *  http://www.id3.org/Id3v2-00
 *  http://www.id3.org/Id3v2.3.0
 *  http://www.id3.org/id3guide
 *  http://www.id3.org/Id3v2.4.0-structure
 *  http://www.id3.org/Id3v2.4.0-frames
 *  http://www.id3.org/Id3v2.4.0-changes
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVideoLib.Tags
{
    /// <summary>
    /// Class used to store an <see cref="Id3v2Tag"/> frame.
    /// </summary>
    /// <remarks>
    /// A frame is a block of information in an <see cref="Id3v2Tag"/>.
    /// </remarks>
    public partial class Id3v2Frame
    {
        private static readonly Id3v2FrameFactoryItem[] FrameFactories = 
        {
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2AudioSeekPointIndexFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "ASPI", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2AudioSeekPointIndexFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2RecommendedBufferSizeFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "BUF", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "RBUF", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2RecommendedBufferSizeFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2CompressedDataMetaFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "CDM", new[] { Id3v2Version.Id3v221 } } },
                Factory = (version, identifier) => new Id3v2CompressedDataMetaFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2PlayCounterFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "CNT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "PCNT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2PlayCounterFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2CommentFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "COM", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "COMM", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2CommentFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2CommercialFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "COMR", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2CommercialFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2AudioEncryptionFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "CRA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "AENC", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2AudioEncryptionFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2EncryptedMetaFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "CRM", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } } },
                Factory = (version, identifier) => new Id3v2EncryptedMetaFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2EncryptionMethodRegistrationFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "ENCR", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2EncryptionMethodRegistrationFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2EqualisationFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "EQU", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "EQUA", new[] { Id3v2Version.Id3v230 } } },
                Factory = (version, identifier) => new Id3v2EqualisationFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2Equalisation2Frame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "EQU2", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2Equalisation2Frame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2EventTimingCodesFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "ETC", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "ETCO", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2EventTimingCodesFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2GeneralEncapsulatedObjectFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "GEO", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "GEOB", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2GeneralEncapsulatedObjectFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2Equalisation2Frame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "GRID", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2GroupIdentificationRegistrationFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2InvolvedPeopleListFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "IPL", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "IPLS", new[] { Id3v2Version.Id3v230 } } },
                Factory = (version, identifier) => new Id3v2InvolvedPeopleListFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2LinkedInformationFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "LNK", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "LINK", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2LinkedInformationFrame(version, identifier)
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2MusicCdIdentifierFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "MCI", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "MCDI", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2MusicCdIdentifierFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2MpegLocationLookupTableFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "MLL", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "MLLT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2MpegLocationLookupTableFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2OwnershipFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "OWNE", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2OwnershipFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2AttachedPictureFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "PIC", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "APIC", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2AttachedPictureFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2PopularimeterFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "POP", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "POPM", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2PopularimeterFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2PositionSynchronizationFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "POSS", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2PositionSynchronizationFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2PrivateFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "PRIV", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2PrivateFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2ReverbFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "REV", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "REVB", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2ReverbFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2ReplayGainAdjustmentFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "RGAD", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2ReplayGainAdjustmentFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2RelativeVolumeAdjustmentFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "RVA", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "RVAD", new[] { Id3v2Version.Id3v230 } } },
                Factory = (version, identifier) => new Id3v2RelativeVolumeAdjustmentFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2RelativeVolumeAdjustment2Frame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "RVA2", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2RelativeVolumeAdjustment2Frame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2SeekFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "SEEK", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2SeekFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2SignatureFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "SIGN", new[] { Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2SignatureFrame(version)
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2SynchronizedLyricsFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "SLT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "SYLT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2SynchronizedLyricsFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2SyncedTempoCodesFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "STC", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "SYTC", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2SyncedTempoCodesFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2UserDefinedTextInformationFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "TXX", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "TXXX", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2UserDefinedTextInformationFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2TextFrame),
                Identifiers = null,
                PartialComparer = (version, identifier) => identifier.StartsWith("T"),
                Factory = (version, identifier) => new Id3v2TextFrame(version, identifier)
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2UniqueFileIdentifierFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "UFI", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "UFID", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2UniqueFileIdentifierFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2UnsynchronizedLyricsFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "ULT", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "USLT", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2UnsynchronizedLyricsFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2TermsOfUseFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "USER", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2TermsOfUseFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2UserDefinedUrlLinkFrame),
                Identifiers = new Dictionary<string, Id3v2Version[]> {
                    { "WXX", new[] { Id3v2Version.Id3v220, Id3v2Version.Id3v221 } },
                    { "WXXX", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2UserDefinedUrlLinkFrame()
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2UrlLinkFrame),
                Identifiers = null,
                PartialComparer = (version, identifier) => identifier.StartsWith("W"),
                Factory = (version, identifier) => new Id3v2UrlLinkFrame(version, identifier)
            },
            new Id3v2FrameFactoryItem
            {
                Type = typeof(Id3v2ExperimentalRelativeVolumeAdjustment2Frame),
                Identifiers = new Dictionary<string, Id3v2Version[]> { { "XRVA", new[] { Id3v2Version.Id3v230, Id3v2Version.Id3v240 } } },
                Factory = (version, identifier) => new Id3v2ExperimentalRelativeVolumeAdjustment2Frame()
            }
        };

        ////------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the identifier for the specified <typeparamref name="T">type</typeparamref>.
        /// </summary>
        /// <typeparam name="T">The frame type.</typeparam>
        /// <param name="version">The version.</param>
        /// <returns>
        /// The identifier if found; otherwise, null.
        /// </returns>
        public static string GetIdentifier<T>(Id3v2Version version) where T : Id3v2Frame
        {
            return
                FrameFactories.Where(f => f.Type == typeof(T) && f.Identifiers != null && f.Identifiers.Values.Any(v => v != null && v.Contains(version)))
                    .Select(f => f.Identifiers.Where(i => i.Value != null && i.Value.Contains(version)).Select(i => i.Key).FirstOrDefault())
                    .FirstOrDefault();
        }

        ////------------------------------------------------------------------------------------------------------------------------------

        private static Id3v2Frame GetFrame(Id3v2Version version, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            Id3v2Frame frame = FrameFactories.Where(f => f.IsMatch(version, identifier)).Select(f => f.Factory(version, identifier)).FirstOrDefault();

            // The identifier might end with a NULL terminator '\0' because the writer of the tag is bugged.
            // Because of this, we won't find the tag by just comparing the identifier.
            // Instead, try to partly match it, ignoring the version.
            // If found, grab the 'correct' identifier for the given version, and use that. Otherwise, use the original identifier as fallback...
            if (frame == null)
            {
                foreach (Id3v2FrameFactoryItem factoryItem in FrameFactories.Where(f => f.Identifiers != null))
                {
                    // See if a known identifier matches the given identifier, or partly does, anyway
                    string realIdentifier =
                        factoryItem.Identifiers.OrderByDescending(i => i.Key)
                            .Where(i => i.Key.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0)
                            .Select(i => i.Key)
                            .FirstOrDefault();

                    // If we found a matching identifier, grab the identifier for the specified version; otherwise, use the identifier as-is
                    if (!String.IsNullOrEmpty(realIdentifier))
                    {
                        identifier = factoryItem.Identifiers.Where(i => i.Value.Contains(version)).Select(i => i.Key).FirstOrDefault() ?? realIdentifier;
                        frame = FrameFactories.Where(f => f.IsMatch(version, identifier)).Select(f => f.Factory(version, identifier)).FirstOrDefault();
                        break;
                    }
                }
            }

            // If not found or the version is not supported, return a default frame
            if ((frame == null) || !frame.IsVersionSupported(version))
            {
                // Initialize new frame
                frame = new Id3v2Frame(identifier);
            }

            // Set the version here.
            // If we call the public constructor, it might throw an InvalidVersionException because the version we read from a stream is not valid.
            // By setting the private field, we circumvent the checks in the constructor (version check, identifier check etc.).
            frame.Version = version;
            return frame;
        }
    }
}
