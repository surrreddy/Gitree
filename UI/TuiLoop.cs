using Gitree.App;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public sealed class TuiLoop
{
    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (screen == null) throw new ArgumentNullException(nameof(screen));

        int cursor = 0;
        if (!snapshot.IsEmpty)
        {
            cursor = Math.Clamp(cursor, 0, snapshot.Count - 1);
        }

        var lines = snapshot.Lines.Select(l => l.PrintedText).ToList();

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;
        try
        {
            screen.DrawLines(lines, cursor);
            screen.DrawStatus(StatusBar.Build(statusHint));

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);

                switch (action)
                {
                    case UiAction.MoveUp:
                        if (!snapshot.IsEmpty)
                        {
                            cursor = Math.Max(0, cursor - 1);
                        }
                        break;
                    case UiAction.MoveDown:
                        if (!snapshot.IsEmpty)
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
                    default:
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
