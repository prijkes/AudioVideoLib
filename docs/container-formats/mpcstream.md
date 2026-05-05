# MpcStream

Walks Musepack (`.mpc`) containers in either stream version 7 (SV7)
or stream version 8 (SV8). `MpcStream.Header` exposes the parsed
top-of-file descriptor; `MpcStream.Packets` lists each SV8 keyed
packet (or, for SV7, a single audio span). Tags (APEv2 footer, ID3v2
header) are surfaced through the existing `AudioTags` scanner.

## On-disk layout

A Musepack file begins with one of two magic markers. SV7 files start
with the 3-byte ASCII sequence `MP+` followed by a one-byte stream-
version field whose low nibble is `7` (commonly `0x17`); a 3-byte
prefix check is not enough — the version nibble must also match. SV8
files begin with the 4-byte literal `MPCK`. The walker dispatches on
the magic and exposes the chosen version through `MpcStream.Version`.

In SV7, the magic is followed by a fixed 24-byte bit-packed
descriptor encoding the frame count, sample-rate index (44.1, 48, 32,
or 37.8 kHz), profile, encoder version, ReplayGain quad, and a
true-gapless flag. The audio bitstream then runs to end-of-file
(modulo any trailing APE/ID3v1 tag). The walker records a single
`MpcPacket` covering the audio span, with `Key` equal to `null`.

In SV8, the file is a sequence of keyed packets. Each packet starts
with a 2-byte ASCII key, followed by a Musepack varint giving the
total packet length (key + size field + payload), followed by the
payload. Recognised keys include `SH` (stream header), `RG`
(replaygain), `EI` (encoder info), `SO` / `ST` (seek table offset and
seek table), `AP` (audio packet), `CT` (chapter), and `SE` (stream
end). The walker parses `SH`, `RG`, and `EI` into `MpcStreamHeader`;
everything else is recorded as an opaque `(Key, StartOffset, Length)`
triple. Audio playback is the concatenation of the `AP` packet
payloads.

`MpcStream.WriteTo` streams the parsed file unchanged from source to
destination — there is no audio re-encoder. Tag editing happens
through `AudioTags`, which writes a new file the caller then re-parses.

```csharp
using var fs = File.OpenRead("song.mpc");
using var mpc = new MpcStream();
if (!mpc.ReadStream(fs)) return;

Console.WriteLine($"Version: {mpc.Version}");
Console.WriteLine($"Header:  {mpc.Header!.SampleRate} Hz, {mpc.Header.Channels} ch, {mpc.Header.TotalSamples} samples");
Console.WriteLine($"Profile: {mpc.Header.ProfileName} ({mpc.Header.Profile:F1})");

if (mpc.Version == MpcStreamVersion.Sv8)
{
    foreach (var pkt in mpc.Packets)
    {
        Console.WriteLine($"  {pkt.Key} @ 0x{pkt.StartOffset:X8} ({pkt.Length} bytes)");
    }
}

using var dst = File.Create("song-copy.mpc");
mpc.WriteTo(dst); // byte-identical for unmodified input
```
