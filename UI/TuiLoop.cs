using Gitree.App;
using Gitree.Core.Selection;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public sealed class TuiLoop
{
    private readonly SelectionSet _selection = new();

    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (screen == null) throw new ArgumentNullException(nameof(screen));

        var rangeIndex = new TreeRangeIndex(snapshot);
        int cursor = snapshot.Count > 0 ? 0 : -1;

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        try
        {
            Redraw(screen, snapshot, rangeIndex, statusHint, cursor);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);

                switch (action)
                {
                    case UiAction.MoveUp:
                        if (snapshot.Count > 0)
                        {
                            cursor = cursor <= 0 ? 0 : cursor - 1;
                        }
                        break;
                    case UiAction.MoveDown:
                        if (snapshot.Count > 0)
                        {
                            cursor = cursor < 0 ? 0 : Math.Min(snapshot.Count - 1, cursor + 1);
                        }
                        break;
                    case UiAction.ToggleSelection:
                        ToggleSelection(snapshot, rangeIndex, cursor);
                        break;
                    case UiAction.SelectAllUnder:
                        SelectAllUnder(snapshot, rangeIndex, cursor);
                        break;
                    case UiAction.ClearAllUnder:
                        ClearAllUnder(snapshot, rangeIndex, cursor);
                        break;
                    case UiAction.Quit:
                        Redraw(screen, snapshot, rangeIndex, statusHint, cursor);
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                    default:
                        break;
                }

                cursor = ClampCursor(cursor, snapshot);
                Redraw(screen, snapshot, rangeIndex, statusHint, cursor);
            }
        }
        finally
        {
            Console.TreatControlCAsInput = previousTreatControlC;
        }
    }

    private static int ClampCursor(int cursor, TreeSnapshot snapshot)
    {
        if (snapshot.Count == 0)
        {
            return -1;
        }

        if (cursor < 0)
        {
            return 0;
        }

        if (cursor >= snapshot.Count)
        {
            return snapshot.Count - 1;
        }

        return cursor;
    }

    private void ToggleSelection(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor)
    {
        if (cursor < 0 || cursor >= snapshot.Count)
        {
            return;
        }

        var node = snapshot.Lines[cursor];
        if (node.IsDirectory)
        {
            var coverage = rangeIndex.ComputeCoverage(cursor, _selection);
            if (coverage.TotalFiles == 0)
            {
                return;
            }

            var filePaths = rangeIndex.GetDescendantFilePaths(cursor);
            if (coverage.SelectedFiles == coverage.TotalFiles)
            {
                _selection.DeselectFiles(filePaths);
            }
            else
            {
                _selection.SelectFiles(filePaths);
            }
        }
        else
        {
            if (_selection.IsFileSelected(node.RelativePath))
            {
                _selection.DeselectFiles(new[] { node.RelativePath });
            }
            else
            {
                _selection.SelectFiles(new[] { node.RelativePath });
            }
        }
    }

    private void SelectAllUnder(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor)
    {
        if (cursor < 0 || cursor >= snapshot.Count)
        {
            return;
        }

        var node = snapshot.Lines[cursor];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(cursor);
            _selection.SelectFiles(filePaths);
        }
        else
        {
            _selection.SelectFiles(new[] { node.RelativePath });
        }
    }

    private void ClearAllUnder(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor)
    {
        if (cursor < 0 || cursor >= snapshot.Count)
        {
            return;
        }

        var node = snapshot.Lines[cursor];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(cursor);
            _selection.DeselectFiles(filePaths);
        }
        else
        {
            _selection.DeselectFiles(new[] { node.RelativePath });
        }
    }

    private void Redraw(Screen screen, TreeSnapshot snapshot, TreeRangeIndex rangeIndex, string statusHint, int cursor)
    {
        var lines = ComposeLines(snapshot, rangeIndex);
        int highlight = cursor;
        if (lines.Count == 0)
        {
            highlight = -1;
        }
        else if (highlight < 0)
        {
            highlight = 0;
        }
        else if (highlight >= lines.Count)
        {
            highlight = lines.Count - 1;
        }

        screen.DrawLines(lines, highlight);

        var summary = SelectionSummary.Compute(snapshot, rangeIndex, _selection);
        string status = StatusBar.BuildWithSelection(statusHint, summary.SelectedFiles, summary.FullDirs, summary.PartialDirs);
        screen.DrawStatus(status);
    }

    private IReadOnlyList<string> ComposeLines(TreeSnapshot snapshot, TreeRangeIndex rangeIndex)
    {
        if (snapshot.Count == 0)
        {
            return Array.Empty<string>();
        }

        var buffer = new string[snapshot.Count];
        for (int i = 0; i < snapshot.Count; i++)
        {
            var node = snapshot.Lines[i];
            buffer[i] = LineComposer.Compose(node, i, _selection, rangeIndex);
        }

        return buffer;
    }
}
