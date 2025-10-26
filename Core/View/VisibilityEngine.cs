using System;
using System.Collections.Generic;
using Gitree.Core.Selection;
using Gitree.Core.Snapshot;

namespace Gitree.Core.View;

public static class VisibilityEngine
{
    public static IReadOnlyList<int> BuildVisibleLineIndexes(TreeSnapshot snapshot, ExpandState expandState, bool filesOnly, TreeRangeIndex rangeIndex)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (expandState == null) throw new ArgumentNullException(nameof(expandState));
        if (rangeIndex == null) throw new ArgumentNullException(nameof(rangeIndex));

        var result = new List<int>();
        if (snapshot.IsEmpty)
        {
            return result;
        }

        var lines = snapshot.Lines;
        var ancestorCollapsed = new Stack<bool>();

        for (int i = 0; i < lines.Count; i++)
        {
            var node = lines[i];

            while (ancestorCollapsed.Count > node.Depth)
            {
                ancestorCollapsed.Pop();
            }

            bool hasCollapsedAncestor = ancestorCollapsed.Count > 0 && ancestorCollapsed.Peek();
            bool isVisible = !hasCollapsedAncestor;
            bool isRoot = i == 0;

            if (isRoot)
            {
                isVisible = true;
            }
            else if (isVisible && filesOnly && node.IsDirectory)
            {
                isVisible = DirectoryHasVisibleFiles(i, snapshot, expandState, rangeIndex, filesOnly);
            }

            if (isVisible)
            {
                result.Add(i);
            }

            if (node.IsDirectory)
            {
                bool collapsedForDescendants = hasCollapsedAncestor || !expandState.IsExpanded(i);
                ancestorCollapsed.Push(collapsedForDescendants);
            }
        }

        return result;
    }

    public static bool DirectoryHasVisibleFiles(int dirLineIndex, TreeSnapshot snapshot, ExpandState expandState, TreeRangeIndex rangeIndex, bool filesOnly)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (expandState == null) throw new ArgumentNullException(nameof(expandState));
        if (rangeIndex == null) throw new ArgumentNullException(nameof(rangeIndex));
        _ = filesOnly; // visibility determination already accounts for files-only semantics via expansion state

        if (dirLineIndex < 0 || dirLineIndex >= snapshot.Count)
        {
            return false;
        }

        var dirNode = snapshot.Lines[dirLineIndex];
        if (!dirNode.IsDirectory)
        {
            return false;
        }

        if (!expandState.IsExpanded(dirLineIndex))
        {
            return false;
        }

        int baseDepth = dirNode.Depth;
        var lines = snapshot.Lines;
        var pathStates = new Stack<bool>();
        pathStates.Push(true);

        var descendants = rangeIndex.GetDescendantLineIndexes(dirLineIndex);
        foreach (var index in descendants)
        {
            var node = lines[index];
            int relativeDepth = Math.Max(0, node.Depth - baseDepth);

            while (pathStates.Count > relativeDepth)
            {
                pathStates.Pop();
            }

            bool ancestorsExpanded = pathStates.Count == 0 || pathStates.Peek();
            if (!ancestorsExpanded)
            {
                if (node.IsDirectory)
                {
                    pathStates.Push(false);
                }
                continue;
            }

            if (node.IsDirectory)
            {
                bool expanded = expandState.IsExpanded(index);
                pathStates.Push(expanded);
                continue;
            }

            return true;
        }

        return false;
    }
}
