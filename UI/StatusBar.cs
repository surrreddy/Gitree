namespace Gitree.UI;

public static class StatusBar
{
    public static string Build(string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return string.Empty;
        }

        int width = GetConsoleWidth();
        if (width <= 0)
        {
            return hint;
        }

        if (hint.Length > width)
        {
            return hint.Substring(0, width);
        }

        if (hint.Length < width)
        {
            return hint + new string(' ', width - hint.Length);
        }

        return hint;
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
