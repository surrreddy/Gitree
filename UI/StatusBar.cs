namespace Gitree.UI;

public static class StatusBar
{
    public static string Build(string hint)
    {
        string text = hint ?? string.Empty;
        int width = GetConsoleWidth();
        if (width <= 0)
        {
            return text;
        }

        if (text.Length > width)
        {
            return text.Substring(0, width);
        }

        if (text.Length < width)
        {
            return text.PadRight(width);
        }

        return text;
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
}
