namespace Gitree.App;

public static class AppConfig
{
    public static readonly int ExitOk = 0;
    public static readonly int ExitErr = 1;
    public static readonly int ExitMissingGitIgnore = 2;
    public static readonly int ExitInterrupted = 130;

    public static bool EnableTuiFromArgs(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return false;
        }

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--ui", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ConsoleSupportsBasicAnsi()
    {
        if (Console.IsOutputRedirected)
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            {
                return false;
            }

            return true;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            return true;
        }

        return false;
    }
}
