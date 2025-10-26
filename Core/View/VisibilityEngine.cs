using Gitree.Core.Selection;
using Gitree.Core.Snapshot;

namespace Gitree.Core.View;

public sealed class VisibilityEngine
{
    public IReadOnlyList<int> BuildVisibleLineIndexes(TreeSnapshot snapshot, ExpandState expandState, bool filesOnly, TreeRangeIndex rangeIndex)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (expandState == null)
        {
            throw new ArgumentNullException(nameof(expandState));
        }

        if (rangeIndex == null)
        {
            throw new ArgumentNullException(nameof(rangeIndex));
        }

        if (snapshot.IsEmpty)
        {
            return Array.Empty<int>();
        }

        var visible = new List<int>(snapshot.Count);
        for (int i = 0; i < snapshot.Count; i++)
        {
            if (!IsLineVisibleByExpansion(i, snapshot, expandState))
            {
                continue;
            }

            var node = snapshot.Lines[i];
            if (!filesOnly)
            {
                visible.Add(i);
                continue;
            }

            if (!node.IsDirectory)
            {
                visible.Add(i);
                continue;
            }

            if (DirectoryHasVisibleFiles(i, snapshot, expandState, rangeIndex, filesOnly))
            {
                visible.Add(i);
            }
        }

        return visible;
    }

    public bool DirectoryHasVisibleFiles(int dirLineIndex, TreeSnapshot snapshot, ExpandState expandState, TreeRangeIndex rangeIndex, bool filesOnly)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (expandState == null)
        {
            throw new ArgumentNullException(nameof(expandState));
        }

        if (rangeIndex == null)
        {
            throw new ArgumentNullException(nameof(rangeIndex));
        }

        if (dirLineIndex < 0 || dirLineIndex >= snapshot.Count)
        {
            return false;
        }

        _ = filesOnly;

        var node = snapshot.Lines[dirLineIndex];
        if (!node.IsDirectory)
        {
            return false;
        }

        foreach (var descendantIndex in rangeIndex.GetDescendantLineIndexes(dirLineIndex))
        {
            if (!IsLineVisibleByExpansion(descendantIndex, snapshot, expandState))
            {
                continue;
            }

            var descendant = snapshot.Lines[descendantIndex];
            if (!descendant.IsDirectory)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLineVisibleByExpansion(int lineIndex, TreeSnapshot snapshot, ExpandState expandState)
    {
        var node = snapshot.Lines[lineIndex];
        int currentDepth = node.Depth;
        if (currentDepth == 0)
        {
            return expandState.IsExpanded(lineIndex);
        }

        for (int i = lineIndex - 1; i >= 0; i--)
        {
            var candidate = snapshot.Lines[i];
            if (candidate.Depth < currentDepth)
            {
                if (!expandState.IsExpanded(i))
                {
                    return false;
                }

                currentDepth = candidate.Depth;
                if (currentDepth == 0)
                {
                    break;
                }
            }
        }

        return true;
    }
}
