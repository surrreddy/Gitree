namespace Gitree.UI;

public static class StatusBar
{
    public static string Build(string hint)
    {
        string text = hint ?? string.Empty;
        return FitToConsoleWidth(text);
    }

    public static string BuildWithSelection(string hint, int files, int fullDirs, int partialDirs)
    {
        string baseText = $"{hint ?? string.Empty}    Selected: {files} ({fullDirs} full, {partialDirs} partial)";
        return FitToConsoleWidth(baseText);
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

    private static string FitToConsoleWidth(string text)
    {
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
}
