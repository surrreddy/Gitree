using System;
using System.Collections.Generic;
using Gitree.Core.Snapshot;

namespace Gitree.Core.Selection;

public sealed class TreeRangeIndex
{
    private readonly TreeSnapshot _snapshot;
    private readonly Dictionary<int, IReadOnlyList<int>> _descendants = new();
    private static readonly IReadOnlyList<int> Empty = Array.Empty<int>();

    public TreeRangeIndex(TreeSnapshot snapshot)
    {
        _snapshot = snapshot;
        BuildIndex();
    }

    private void BuildIndex()
    {
        for (int i = 0; i < _snapshot.Count; i++)
        {
            var node = _snapshot.Lines[i];
            if (!node.IsDirectory)
            {
                continue;
            }

            int depth = node.Depth;
            var indices = new List<int>();
            for (int j = i + 1; j < _snapshot.Count; j++)
            {
                var candidate = _snapshot.Lines[j];
                if (candidate.Depth <= depth)
                {
                    break;
                }

                indices.Add(j);
            }

            _descendants[i] = indices.Count > 0 ? indices.ToArray() : Empty;
        }
    }

    public IReadOnlyList<int> GetDescendantLineIndexes(int dirLineIndex)
    {
        if (_descendants.TryGetValue(dirLineIndex, out var indexes))
        {
            return indexes;
        }

        return Empty;
    }

    public IEnumerable<string> GetDescendantFilePaths(int dirLineIndex)
    {
        var indexes = GetDescendantLineIndexes(dirLineIndex);
        foreach (var idx in indexes)
        {
            var node = _snapshot.Lines[idx];
            if (!node.IsDirectory)
            {
                yield return node.RelativePath;
            }
        }
    }

    public DirectoryCoverage ComputeCoverage(int dirLineIndex, SelectionSet set)
    {
        var indexes = GetDescendantLineIndexes(dirLineIndex);
        if (indexes.Count == 0)
        {
            return new DirectoryCoverage(0, 0);
        }

        int totalFiles = 0;
        int selectedFiles = 0;
        foreach (var idx in indexes)
        {
            var node = _snapshot.Lines[idx];
            if (node.IsDirectory)
            {
                continue;
            }

            totalFiles++;
            if (set.IsFileSelected(node.RelativePath))
            {
                selectedFiles++;
            }
        }

        return new DirectoryCoverage(totalFiles, selectedFiles);
    }

    public readonly struct DirectoryCoverage
    {
        public DirectoryCoverage(int totalFiles, int selectedFiles)
        {
            TotalFiles = totalFiles;
            SelectedFiles = selectedFiles;
        }

        public int TotalFiles { get; }
        public int SelectedFiles { get; }
    }
}
