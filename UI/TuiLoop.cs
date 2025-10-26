using System.Collections.Generic;
using System.Linq;
using Gitree.App;
using Gitree.Core.Selection;
using Gitree.Core.Snapshot;
using Gitree.Core.View;

namespace Gitree.UI;

public sealed class TuiLoop
{
    private readonly SelectionSet _selection = new();
    private readonly ExpandState _expand;
    private readonly VisibilityEngine _visibility = new();
    private bool _filesOnly;

    public TuiLoop()
    {
        _expand = new ExpandState(AppConfig.RootExpandedByDefault);
        _filesOnly = AppConfig.FilesOnlyDefault;
    }

    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (screen == null) throw new ArgumentNullException(nameof(screen));

        var rangeIndex = new TreeRangeIndex(snapshot);
        var visibleIndexes = (IReadOnlyList<int>)Array.Empty<int>();
        int visibleCursor = 0;
        bool useUnicodeGlyphs = SupportsUnicodeGlyphs();

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;

        try
        {
            visibleIndexes = UpdateView(screen, snapshot, rangeIndex, statusHint, useUnicodeGlyphs, ref visibleCursor);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);

                switch (action)
                {
                    case UiAction.MoveUp:
                        if (visibleIndexes.Count > 0 && visibleCursor > 0)
                        {
                            visibleCursor--;
                        }
                        break;
                    case UiAction.MoveDown:
                        if (visibleIndexes.Count > 0 && visibleCursor < visibleIndexes.Count - 1)
                        {
                            visibleCursor++;
                        }
                        break;
                    case UiAction.ToggleSelection:
                        ToggleSelection(snapshot, rangeIndex, visibleIndexes, visibleCursor);
                        break;
                    case UiAction.SelectAllUnder:
                        SelectAllUnder(snapshot, rangeIndex, visibleIndexes, visibleCursor);
                        break;
                    case UiAction.ClearAllUnder:
                        ClearAllUnder(snapshot, rangeIndex, visibleIndexes, visibleCursor);
                        break;
                    case UiAction.ExpandOrEnter:
                        ExpandFocusedDirectory(snapshot, rangeIndex, visibleIndexes, visibleCursor);
                        break;
                    case UiAction.CollapseOrUp:
                        CollapseOrMoveToParent(snapshot, visibleIndexes, ref visibleCursor);
                        break;
                    case UiAction.ToggleFilesOnly:
                        _filesOnly = !_filesOnly;
                        break;
                    case UiAction.Quit:
                        visibleIndexes = UpdateView(screen, snapshot, rangeIndex, statusHint, useUnicodeGlyphs, ref visibleCursor);
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                    default:
                        break;
                }

