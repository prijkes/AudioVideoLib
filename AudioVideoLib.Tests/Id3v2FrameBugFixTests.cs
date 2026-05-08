namespace AudioVideoLib.Tests;

using System;
using System.IO;
using System.Linq;

using AudioVideoLib.Tags;

using Xunit;

/// <summary>
/// Regression tests for correctness bugs found during the post-L4 audit pass.
/// Each test pins behavior against the bug it surfaced.
/// </summary>
public class Id3v2FrameBugFixTests
{
    private static T RoundTrip<T>(Id3v2Version version, T frame) where T : Id3v2Frame
    {
        var tag = new Id3v2Tag(version);
        tag.SetFrame(frame);
        var bytes = tag.ToByteArray();

        var reader = new Id3v2TagReader();
        using var stream = new MemoryStream(bytes);
        var offset = reader.ReadFromStream(stream, TagOrigin.Start);
        Assert.NotNull(offset);
        var roundTripped = Assert.IsType<Id3v2Tag>(offset!.AudioTag);
        return Assert.Single(roundTripped.Frames.OfType<T>());
    }

    // B1 — GRID Equals must require both GroupSymbol AND OwnerIdentifier (was OR).
    [Fact]
    public void Grid_Equals_RequiresBothSymbolAndOwner()
    {
        var a = new Id3v2GroupIdentificationRegistrationFrame
        {
            OwnerIdentifier = "http://example.com/grid",
            GroupSymbol = 0x80,
        };
        var b = new Id3v2GroupIdentificationRegistrationFrame
        {
            OwnerIdentifier = "http://other.example.com/grid",
            GroupSymbol = 0x80,
        };
        Assert.False(a.Equals(b), "different owners but same symbol must NOT be equal");

        var c = new Id3v2GroupIdentificationRegistrationFrame
        {
            OwnerIdentifier = "http://example.com/grid",
            GroupSymbol = 0x81,
        };
        Assert.False(a.Equals(c), "same owner but different symbol must NOT be equal");

        var d = new Id3v2GroupIdentificationRegistrationFrame
        {
            OwnerIdentifier = "http://example.com/grid",
            GroupSymbol = 0x80,
        };
        Assert.True(a.Equals(d), "same version, owner, and symbol must be equal");
    }

    // B2 — ETCO writer's `length` counter must reset per event.
    // KeyEvents are sorted by EventType ascending by the validator.
    [Fact]
    public void Etco_RoundTripsMultipleEvents()
    {
        var f = new Id3v2EventTimingCodesFrame(Id3v2Version.Id3v240)
        {
            TimeStampFormat = Id3v2TimeStampFormat.AbsoluteTimeMilliseconds,
        };
        f.KeyEvents.Add(new Id3v2KeyEvent(Id3v2KeyEventType.IntroStart, 1000));
        f.KeyEvents.Add(new Id3v2KeyEvent(Id3v2KeyEventType.MainPartStart, 2000));
        f.KeyEvents.Add(new Id3v2KeyEvent(Id3v2KeyEventType.OutroStart, 3000));

        var rt = RoundTrip(Id3v2Version.Id3v240, f);

        var events = rt.KeyEvents.ToList();
        Assert.Equal(3, events.Count);
        Assert.Equal(Id3v2KeyEventType.IntroStart, events[0].EventType);
        Assert.Equal(1000, events[0].TimeStamp);
        Assert.Equal(Id3v2KeyEventType.MainPartStart, events[1].EventType);
        Assert.Equal(2000, events[1].TimeStamp);
        Assert.Equal(Id3v2KeyEventType.OutroStart, events[2].EventType);
        Assert.Equal(3000, events[2].TimeStamp);
    }

    // B3 — EQUA writer must use 0x8000 for increment bit, preserving frequency.
    // Inspecting the encoded bytes directly: high bit set when Increment=true,
    // low 15 bits exactly equal to Frequency.
    [Fact]
    public void Equa_WriterSetsIncrementBitWithoutCorruptingFrequency()
    {
        var f = new Id3v2EqualisationFrame(Id3v2Version.Id3v230) { AdjustmentBits = 16 };
        f.EqualisationBands.Add(new Id3v2EqualisationBand(true, 1234, 100));

        var data = f.Data;
        // [0]=AdjustmentBits, [1..2]=encoded freq (big-endian short), [3..4]=adjustment
        var encodedFreq = (data[1] << 8) | data[2];
        Assert.Equal(0x8000 | 1234, encodedFreq);
    }

    // B4 — COMR PriceString must accept multi-currency strings.
    [Fact]
    public void Comr_PriceString_AcceptsMultiCurrency()
    {
        var f = new Id3v2CommercialFrame(Id3v2Version.Id3v230)
        {
            PriceString = "USD9.99/EUR8.50",
        };
        Assert.Equal("USD9.99/EUR8.50", f.PriceString);
    }

    // B4 — COMR PriceString must reject malformed decimal in a later segment.
    [Fact]
    public void Comr_PriceString_RejectsMalformedDecimalInLaterSegment()
    {
        var f = new Id3v2CommercialFrame(Id3v2Version.Id3v230);
        Assert.Throws<InvalidDataException>(() => f.PriceString = "USD9.99/EURabc");
    }

    // B5 — XRVA channel-info validator must fire when users mutate the list directly.
    // (Pre-fix: the ctor never bound ItemAdd; nulls slipped through silently.)
    [Fact]
    public void Xrva_ChannelInformationValidatorFiresOnDirectAdd()
    {
        var f = new Id3v2ExperimentalRelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240);
        Assert.Throws<NullReferenceException>(() => f.ChannelInformation.Add(null!));
    }

    // B5 — XRVA peak read must support arbitrary bit widths.
    [Fact]
    public void Xrva_PeakVolumeRoundTripsAtThreeBytes()
    {
        var f = new Id3v2ExperimentalRelativeVolumeAdjustment2Frame(Id3v2Version.Id3v240);
        f.ChannelInformation.Add(new Id3v2ChannelInformation(Id3v2ChannelType.MasterVolume, 100, 24, 0xABCDEF));

        var rt = RoundTrip(Id3v2Version.Id3v240, f);

        Assert.Single(rt.ChannelInformation);
        Assert.Equal(0xABCDEF, rt.ChannelInformation[0].PeakVolume);
        Assert.Equal((byte)24, rt.ChannelInformation[0].BitsRepresentingPeak);
    }

    // B6 — COMR TextEncoding setter must validate dependent fields against the NEW value.
    [Fact]
    public void Comr_ShortDescriptionMustBeValidUnderCurrentEncoding()
    {
        var f = new Id3v2CommercialFrame(Id3v2Version.Id3v230)
        {
            TextEncoding = Id3v2FrameEncodingType.Default,
        };
        // ISO-8859-1 cannot represent "你好"; setter must reject.
        Assert.Throws<InvalidDataException>(() => f.ShortDescription = "你好");
    }
}
