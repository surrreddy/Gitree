using System.Text;

namespace Gitree.App;

public static class AppConfig
{
    public static readonly int ExitOk = 0;
    public static readonly int ExitErr = 1;
    public static readonly int ExitMissingGitIgnore = 2;
    public static readonly int ExitInterrupted = 130;
    public static readonly int SelectAllSoftLimit = 5000;
    public const string ExportFileName = "requested_src_files.txt";
    public static readonly Encoding ExportEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public static readonly string SectionSeparator = "---";

    public static bool EnableTuiFromArgs(string[] args)
    {
        if (args == null || args.Length == 0)
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
        if (OperatingSystem.IsWindows())
        {
            return OperatingSystem.IsWindowsVersionAtLeast(10);
        }

        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(term))
        {
            return false;
        }

        return !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase);
    }
}
