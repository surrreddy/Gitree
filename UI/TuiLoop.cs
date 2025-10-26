using System.Collections.Generic;
using System.Linq;
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
        int cursor = 0;
        if (!snapshot.IsEmpty)
        {
            cursor = Math.Clamp(cursor, 0, snapshot.Count - 1);
        }

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;
        try
        {
            Redraw(screen, snapshot, rangeIndex, cursor, statusHint);

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
                        Redraw(screen, snapshot, rangeIndex, cursor, statusHint);
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                    default:
                        break;
                }

                Redraw(screen, snapshot, rangeIndex, cursor, statusHint);
            }
        }
        finally
        {
            Console.TreatControlCAsInput = previousTreatControlC;
        }
    }

    private void Redraw(Screen screen, TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor, string statusHint)
    {
        var composedLines = ComposeLines(snapshot, rangeIndex);
        var summary = SelectionSummary.Compute(snapshot, rangeIndex, _selection);
        screen.DrawLines(composedLines, cursor);
        screen.DrawStatus(StatusBar.BuildWithSelection(statusHint, summary.SelectedFiles, summary.FullDirs, summary.PartialDirs));
    }

    private IReadOnlyList<string> ComposeLines(TreeSnapshot snapshot, TreeRangeIndex rangeIndex)
    {
        if (snapshot.IsEmpty)
        {
            return Array.Empty<string>();
        }

        var buffer = new List<string>(snapshot.Count);
        for (int i = 0; i < snapshot.Count; i++)
        {
            var node = snapshot.Lines[i];
            buffer.Add(LineComposer.Compose(node, i, _selection, rangeIndex));
        }

        return buffer;
    }

    private void ToggleSelection(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor)
    {
        if (snapshot.IsEmpty || cursor < 0 || cursor >= snapshot.Count)
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

            var filePaths = rangeIndex.GetDescendantFilePaths(cursor).ToList();
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
        if (snapshot.IsEmpty || cursor < 0 || cursor >= snapshot.Count)
        {
            return;
        }

        var node = snapshot.Lines[cursor];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(cursor).ToList();
            _selection.SelectFiles(filePaths);
        }
        else
        {
            _selection.SelectFiles(new[] { node.RelativePath });
        }
    }

    private void ClearAllUnder(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, int cursor)
    {
        if (snapshot.IsEmpty || cursor < 0 || cursor >= snapshot.Count)
        {
            return;
        }

        var node = snapshot.Lines[cursor];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(cursor).ToList();
            _selection.DeselectFiles(filePaths);
        }
        else
        {
            _selection.DeselectFiles(new[] { node.RelativePath });
        }
    }
}
