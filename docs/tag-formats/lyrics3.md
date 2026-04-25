# Lyrics3

**Spec:** [id3.org/Lyrics3v2](http://id3.org/Lyrics3v2).

**Shape:** v1 is a simple "LYRICS"…"LYRICSEND" wrapper at end-of-file
(just before any ID3v1). v2 adds typed fields (IND, LYR, INF, AUT, EAL,
EAR, ETT, IMG) and a final 9-byte length+footer.
