# MPC test samples

| File | Size | Source | License | Notes |
|---|---|---|---|---|
| sample-sv7.mpc | (absent) | n/a | n/a | Not checked in — see "Acquisition status" below. |
| sample-sv8.mpc | (absent) | n/a | n/a | Not checked in — see "Acquisition status" below. |

## Acquisition status (Bundle D, Phase 1)

Sample acquisition fell back to **option 3** ("no MPC sample available")
of the three options enumerated in `plans/2026-05-05-format-pack-mpc.md`
task 10. None of the three sources panned out:

1. **Reference vectors from the Musepack project.** No public-domain
   reference vectors are checked into `3rdparty/musepack_src_r475/` (the
   project ships only source for `mpcenc` / `mpc2sv8` / `mpcdec`; the
   `bin/` directory is empty and `docs/` contains only Doxygen config).
   A web search of `musepack.net` found no downloadable test-vector
   archive.

2. **Encode locally with `mpcenc` / `mpc2sv8`.** The .NET solution does
   not build the C encoder, and no precompiled `mpcenc.exe` /
   `mpc2sv8.exe` is available on this machine (`which mpcenc` empty,
   no `cmake` / `gcc-as-MSVC` toolchain on `PATH`). `ffmpeg` decodes
   `mpc7` / `mpc8` but does not encode them.

3. **Found-on-disk samples.** `3rdparty/getid3-mp3-testfiles/` does
   contain `.mpc` files (both SV7 and SV8 magic), but the SV7 entries
   are all clearly copyrighted commercial recordings (Pulp Fiction
   dialogue, Duran Duran, ABBA, etc.), and the SV8 entries are not
   accompanied by a redistribution license that would let us copy them
   into our own repo's test corpus.

## What this means for the test suite

`AudioVideoLib.Tests/MpcStreamTests.cs` lands with:

- Tests that do **not** need a real sample run unconditionally:
    - `WriteTo_ThrowsWhenSourceIsNull` — detached-source error.
    - `ReadStream_RejectsForeignMagic` — `'fLaC'` rejection.
    - `ReadStream_RejectsBareMpPlusWithWrongVersionNibble` —
      `'M','P','+',0x18` (low nibble 8, not 7) rejection.
- Tests that **do** need a sample are marked
  `[Fact(Skip = "no MPC sample available")]` with that exact reason.

## Reproduction once samples become available

The intended file layout when samples land is:

    AudioVideoLib.Tests/TestFiles/mpc/sample-sv7.mpc   (~few KB, SV7)
    AudioVideoLib.Tests/TestFiles/mpc/sample-sv8.mpc   (~few KB, SV8)

Magic bytes to verify after dropping samples in:

- `sample-sv7.mpc`: first 4 bytes `4D 50 2B X7` where `(X7 & 0x0F) == 0x07`
  (typically `4D 50 2B 17` — `MP+\x17`).
- `sample-sv8.mpc`: first 4 bytes `4D 50 43 4B` (`MPCK`).

Reproduction commands once a public-domain WAV and a built `mpcenc` are
available:

    mpcenc --quality 5.0 input.wav sample-sv7.mpc          # SV7 directly
    mpcenc --quality 5.0 input.wav tmp.mpc \
      && mpc2sv8 tmp.mpc sample-sv8.mpc                    # then upgrade to SV8

After dropping the files in, flip the `[Fact(Skip = "no MPC sample available")]`
attributes back to `[Fact]` and re-run `dotnet test`.

## csproj wiring

`AudioVideoLib.Tests/AudioVideoLib.Tests.csproj` already declares

    <None Include="TestFiles\mpc\*.mpc" CopyToOutputDirectory="PreserveNewest" />

so any future `*.mpc` dropped into this directory will be copied to
`bin/Debug/net10.0/TestFiles/mpc/` automatically — no further project
changes required.

Phase 2 will append a reference to this fragment from `src/TestFiles.txt`.
