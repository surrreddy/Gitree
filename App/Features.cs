namespace Gitree.App;

public static class Features
{
    public static string Phase1StatusHint => "Arrows Move  Q Quit";

    public static bool IsPhase1Enabled(string[] args) => AppConfig.EnableTuiFromArgs(args);
}
