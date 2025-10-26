namespace Gitree.UI;

public sealed class Screen
{
    private readonly bool useAnsi;
    private bool initialized;
    private int baseRow;
    private int statusRow;

    public Screen(bool useAnsi)
    {
        this.useAnsi = useAnsi;
        baseRow = Console.CursorTop;
        statusRow = baseRow;
    }

    public void DrawLines(IReadOnlyList<string> lines, int cursorIndex)
    {
        if (!initialized)
        {
            int start = Console.CursorTop - lines.Count;
            baseRow = start < 0 ? 0 : start;
            statusRow = baseRow + lines.Count;
            initialized = true;
        }

        int safeCursor = cursorIndex;
        if (lines.Count == 0)
        {
            safeCursor = 0;
        }
        else if (cursorIndex < 0 || cursorIndex >= lines.Count)
        {
            safeCursor = Math.Clamp(cursorIndex, 0, lines.Count - 1);
        }

        for (int i = 0; i < lines.Count; i++)
        {
            Console.SetCursorPosition(0, baseRow + i);
            string formatted = FormatLine(lines[i], i == safeCursor);
            WriteLineContent(formatted);
        }

        if (lines.Count == 0)
        {
            Console.SetCursorPosition(0, baseRow);
        }

        statusRow = baseRow + lines.Count;
        Console.SetCursorPosition(0, statusRow);
    }

    public void DrawStatus(string text)
    {
        Console.SetCursorPosition(0, statusRow);
        WriteLineContent(text);
    }

    public void ClearBelow(int fromRow)
    {
        // Phase-1 placeholder
    }

    private string FormatLine(string text, bool focused)
    {
        if (useAnsi)
        {
            if (focused)
            {
                return "\u001b[7m" + text + "\u001b[0m";
            }
            return text;
        }

        if (focused)
        {
            return "> " + text;
        }

        return "  " + text;
    }

    private void WriteLineContent(string text)
    {
        int width = GetConsoleWidth();
        bool containsAnsi = text.IndexOf('\u001b') >= 0;

        if (containsAnsi)
        {
            string cleaned = text;
            if (width > 0)
            {
                int visible = GetVisibleLength(cleaned);
                if (visible > width)
                {
                    cleaned = TrimVisible(cleaned, width);
                    visible = width;
                }
                Console.Write(cleaned);
                if (visible < width)
                {
                    Console.Write(new string(' ', width - visible));
                }
            }
            else
            {
                Console.Write(cleaned);
            }
            return;
        }

        if (width > 0)
        {
            if (text.Length > width)
            {
                Console.Write(text.Substring(0, width));
            }
            else
            {
                Console.Write(text);
                if (text.Length < width)
                {
                    Console.Write(new string(' ', width - text.Length));
                }
            }
        }
        else
        {
            Console.Write(text);
        }
    }

    private static int GetConsoleWidth()
    {
        try
        {
            return Console.BufferWidth;
        }
        catch
        {
            return 0;
        }
    }

    private static int GetVisibleLength(string text)
    {
        int length = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\u001b')
            {
                int end = text.IndexOf('m', i);
                if (end < 0)
                {
                    break;
                }
                i = end;
                continue;
            }
            length++;
        }
        return length;
    }

    private static string TrimVisible(string text, int maxVisible)
    {
        var sb = new System.Text.StringBuilder();
        int visible = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\u001b')
            {
                int end = text.IndexOf('m', i);
                if (end < 0)
                {
                    break;
                }
                sb.Append(text.Substring(i, end - i + 1));
                i = end;
                continue;
            }

            if (visible >= maxVisible)
            {
                break;
            }

            sb.Append(text[i]);
            visible++;
        }
        return sb.ToString();
    }
}
