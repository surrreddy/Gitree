namespace Gitree.App;

public static class Features
{
    public static string Phase1StatusHint => "Arrows Move  Q Quit";
    public static string Phase2StatusHint => "Arrows Move  Space Toggle  A Select-All  Shift+A Clear-All  Q Quit";
    public static string Phase3StatusHint => "Arrows Move  Enter/→ Expand  ← Collapse  Space Toggle  A Select-All  Shift+A Clear-All  S Files-Only  Q Quit";

    public static bool IsPhase1Enabled(string[] args) => AppConfig.EnableTuiFromArgs(args);
}
