namespace AudioVideoLib.Studio.Controls;

using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Reusable hex-editor control with synchronised offset / hex / ASCII columns.
/// </summary>
/// <remarks>
/// The hex column accepts only hex digits (input is filtered at <c>PreviewTextInput</c>);
/// the ASCII column accepts any byte (Latin-1 mapping for codes 0..255). Both columns
/// support typing to overwrite, Backspace / Delete to remove bytes, Enter to insert
/// a byte (hex column appends 0x00; ASCII appends Latin-1 of typed char). Caret position
/// in one column is synchronised to the corresponding byte position in the other column.
/// <para />
/// 16 bytes per line. Hex format: <c>"XX XX XX...XX"</c> (single space between bytes).
/// ASCII format: 16 chars per line, non-printable bytes rendered as <c>'.'</c>.
/// <para />
/// <see cref="MaxLength"/> caps growth; <see cref="MinLength"/> blocks shrinking below
/// a floor (useful for spec-mandated minimum sizes).
/// </remarks>
public partial class HexEditor : UserControl
{
    /// <summary>The current byte buffer.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(byte[]), typeof(HexEditor),
        new FrameworkPropertyMetadata(
            Array.Empty<byte>(),
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
            OnDataChanged));

    /// <summary>Maximum allowed buffer size; null means unlimited.</summary>
    public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
        nameof(MaxLength), typeof(int?), typeof(HexEditor),
        new PropertyMetadata(null));

    /// <summary>Minimum allowed buffer size; null means unrestricted (can shrink to empty).</summary>
    public static readonly DependencyProperty MinLengthProperty = DependencyProperty.Register(
        nameof(MinLength), typeof(int?), typeof(HexEditor),
        new PropertyMetadata(null));

    public byte[] Data
    {
        get => (byte[])GetValue(DataProperty) ?? [];
        set => SetValue(DataProperty, value ?? []);
    }

    public int? MaxLength
    {
        get => (int?)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public int? MinLength
    {
        get => (int?)GetValue(MinLengthProperty);
        set => SetValue(MinLengthProperty, value);
    }

    private bool _suppressRender;
    private bool _suppressSelectionSync;

    public HexEditor()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (HexEditor)d;
        if (!self._suppressRender)
        {
            self.Render();
        }
    }

    // ============================================================
    // Rendering
    // ============================================================

    private void Render()
    {
        var bytes = Data;
        OffsetBox.Text = RenderOffsets(bytes.Length);
        HexBox.Text = RenderHex(bytes);
        AsciiBox.Text = RenderAscii(bytes);
    }

    /// <summary>Render the offset gutter — one 8-digit hex offset per line.</summary>
    public static string RenderOffsets(int length)
    {
        if (length == 0)
        {
            return "00000000";
        }
        var sb = new StringBuilder();
        var lines = (length + 15) / 16;
        for (var i = 0; i < lines; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            sb.Append((i * 16).ToString("X8"));
        }
        return sb.ToString();
    }

    /// <summary>Render the hex column: 16 bytes per line, each as <c>"XX"</c>, single space between, newline between lines, no trailing space.</summary>
    public static string RenderHex(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        var sb = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(i % 16 == 0 ? '\n' : ' ');
            }
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }

    /// <summary>Render the ASCII column: 16 chars per line, non-printable bytes as <c>'.'</c>.</summary>
    public static string RenderAscii(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        var sb = new StringBuilder(bytes.Length + (bytes.Length / 16));
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && i % 16 == 0)
            {
                sb.Append('\n');
            }
            var b = bytes[i];
            sb.Append(b is >= 32 and <= 126 ? (char)b : '.');
        }
        return sb.ToString();
    }

    // ============================================================
    // Caret <-> byte index mapping
    // ============================================================

    /// <summary>Byte index for a caret position in the hex column. Caret on the high nibble, low nibble, or trailing space all resolve to the byte at that visual position.</summary>
    public static int HexCaretToByteIndex(int caret, int totalBytes)
    {
        // Each line is min(16, remaining_bytes) bytes. Hex chars per line: 3*N - 1 (no trailing space).
        // We handle multi-line layouts by walking 16-byte chunks.
        if (caret <= 0 || totalBytes == 0)
        {
            return 0;
        }
        var byteIdx = 0;
        var pos = 0;
        while (byteIdx < totalBytes)
        {
            var bytesOnLine = Math.Min(16, totalBytes - byteIdx);
            var lineLen = (bytesOnLine * 3) - 1;       // "XX " * (n-1) + "XX"
            if (caret <= pos + lineLen)
            {
                var col = caret - pos;
                // Each byte takes 3 columns: "XX " (high nibble, low nibble, space).
                // Caret on the space (or past the last byte's low nibble) means caret is between
                // bytes — advance to the next byte. Formula: (col + 1) / 3.
                var byteInLine = (col + 1) / 3;
                if (byteInLine > bytesOnLine)
                {
                    byteInLine = bytesOnLine;
                }
                return byteIdx + byteInLine;
            }
            pos += lineLen;
            byteIdx += bytesOnLine;
            // newline between lines (1 char)
            if (byteIdx < totalBytes)
            {
                if (caret == pos)
                {
                    // caret right at the newline boundary — treat as start of next line
                    return byteIdx;
                }
                pos += 1;
            }
        }
        return totalBytes;
    }

    /// <summary>Caret offset for the start (high nibble) of a given byte in the hex column.</summary>
    public static int ByteIndexToHexCaret(int byteIndex, int totalBytes)
    {
        if (byteIndex <= 0)
        {
            return 0;
        }
        var clamped = Math.Min(byteIndex, totalBytes);
        var fullLines = clamped / 16;
        var byteInLine = clamped % 16;
        // each full line: 16*3 - 1 = 47 hex chars + 1 newline = 48 chars
        var pos = fullLines * 48;
        if (byteInLine > 0)
        {
            pos += byteInLine * 3;     // each byte occupies "XX " (3 chars); we want start of byte
        }
        return pos;
    }

    /// <summary>Caret position (high-nibble) in the hex column for a byte just past the end (insertion point at EOF).</summary>
    public static int EndOfHexCaret(int totalBytes)
    {
        if (totalBytes == 0)
        {
            return 0;
        }
        var fullLines = totalBytes / 16;
        var byteInLine = totalBytes % 16;
        if (byteInLine == 0)
        {
            // ends exactly on a 16-byte boundary; caret sits past last byte = end of last line
            return (fullLines * 48) - 1;
        }
        return (fullLines * 48) + (byteInLine * 3) - 1;
    }

    /// <summary>Byte index for a caret position in the ASCII column.</summary>
    public static int AsciiCaretToByteIndex(int caret, int totalBytes)
    {
        if (caret <= 0 || totalBytes == 0)
        {
            return 0;
        }
        // ASCII format: 16 chars per line, '\n' between lines.
        // Each full line: 16 chars + 1 newline = 17 chars.
        var byteIdx = 0;
        var pos = 0;
        while (byteIdx < totalBytes)
        {
            var charsOnLine = Math.Min(16, totalBytes - byteIdx);
            if (caret <= pos + charsOnLine)
            {
                return byteIdx + (caret - pos);
            }
            pos += charsOnLine;
            byteIdx += charsOnLine;
            if (byteIdx < totalBytes)
            {
                if (caret == pos)
                {
                    return byteIdx;
                }
                pos += 1;       // newline
            }
        }
        return totalBytes;
    }

    /// <summary>Caret offset for the given byte index in the ASCII column.</summary>
    public static int ByteIndexToAsciiCaret(int byteIndex, int totalBytes)
    {
        if (byteIndex <= 0)
        {
            return 0;
        }
        var clamped = Math.Min(byteIndex, totalBytes);
        var fullLines = clamped / 16;
        var byteInLine = clamped % 16;
        return (fullLines * 17) + byteInLine;
    }

    // ============================================================
    // Hex column input
    // ============================================================

    // The hex/ASCII columns are sized to their content, so when the buffer is empty
    // they collapse to ~0 px and can't receive a mouse click. Forward clicks on the
    // surrounding dark surface to the hex column so the user can start typing.
    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HexBox.Focus();
        HexBox.CaretIndex = HexBox.Text.Length;
        e.Handled = true;
    }

    private void HexBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
        {
            return;
        }
        var c = e.Text[0];
        if (!IsHex(c))
        {
            e.Handled = true;
            return;
        }

        var bytes = (byte[])Data.Clone();
        var caret = HexBox.CaretIndex;
        var byteIdx = HexCaretToByteIndex(caret, bytes.Length);
        var nibble = HexCaretNibble(HexBox.Text, caret);
        var nibValue = HexValue(c);

        if (byteIdx >= bytes.Length)
        {
            // append
            if (MaxLength is { } max && bytes.Length >= max)
            {
                e.Handled = true;
                return;
            }
            bytes = [.. bytes, (byte)(nibValue << 4)];
            CommitData(bytes, ByteIndexToHexCaret(bytes.Length - 1, bytes.Length) + 1, syncToAscii: true);
            e.Handled = true;
            return;
        }

        var b = bytes[byteIdx];
        if (nibble == HexNibble.High)
        {
            bytes[byteIdx] = (byte)((b & 0x0F) | (nibValue << 4));
            CommitData(bytes, ByteIndexToHexCaret(byteIdx, bytes.Length) + 1, syncToAscii: true);
        }
        else
        {
            // Low nibble (or caret on space — treat as low nibble of preceding byte)
            bytes[byteIdx] = (byte)((b & 0xF0) | nibValue);
            // Advance past this byte to next byte's high nibble (or end-of-line caret)
            var nextCaret = ByteIndexToHexCaret(byteIdx + 1, bytes.Length);
            if (byteIdx + 1 >= bytes.Length)
            {
                nextCaret = EndOfHexCaret(bytes.Length) + 1;
            }
            CommitData(bytes, nextCaret, syncToAscii: true);
        }
        e.Handled = true;
    }

    private void HexBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            DeleteAtHexCaret(direction: -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteAtHexCaret(direction: 1);
            e.Handled = true;
        }
    }

    private void DeleteAtHexCaret(int direction)
    {
        var bytes = Data;
        if (bytes.Length == 0)
        {
            return;
        }
        if (MinLength is { } min && bytes.Length <= min)
        {
            return;
        }
        var caret = HexBox.CaretIndex;
        var byteIdx = HexCaretToByteIndex(caret, bytes.Length);
        if (direction < 0)
        {
            byteIdx -= 1;
            if (byteIdx < 0)
            {
                return;
            }
        }
        if (byteIdx >= bytes.Length)
        {
            byteIdx = bytes.Length - 1;
        }
        var newBytes = new byte[bytes.Length - 1];
        Array.Copy(bytes, 0, newBytes, 0, byteIdx);
        Array.Copy(bytes, byteIdx + 1, newBytes, byteIdx, bytes.Length - byteIdx - 1);
        var newCaret = direction < 0
            ? ByteIndexToHexCaret(byteIdx, newBytes.Length)
            : ByteIndexToHexCaret(byteIdx, newBytes.Length);
        CommitData(newBytes, newCaret, syncToAscii: true);
    }

    private enum HexNibble { High, Low }

    private static HexNibble HexCaretNibble(string text, int caret)
    {
        // caret right after a hex digit + immediately before another hex digit = low nibble of that pair
        // caret on a space, newline, or right after a low nibble = high nibble of next byte (or low of current if space-after-low)
        if (caret <= 0 || caret > text.Length)
        {
            return HexNibble.High;
        }
        var prev = caret - 1;
        if (prev >= 0 && IsHex(text[prev]))
        {
            // previous is a hex digit; check if the one before THAT was also a hex digit (then prev is a low nibble — caret is past byte)
            if (prev - 1 >= 0 && IsHex(text[prev - 1]))
            {
                // prev was the low nibble; caret is between bytes → high of next
                return HexNibble.High;
            }
            // prev was the high nibble; caret is at the low nibble
            return HexNibble.Low;
        }
        return HexNibble.High;
    }

    // ============================================================
    // ASCII column input
    // ============================================================

    private void AsciiBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length == 0)
        {
            return;
        }
        var bytes = (byte[])Data.Clone();
        var caret = AsciiBox.CaretIndex;
        var byteIdx = AsciiCaretToByteIndex(caret, bytes.Length);

        // Map each typed char to a byte (Latin-1 truncation; codes > 255 → '?').
        Span<byte> typed = stackalloc byte[e.Text.Length];
        for (var i = 0; i < e.Text.Length; i++)
        {
            typed[i] = e.Text[i] <= 0xFF ? (byte)e.Text[i] : (byte)'?';
        }

        if (byteIdx >= bytes.Length)
        {
            // append
            if (MaxLength is { } max && bytes.Length + typed.Length > max)
            {
                e.Handled = true;
                return;
            }
            var grown = new byte[bytes.Length + typed.Length];
            Array.Copy(bytes, grown, bytes.Length);
            for (var i = 0; i < typed.Length; i++)
            {
                grown[bytes.Length + i] = typed[i];
            }
            CommitData(grown, ByteIndexToAsciiCaret(grown.Length, grown.Length), syncToAscii: false);
            e.Handled = true;
            return;
        }

        // overwrite at byteIdx
        var available = bytes.Length - byteIdx;
        if (typed.Length > available)
        {
            // overwrite then append
            if (MaxLength is { } max && bytes.Length + (typed.Length - available) > max)
            {
                e.Handled = true;
                return;
            }
            var grown = new byte[bytes.Length + (typed.Length - available)];
            Array.Copy(bytes, grown, bytes.Length);
            for (var i = 0; i < typed.Length; i++)
            {
                grown[byteIdx + i] = typed[i];
            }
            CommitData(grown, ByteIndexToAsciiCaret(byteIdx + typed.Length, grown.Length), syncToAscii: false);
        }
        else
        {
            for (var i = 0; i < typed.Length; i++)
            {
                bytes[byteIdx + i] = typed[i];
            }
            CommitData(bytes, ByteIndexToAsciiCaret(byteIdx + typed.Length, bytes.Length), syncToAscii: false);
        }
        e.Handled = true;
    }

    private void AsciiBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back)
        {
            DeleteAtAsciiCaret(direction: -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteAtAsciiCaret(direction: 1);
            e.Handled = true;
        }
    }

    private void DeleteAtAsciiCaret(int direction)
    {
        var bytes = Data;
        if (bytes.Length == 0)
        {
            return;
        }
        if (MinLength is { } min && bytes.Length <= min)
        {
            return;
        }
        var caret = AsciiBox.CaretIndex;
        var byteIdx = AsciiCaretToByteIndex(caret, bytes.Length);
        if (direction < 0)
        {
            byteIdx -= 1;
            if (byteIdx < 0)
            {
                return;
            }
        }
        if (byteIdx >= bytes.Length)
        {
            byteIdx = bytes.Length - 1;
        }
        var newBytes = new byte[bytes.Length - 1];
        Array.Copy(bytes, 0, newBytes, 0, byteIdx);
        Array.Copy(bytes, byteIdx + 1, newBytes, byteIdx, bytes.Length - byteIdx - 1);
        CommitData(newBytes, ByteIndexToAsciiCaret(byteIdx, newBytes.Length), syncToAscii: false);
    }

    // ============================================================
    // Cursor sync
    // ============================================================

    private void HexBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSelectionSync)
        {
            return;
        }
        var byteIdx = HexCaretToByteIndex(HexBox.CaretIndex, Data.Length);
        try
        {
            _suppressSelectionSync = true;
            AsciiBox.CaretIndex = ByteIndexToAsciiCaret(byteIdx, Data.Length);
        }
        finally
        {
            _suppressSelectionSync = false;
        }
    }

    private void AsciiBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSelectionSync)
        {
            return;
        }
        var byteIdx = AsciiCaretToByteIndex(AsciiBox.CaretIndex, Data.Length);
        try
        {
            _suppressSelectionSync = true;
            HexBox.CaretIndex = ByteIndexToHexCaret(byteIdx, Data.Length);
        }
        finally
        {
            _suppressSelectionSync = false;
        }
    }

    // ============================================================
    // Commit + restore
    // ============================================================

    private void CommitData(byte[] newBytes, int newCaret, bool syncToAscii)
    {
        try
        {
            _suppressRender = true;
            _suppressSelectionSync = true;
            Data = newBytes;
            Render();
            if (syncToAscii)
            {
                HexBox.CaretIndex = Math.Min(newCaret, HexBox.Text.Length);
                AsciiBox.CaretIndex = ByteIndexToAsciiCaret(
                    HexCaretToByteIndex(HexBox.CaretIndex, newBytes.Length),
                    newBytes.Length);
                HexBox.Focus();
            }
            else
            {
                AsciiBox.CaretIndex = Math.Min(newCaret, AsciiBox.Text.Length);
                HexBox.CaretIndex = ByteIndexToHexCaret(
                    AsciiCaretToByteIndex(AsciiBox.CaretIndex, newBytes.Length),
                    newBytes.Length);
                AsciiBox.Focus();
            }
        }
        finally
        {
            _suppressRender = false;
            _suppressSelectionSync = false;
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static bool IsHex(char c)
        => c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f');

    private static int HexValue(char c)
        => c is >= '0' and <= '9' ? c - '0'
            : c is >= 'A' and <= 'F' ? c - 'A' + 10
            : c - 'a' + 10;
}
