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
        bool originalTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        int cursor = 0;
        if (snapshot.Count > 0)
        {
            cursor = Math.Clamp(cursor, 0, snapshot.Count - 1);
        }

        var rangeIndex = new TreeRangeIndex(snapshot);

        try
        {
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
                    case UiAction.ToggleSelection:
                        HandleToggle(cursor);
                        break;
                    case UiAction.SelectAllUnder:
                        HandleSelectAll(cursor);
                        break;
                    case UiAction.ClearAllUnder:
                        HandleClearAll(cursor);
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

        void Render()
        {
            var composed = ComposeLines();
            var summary = SelectionSummary.Compute(snapshot, rangeIndex, _selection);
            screen.DrawLines(composed, cursor);
            var status = StatusBar.BuildWithSelection(statusHint, summary.SelectedFiles, summary.FullDirs, summary.PartialDirs);
            screen.DrawStatus(status);
        }

        IReadOnlyList<string> ComposeLines()
        {
            var buffer = new string[snapshot.Count];
            for (int i = 0; i < snapshot.Count; i++)
            {
                var node = snapshot.Lines[i];
                var printed = snapshot.PrintedLines[i];
                buffer[i] = LineComposer.Compose(node, printed, i, _selection, rangeIndex);
            }

            return buffer;
        }

        void HandleToggle(int index)
        {
            if (snapshot.Count == 0)
            {
                return;
            }

            if (index < 0 || index >= snapshot.Count)
            {
                return;
            }

            var node = snapshot.Lines[index];
            if (node.IsDirectory)
            {
                var coverage = rangeIndex.ComputeCoverage(index, _selection);
                if (coverage.TotalFiles == 0)
                {
                    return;
                }

                var filePaths = rangeIndex.GetDescendantFilePaths(index).ToList();
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
                ToggleFile(node.RelativePath);
            }
        }

        void HandleSelectAll(int index)
        {
            if (!HasNode(index))
            {
                return;
            }

            var node = snapshot.Lines[index];
            if (node.IsDirectory)
            {
                var filePaths = rangeIndex.GetDescendantFilePaths(index).ToList();
                _selection.SelectFiles(filePaths);
            }
            else
            {
                SelectFile(node.RelativePath);
            }
        }

        void HandleClearAll(int index)
        {
            if (!HasNode(index))
            {
                return;
            }

            var node = snapshot.Lines[index];
            if (node.IsDirectory)
            {
                var filePaths = rangeIndex.GetDescendantFilePaths(index).ToList();
                _selection.DeselectFiles(filePaths);
            }
            else
            {
                DeselectFile(node.RelativePath);
            }
        }

        bool HasNode(int index) => snapshot.Count > 0 && index >= 0 && index < snapshot.Count;

        void ToggleFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (_selection.IsFileSelected(path))
            {
                _selection.DeselectFiles(new[] { path });
            }
            else
            {
                _selection.SelectFiles(new[] { path });
            }
        }

        void SelectFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _selection.SelectFiles(new[] { path });
        }

        void DeselectFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _selection.DeselectFiles(new[] { path });
        }
    }
}
