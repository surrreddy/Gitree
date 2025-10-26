namespace Gitree.UI;

public sealed class Screen
{
    private readonly bool _useAnsi;
    private int? _startRow;
    private int _linesCount;

    public Screen(bool useAnsi)
    {
        _useAnsi = useAnsi;
    }

    public void DrawLines(IReadOnlyList<string> lines, int cursorIndex)
    {
        if (lines == null)
        {
            return;
        }

        EnsureStartRow(lines.Count);

        if (_startRow.HasValue)
        {
            SetCursorPositionSafe(0, _startRow.Value);
        }

        int width = GetConsoleWidth();
        _linesCount = lines.Count;

        for (int i = 0; i < lines.Count; i++)
        {
            string baseText = lines[i] ?? string.Empty;
            bool isFocused = i == cursorIndex;
            string rendered = RenderLine(baseText, isFocused);
            int visibleLength = GetVisibleLength(baseText);
            WritePadded(rendered, visibleLength, width);
        }
    }

    public void DrawStatus(string text)
    {
        int statusRow = (_startRow ?? Console.CursorTop) + _linesCount;
        SetCursorPositionSafe(0, statusRow);
        int width = GetConsoleWidth();
        string status = text ?? string.Empty;
        WritePadded(status, status.Length, width);
    }

    public void ClearBelow(int fromRow)
    {
        _ = fromRow;
    }

    private void EnsureStartRow(int lineCount)
    {
        if (!_startRow.HasValue)
        {
            int currentTop = Console.CursorTop;
            _startRow = Math.Max(0, currentTop - lineCount);
            _linesCount = lineCount;
        }
    }

    private string RenderLine(string line, bool isFocused)
    {
        if (_useAnsi)
        {
            if (isFocused)
            {
                return $"\u001b[7m{line}\u001b[0m";
            }
            return line;
        }

        string prefix = isFocused ? "> " : "  ";
        return prefix + line;
    }

    private static int GetConsoleWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch
        {
            return 0;
        }
    }

    private void WritePadded(string rendered, int visibleLength, int width)
    {
        if (width <= 0)
        {
            Console.WriteLine(rendered);
            return;
        }

        Console.Write(rendered);

        if (visibleLength < width)
        {
            Console.Write(new string(' ', width - visibleLength));
        }
        Console.WriteLine();
    }

    private int GetVisibleLength(string baseText)
    {
        if (_useAnsi)
        {
            return baseText.Length;
        }

        return baseText.Length + 2; // prefixes "  " or "> "
    }

    private static void SetCursorPositionSafe(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch
        {
            // Ignore positioning failures in minimal Phase-1 implementation.
        }
    }
}
