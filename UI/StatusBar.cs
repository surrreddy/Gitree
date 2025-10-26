namespace Gitree.UI;

public static class StatusBar
{
    public static string Build(string hint)
    {
        if (string.IsNullOrEmpty(hint))
        {
            return string.Empty;
        }

        try
        {
            int width = Console.WindowWidth;
            if (width <= 0)
            {
                return hint;
            }

            if (hint.Length > width)
            {
                return hint[..width];
            }

            if (hint.Length < width)
            {
                return hint + new string(' ', width - hint.Length);
            }

            return hint;
        }
        catch
        {
            return hint;
        }
    }
}
