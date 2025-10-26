using System;
using System.Collections.Generic;
using Gitree.App;
using Gitree.Core.Selection;
using Gitree.Core.Snapshot;
using Gitree.Core.View;

namespace Gitree.UI;

public sealed class TuiLoop
{
    private readonly SelectionSet _selection = new();
    private readonly ExpandState _expandState = new(AppConfig.RootExpandedByDefault);
    private TreeRangeIndex? _rangeIndex;
    private bool _filesOnly = AppConfig.FilesOnlyDefault;
    private bool _useUnicodeGlyphs;

    public int Run(TreeSnapshot snapshot, Screen screen, string statusHint)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (screen == null) throw new ArgumentNullException(nameof(screen));

        _rangeIndex = new TreeRangeIndex(snapshot);
        _filesOnly = AppConfig.FilesOnlyDefault;
        _useUnicodeGlyphs = ShouldUseUnicodeGlyphs(snapshot);

        int focusedIndex = snapshot.IsEmpty ? -1 : 0;

        bool previousTreatControlC = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;
        try
        {
            var context = Redraw(screen, snapshot, statusHint, focusedIndex);
            focusedIndex = context.FocusedIndex;

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                var action = KeyBindings.Map(key);

                bool needsRedraw = true;

                switch (action)
                {
                    case UiAction.MoveUp:
                        focusedIndex = MoveFocus(context, -1, focusedIndex);
                        break;
                    case UiAction.MoveDown:
                        focusedIndex = MoveFocus(context, 1, focusedIndex);
                        break;
                    case UiAction.ToggleSelection:
                        ToggleSelection(snapshot, focusedIndex);
                        break;
                    case UiAction.SelectAllUnder:
                        SelectAllUnder(snapshot, focusedIndex);
                        break;
                    case UiAction.ClearAllUnder:
                        ClearAllUnder(snapshot, focusedIndex);
                        break;
                    case UiAction.ExpandOrEnter:
                        HandleExpand(snapshot, focusedIndex);
                        break;
                    case UiAction.CollapseOrUp:
                        focusedIndex = HandleCollapseOrUp(snapshot, context, focusedIndex);
                        break;
                    case UiAction.ToggleFilesOnly:
                        _filesOnly = !_filesOnly;
                        break;
                    case UiAction.Quit:
                        return AppConfig.ExitOk;
                    case UiAction.Interrupt:
                        return AppConfig.ExitInterrupted;
                    case UiAction.NoOp:
                    default:
                        needsRedraw = false;
                        break;
                }

                if (needsRedraw)
                {
                    context = Redraw(screen, snapshot, statusHint, focusedIndex);
                    focusedIndex = context.FocusedIndex;
                }
            }
        }
        finally
        {
            Console.TreatControlCAsInput = previousTreatControlC;
        }
    }

    private VisibleContext Redraw(Screen screen, TreeSnapshot snapshot, string statusHint, int desiredFocusIndex)
    {
        var rangeIndex = GetRangeIndex();
        var visibleIndexes = VisibilityEngine.BuildVisibleLineIndexes(snapshot, _expandState, _filesOnly, rangeIndex);

        int focusIndex = desiredFocusIndex;
        int cursorPosition = -1;

        if (visibleIndexes.Count > 0)
        {
            if (focusIndex < 0)
            {
                focusIndex = visibleIndexes[0];
                cursorPosition = 0;
            }
            else
            {
                cursorPosition = IndexOf(visibleIndexes, focusIndex);
                if (cursorPosition < 0)
                {
                    int nextPosition = FindNextVisibleIndex(visibleIndexes, focusIndex);
                    if (nextPosition >= 0)
                    {
                        focusIndex = visibleIndexes[nextPosition];
                        cursorPosition = nextPosition;
                    }
                    else
                    {
                        focusIndex = visibleIndexes[visibleIndexes.Count - 1];
                        cursorPosition = visibleIndexes.Count - 1;
                    }
                }
            }
        }
        else
        {
            focusIndex = -1;
            cursorPosition = -1;
        }

        var composedLines = ComposeVisibleLines(snapshot, visibleIndexes);
        var summary = ComputeVisibleSummary(snapshot, visibleIndexes);
        screen.DrawLines(composedLines, cursorPosition);
        screen.DrawStatus(StatusBar.BuildPhase3(statusHint, summary.SelectedFiles, summary.FullDirs, summary.PartialDirs));

        return new VisibleContext(visibleIndexes, focusIndex, cursorPosition);
    }

    private IReadOnlyList<string> ComposeVisibleLines(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes)
    {
        if (visibleIndexes.Count == 0)
        {
            return Array.Empty<string>();
        }

        var rangeIndex = GetRangeIndex();
        var buffer = new List<string>(visibleIndexes.Count);
        foreach (var index in visibleIndexes)
        {
            var node = snapshot.Lines[index];
            bool hasDescendants = node.IsDirectory && HasDescendants(index);
            bool isExpanded = node.IsDirectory && _expandState.IsExpanded(index);
            string printed = node.PrintedText ?? string.Empty;
            string composed = LineComposer.Compose(
                node,
                index,
                printed,
                _selection,
                rangeIndex,
                hasDescendants,
                isExpanded,
                _useUnicodeGlyphs);
            buffer.Add(composed);
        }

        return buffer;
    }

    private SelectionSummary ComputeVisibleSummary(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes)
    {
        if (visibleIndexes.Count == 0)
        {
            return new SelectionSummary(0, 0, 0);
        }

        int selectedFiles = 0;
        int fullDirs = 0;
        int partialDirs = 0;

        var lines = snapshot.Lines;
        var dirStack = new Stack<DirectoryTally>();

        foreach (var index in visibleIndexes)
        {
            var node = lines[index];

            while (dirStack.Count > 0 && dirStack.Peek().Depth >= node.Depth)
            {
                var completed = dirStack.Pop();
                ProcessCompletedDirectory(completed, ref fullDirs, ref partialDirs);
            }

            if (node.IsDirectory)
            {
                dirStack.Push(new DirectoryTally(node.Depth));
                continue;
            }

            bool isSelected = _selection.IsFileSelected(node.RelativePath);
            if (isSelected)
            {
                selectedFiles++;
            }

            foreach (var directory in dirStack)
            {
                directory.TotalFiles++;
                if (isSelected)
                {
                    directory.SelectedFiles++;
                }
            }
        }

        while (dirStack.Count > 0)
        {
            var completed = dirStack.Pop();
            ProcessCompletedDirectory(completed, ref fullDirs, ref partialDirs);
        }

        return new SelectionSummary(selectedFiles, fullDirs, partialDirs);
    }

    private static void ProcessCompletedDirectory(DirectoryTally tally, ref int fullDirs, ref int partialDirs)
    {
        if (tally.TotalFiles == 0)
        {
            return;
        }

        if (tally.SelectedFiles == tally.TotalFiles)
        {
            fullDirs++;
        }
        else if (tally.SelectedFiles > 0)
        {
            partialDirs++;
        }
    }

    private int MoveFocus(VisibleContext context, int delta, int currentFocus)
    {
        if (context.VisibleIndexes.Count == 0)
        {
            return -1;
        }

        int position = context.CursorPosition;
        if (position < 0)
        {
            position = delta >= 0 ? 0 : context.VisibleIndexes.Count - 1;
        }
        else
        {
            position = Math.Clamp(position + delta, 0, context.VisibleIndexes.Count - 1);
        }

        return context.VisibleIndexes[position];
    }

    private void ToggleSelection(TreeSnapshot snapshot, int focusedIndex)
    {
        if (!IsValidIndex(snapshot, focusedIndex))
        {
            return;
        }

        var rangeIndex = GetRangeIndex();
        var node = snapshot.Lines[focusedIndex];
        if (node.IsDirectory)
        {
            var coverage = rangeIndex.ComputeCoverage(focusedIndex, _selection);
            if (coverage.TotalFiles == 0)
            {
                return;
            }

            var filePaths = rangeIndex.GetDescendantFilePaths(focusedIndex);
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

    private void SelectAllUnder(TreeSnapshot snapshot, int focusedIndex)
    {
        if (!IsValidIndex(snapshot, focusedIndex))
        {
            return;
        }

        var rangeIndex = GetRangeIndex();
        var node = snapshot.Lines[focusedIndex];
        if (node.IsDirectory)
        {
            _selection.SelectFiles(rangeIndex.GetDescendantFilePaths(focusedIndex));
        }
        else
        {
            _selection.SelectFiles(new[] { node.RelativePath });
        }
    }

    private void ClearAllUnder(TreeSnapshot snapshot, int focusedIndex)
    {
        if (!IsValidIndex(snapshot, focusedIndex))
        {
            return;
        }

        var rangeIndex = GetRangeIndex();
        var node = snapshot.Lines[focusedIndex];
        if (node.IsDirectory)
        {
            _selection.DeselectFiles(rangeIndex.GetDescendantFilePaths(focusedIndex));
        }
        else
        {
            _selection.DeselectFiles(new[] { node.RelativePath });
        }
    }

    private void HandleExpand(TreeSnapshot snapshot, int focusedIndex)
    {
        if (!IsValidIndex(snapshot, focusedIndex))
        {
            return;
        }

        var node = snapshot.Lines[focusedIndex];
        if (!node.IsDirectory)
        {
            return;
        }

        if (!HasDescendants(focusedIndex))
        {
            return;
        }

        if (!_expandState.IsExpanded(focusedIndex))
        {
            _expandState.Expand(focusedIndex);
        }
    }

    private int HandleCollapseOrUp(TreeSnapshot snapshot, VisibleContext context, int focusedIndex)
    {
        if (!IsValidIndex(snapshot, focusedIndex))
        {
            return focusedIndex;
        }

        if (focusedIndex == 0)
        {
            return focusedIndex;
        }

        var node = snapshot.Lines[focusedIndex];
        if (node.IsDirectory && _expandState.IsExpanded(focusedIndex) && HasDescendants(focusedIndex))
        {
            _expandState.Collapse(focusedIndex);
            return focusedIndex;
        }

        return FocusParent(snapshot, context.VisibleIndexes, context.CursorPosition, focusedIndex);
    }

    private static int FocusParent(TreeSnapshot snapshot, IReadOnlyList<int> visibleIndexes, int cursorPosition, int currentFocus)
    {
        if (visibleIndexes.Count == 0 || cursorPosition < 0)
        {
            return currentFocus;
        }

        int currentDepth = snapshot.Lines[currentFocus].Depth;
        for (int i = cursorPosition - 1; i >= 0; i--)
        {
            var candidateIndex = visibleIndexes[i];
            var candidate = snapshot.Lines[candidateIndex];
            if (candidate.IsDirectory && candidate.Depth < currentDepth)
            {
                return candidateIndex;
            }
        }

        return currentFocus;
    }

    private bool HasDescendants(int lineIndex)
    {
        var rangeIndex = GetRangeIndex();
        var descendants = rangeIndex.GetDescendantLineIndexes(lineIndex);
        return descendants.Count > 0;
    }

    private TreeRangeIndex GetRangeIndex()
    {
        return _rangeIndex ?? throw new InvalidOperationException("Range index not initialized.");
    }

    private static bool IsValidIndex(TreeSnapshot snapshot, int index)
    {
        return index >= 0 && index < snapshot.Count;
    }

    private static int FindNextVisibleIndex(IReadOnlyList<int> visibleIndexes, int focusIndex)
    {
        for (int i = 0; i < visibleIndexes.Count; i++)
        {
            if (visibleIndexes[i] > focusIndex)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOf(IReadOnlyList<int> values, int target)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == target)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ShouldUseUnicodeGlyphs(TreeSnapshot snapshot)
    {
        foreach (var node in snapshot.Lines)
        {
            var text = node.PrintedText;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (text.IndexOf('│') >= 0 || text.IndexOf('└') >= 0 || text.IndexOf('├') >= 0)
            {
                return true;
            }

            if (text.Contains("|--") || text.Contains("`--"))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class VisibleContext
    {
        public VisibleContext(IReadOnlyList<int> visibleIndexes, int focusedIndex, int cursorPosition)
        {
            VisibleIndexes = visibleIndexes;
            FocusedIndex = focusedIndex;
            CursorPosition = cursorPosition;
        }

        public IReadOnlyList<int> VisibleIndexes { get; }
        public int FocusedIndex { get; }
        public int CursorPosition { get; }
    }

    private sealed class DirectoryTally
    {
        public DirectoryTally(int depth)
        {
            Depth = depth;
        }

        public int Depth { get; }
        public int TotalFiles { get; set; }
        public int SelectedFiles { get; set; }
    }
}
