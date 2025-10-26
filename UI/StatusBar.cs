namespace Gitree.UI;

public static class StatusBar
{
    public static string Build(string hint)
    {
        return FormatLine(hint);
    }

    public static string BuildWithSelection(string hint, int files, int fullDirs, int partialDirs)
    {
        string composed = string.IsNullOrEmpty(hint)
            ? $"Selected: {files} ({fullDirs} full, {partialDirs} partial)"
            : $"{hint}    Selected: {files} ({fullDirs} full, {partialDirs} partial)";

        return FormatLine(composed);
    }

    private static string FormatLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        try
        {
            int width = Console.WindowWidth;
            if (width <= 0)
            {
                return text;
            }

            if (text.Length > width)
            {
                return text[..width];
            }

            if (text.Length < width)
            {
                return text + new string(' ', width - text.Length);
            }

            return text;
        }
        catch
        {
            return text;
        }
    }
}