                visibleIndexes = UpdateView(screen, snapshot, rangeIndex, statusHint, useUnicodeGlyphs, ref visibleCursor);
            }
        }
        finally
        {
            Console.TreatControlCAsInput = previousTreatControlC;
        }
    }

    private IReadOnlyList<int> UpdateView(
        Screen screen,
        TreeSnapshot snapshot,
        TreeRangeIndex rangeIndex,
        string statusHint,
        bool useUnicodeGlyphs,
        ref int visibleCursor)
    {
        var visibleIndexes = _visibility.BuildVisibleLineIndexes(snapshot, _expand, _filesOnly, rangeIndex);

        if (visibleIndexes.Count == 0)
        {
            visibleCursor = -1;
        }
        else
        {
            if (visibleCursor < 0)
            {
                visibleCursor = 0;
            }
            else if (visibleCursor >= visibleIndexes.Count)
            {
                visibleCursor = visibleIndexes.Count - 1;
            }
        }

        var composed = ComposeVisibleLines(snapshot, rangeIndex, visibleIndexes, useUnicodeGlyphs);
        var summary = ComputeVisibleSummary(snapshot, visibleIndexes);

        screen.DrawLines(composed, visibleCursor);
        screen.DrawStatus(StatusBar.BuildPhase3(statusHint, summary.SelectedFiles, summary.FullDirs, summary.PartialDirs));

        return visibleIndexes;
    }

    private IReadOnlyList<string> ComposeVisibleLines(
        TreeSnapshot snapshot,
        TreeRangeIndex rangeIndex,
        IReadOnlyList<int> visibleIndexes,
        bool useUnicodeGlyphs)
    {
        if (visibleIndexes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var buffer = new List<string>(visibleIndexes.Count);
        foreach (var index in visibleIndexes)
        {
            var node = snapshot.Lines[index];
            bool hasDescendants = node.IsDirectory && rangeIndex.GetDescendantLineIndexes(index).Count > 0;
            bool isExpanded = node.IsDirectory && _expand.IsExpanded(index);
            buffer.Add(LineComposer.Compose(node, index, _selection, rangeIndex, hasDescendants, isExpanded, useUnicodeGlyphs));
        }

        return buffer;
    }

    private VisibleSummary ComputeVisibleSummary(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes)
    {
        if (visibleIndexes.Count == 0)
        {
            return new VisibleSummary(0, 0, 0);
        }

        int selectedFiles = 0;
        int fullDirs = 0;
        int partialDirs = 0;

        var stack = new List<DirAccumulator>();

        foreach (var index in visibleIndexes)
        {
            var node = snapshot.Lines[index];

            while (stack.Count > 0 && stack[^1].Depth >= node.Depth)
            {
                var completed = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                if (completed.TotalFiles > 0)
                {
                    if (completed.SelectedFiles == completed.TotalFiles)
                    {
                        fullDirs++;
                    }
                    else if (completed.SelectedFiles > 0)
                    {
                        partialDirs++;
                    }
                }
            }

            if (node.IsDirectory)
            {
                stack.Add(new DirAccumulator(node.Depth));
                continue;
            }

            bool isSelected = _selection.IsFileSelected(node.RelativePath);
            if (isSelected)
            {
                selectedFiles++;
            }

            for (int i = 0; i < stack.Count; i++)
            {
                var acc = stack[i];
                acc.TotalFiles++;
                if (isSelected)
                {
                    acc.SelectedFiles++;
                }
                stack[i] = acc;
            }
        }

        for (int i = stack.Count - 1; i >= 0; i--)
        {
            var completed = stack[i];
            if (completed.TotalFiles > 0)
            {
                if (completed.SelectedFiles == completed.TotalFiles)
                {
                    fullDirs++;
                }
                else if (completed.SelectedFiles > 0)
                {
                    partialDirs++;
                }
            }
        }

        return new VisibleSummary(selectedFiles, fullDirs, partialDirs);
    }

    private void ToggleSelection(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, IReadOnlyList<int> visibleIndexes, int visibleCursor)
    {
        int lineIndex = GetFocusedLineIndex(visibleIndexes, visibleCursor);
        if (lineIndex < 0)
        {
            return;
        }

        var node = snapshot.Lines[lineIndex];
        if (node.IsDirectory)
        {
            var coverage = rangeIndex.ComputeCoverage(lineIndex, _selection);
            if (coverage.TotalFiles == 0)
            {
                return;
            }

            var filePaths = rangeIndex.GetDescendantFilePaths(lineIndex).ToList();
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

    private void SelectAllUnder(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, IReadOnlyList<int> visibleIndexes, int visibleCursor)
    {
        int lineIndex = GetFocusedLineIndex(visibleIndexes, visibleCursor);
        if (lineIndex < 0)
        {
            return;
        }

        var node = snapshot.Lines[lineIndex];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(lineIndex).ToList();
            _selection.SelectFiles(filePaths);
        }
        else
        {
            _selection.SelectFiles(new[] { node.RelativePath });
        }
    }

    private void ClearAllUnder(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, IReadOnlyList<int> visibleIndexes, int visibleCursor)
    {
        int lineIndex = GetFocusedLineIndex(visibleIndexes, visibleCursor);
        if (lineIndex < 0)
        {
            return;
        }

        var node = snapshot.Lines[lineIndex];
        if (node.IsDirectory)
        {
            var filePaths = rangeIndex.GetDescendantFilePaths(lineIndex).ToList();
            _selection.DeselectFiles(filePaths);
        }
        else
        {
            _selection.DeselectFiles(new[] { node.RelativePath });
        }
    }

    private void ExpandFocusedDirectory(TreeSnapshot snapshot, TreeRangeIndex rangeIndex, IReadOnlyList<int> visibleIndexes, int visibleCursor)
    {
        int lineIndex = GetFocusedLineIndex(visibleIndexes, visibleCursor);
        if (lineIndex < 0)
        {
            return;
        }

        var node = snapshot.Lines[lineIndex];
        if (!node.IsDirectory)
        {
            return;
        }

        if (!_expand.IsExpanded(lineIndex) && rangeIndex.GetDescendantLineIndexes(lineIndex).Count > 0)
        {
            _expand.Expand(lineIndex);
        }
    }

    private void CollapseOrMoveToParent(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes, ref int visibleCursor)
    {
        int lineIndex = GetFocusedLineIndex(visibleIndexes, visibleCursor);
        if (lineIndex < 0)
        {
            return;
        }

        var node = snapshot.Lines[lineIndex];
        if (node.IsDirectory && _expand.IsExpanded(lineIndex) && lineIndex != 0)
        {
            _expand.Collapse(lineIndex);
            return;
        }

        var parentCursor = FindParentVisibleDirectoryCursor(snapshot, visibleIndexes, visibleCursor, node.Depth);
        if (parentCursor.HasValue)
        {
            visibleCursor = parentCursor.Value;
        }
    }

    private static int? FindParentVisibleDirectoryCursor(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes, int visibleCursor, int currentDepth)
    {
        if (visibleIndexes.Count == 0)
        {
            return null;
        }

        int searchStart = Math.Min(Math.Max(visibleCursor, 0), visibleIndexes.Count - 1) - 1;
        for (int i = searchStart; i >= 0; i--)
        {
            int candidateIndex = visibleIndexes[i];
            var candidateNode = snapshot.Lines[candidateIndex];
            if (!candidateNode.IsDirectory)
            {
                continue;
            }

            if (candidateNode.Depth < currentDepth)
            {
                return i;
            }
        }

        return null;
    }

    private int GetFocusedLineIndex(IReadOnlyList<int> visibleIndexes, int visibleCursor)
    {
        if (visibleIndexes.Count == 0 || visibleCursor < 0)
        {
            return -1;
        }

        if (visibleCursor >= visibleIndexes.Count)
        {
            return visibleIndexes[^1];
        }

        return visibleIndexes[visibleCursor];
    }

    private static bool SupportsUnicodeGlyphs()
    {
        try
        {
            return Console.OutputEncoding.CodePage == 65001;
        }
        catch
        {
            return true;
        }
    }

    private readonly struct VisibleSummary
    {
        public VisibleSummary(int selectedFiles, int fullDirs, int partialDirs)
        {
            SelectedFiles = selectedFiles;
            FullDirs = fullDirs;
            PartialDirs = partialDirs;
        }

        public int SelectedFiles { get; }
        public int FullDirs { get; }
        public int PartialDirs { get; }
    }

    private struct DirAccumulator
    {
        public DirAccumulator(int depth)
        {
            Depth = depth;
            TotalFiles = 0;
            SelectedFiles = 0;
        }

        public int Depth { get; }
        public int TotalFiles { get; set; }
        public int SelectedFiles { get; set; }
    }
}
