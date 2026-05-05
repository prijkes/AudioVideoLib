# Container formats

Walkers in `AudioVideoLib.IO`. Each implements `IMediaContainer` and is
auto-detected by `MediaContainers.ReadStream(stream)`. Pick a walker
below for its on-disk shape, the public API surface, and any quirks.

| Walker | Container | Surfaces |
|---|---|---|
| [MpaStream](container-formats/mpastream.md) | MPEG-1 / MPEG-2 / MPEG-2.5 audio (Layer I / II / III) | frame-by-frame `MpaFrame` + Xing / LAME / VBRI VBR header |
| [FlacStream](container-formats/flacstream.md) | FLAC | metadata blocks (STREAMINFO, VORBIS_COMMENT, PICTURE, …) + frames |
| [RiffStream](container-formats/riffstream.md) | RIFF / WAV / RIFX | `fmt` / `data` + LIST INFO / id3 / bext / iXML side-channels |
| [AiffStream](container-formats/aiffstream.md) | AIFF / AIFF-C | `COMM` / `SSND` + NAME / AUTH / ANNO / COMT text chunks |
| [OggStream](container-formats/oggstream.md) | OGG | page-by-page; codec (vorbis / opus), channels, sample rate |
| [Mp4Stream](container-formats/mp4stream.md) | MP4 / M4A | top-level boxes + `moov.udta.meta.ilst` iTunes metadata |
| [AsfStream](container-formats/asfstream.md) | ASF / WMA / WMV | Header Object children + aggregated metadata tag |
| [MatroskaStream](container-formats/matroskastream.md) | Matroska / WebM | EBML header + first Segment (Info, Tags) |
| [DsfStream / DffStream](container-formats/dsf-dff-streams.md) | DSD audio (Sony DSF, Philips DFF) | DSD / fmt / data chunks + embedded ID3v2 |
| [MpcStream](container-formats/mpcstream.md) | Musepack (`.mpc`, SV7 + SV8) | header + per-packet byte ranges |
| [WavPackStream](container-formats/wavpackstream.md) | WavPack (`.wv` lossless / hybrid lossy) | per-block header + sub-block index |
| [TtaStream](container-formats/ttastream.md) | TrueAudio (`.tta`) | fixed header + seek table + per-frame byte ranges |
| [MacStream](container-formats/macstream.md) | Monkey's Audio (`.ape`, integer + float) | descriptor + header + seek table + per-frame byte ranges |

Container-embedded metadata (MP4 ilst, ASF, Matroska tags, WAV side
channels, AIFF text, DSF/DFF embedded ID3v2) is documented under
[Tag formats](tag-formats.md). The walkers above describe how the
container is parsed and which properties carry the metadata models.
