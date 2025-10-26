namespace Gitree.UI;

public sealed class Screen
{
    private readonly bool _useAnsi;

    public Screen(bool useAnsi)
    {
        _useAnsi = useAnsi;
    }

    public void DrawLines(IReadOnlyList<string> lines, int cursorIndex)
    {
        Console.Clear();
        for (int i = 0; i < lines.Count; i++)
        {
            bool isFocused = i == cursorIndex;
            if (_useAnsi)
            {
                if (isFocused)
                {
                    Console.Write("\u001b[7m");
                    Console.Write(lines[i]);
                    Console.WriteLine("\u001b[0m");
                }
                else
                {
                    Console.WriteLine(lines[i]);
                }
            }
            else
            {
                string prefix = isFocused ? "> " : "  ";
                Console.WriteLine(prefix + lines[i]);
            }
        }
    }

    public void DrawStatus(string text)
    {
        Console.WriteLine(text);
    }

    public void ClearBelow(int fromRow)
    {
        // Phase-1 placeholder.
    }
}
