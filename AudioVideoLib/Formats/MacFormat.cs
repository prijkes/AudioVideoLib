namespace AudioVideoLib.Formats;

/// <summary>
/// The two audio sample formats that Monkey's Audio supports. The distinction is encoded in
/// the descriptor's <c>cID</c> magic — see
/// <c>3rdparty/MAC_1284_SDK/Source/MACLib/APECompressCreate.cpp:250-253</c>.
/// </summary>
public enum MacFormat
{
    /// <summary>Integer-PCM source. Magic is <c>"MAC "</c>.</summary>
    Integer = 0,

    /// <summary>IEEE float source. Magic is <c>"MACF"</c>.</summary>
    Float = 1,
}
