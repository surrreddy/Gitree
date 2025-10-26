using Gitree.App;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public sealed class TuiLoop
{
    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        bool originalTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        int cursor = 0;
        if (snapshot.Count > 0)
        {
            cursor = Math.Clamp(cursor, 0, snapshot.Count - 1);
        }

        try
        {
            void Render()
            {
                screen.DrawLines(snapshot.PrintedLines, cursor);
                var status = StatusBar.Build(statusHint);
                screen.DrawStatus(status);
            }

            Render();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);
                switch (action)
                {
                    case UiAction.MoveUp:
                        if (snapshot.Count > 0)
                        {
                            cursor = Math.Max(0, cursor - 1);
                        }
                        break;
                    case UiAction.MoveDown:
                        if (snapshot.Count > 0)
                        {
                            cursor = Math.Min(snapshot.Count - 1, cursor + 1);
                        }
                        break;
                    case UiAction.Quit:
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                    default:
                        break;
                }

                Render();
            }
        }
        finally
        {
            Console.TreatControlCAsInput = originalTreatControlC;
        }
    }
}
