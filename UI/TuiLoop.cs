using Gitree.App;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public sealed class TuiLoop
{
    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        if (screen is null) throw new ArgumentNullException(nameof(screen));
        statusHint ??= string.Empty;

        var lines = snapshot.PrintedLines;
        int cursor = 0;
        if (snapshot.Count > 0)
        {
            cursor = Math.Clamp(cursor, 0, snapshot.Count - 1);
        }

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        try
        {
            string status = StatusBar.Build(statusHint);
            screen.DrawLines(lines, cursor);
            screen.DrawStatus(status);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);

                switch (action)
                {
                    case UiAction.MoveUp:
                        cursor = Math.Max(0, cursor - 1);
                        break;
                    case UiAction.MoveDown:
                        if (snapshot.Count > 0)
                        {
                            cursor = Math.Min(snapshot.Count - 1, cursor + 1);
                        }
                        break;
                    case UiAction.Quit:
                        screen.DrawLines(lines, cursor);
                        screen.DrawStatus(StatusBar.Build(statusHint));
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                        break;
                }

                screen.DrawLines(lines, cursor);
                screen.DrawStatus(StatusBar.Build(statusHint));
            }
        }
        finally
        {
            Console.TreatControlCAsInput = previousTreatControlC;
        }
    }
}
