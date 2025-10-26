using System;
using System.Collections.Generic;
using Gitree.Core.Snapshot;

namespace Gitree.Core.Selection;

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

public sealed class TreeRangeIndex
{
    private static readonly IReadOnlyList<int> EmptyIndexes = Array.Empty<int>();

    private readonly TreeSnapshot _snapshot;
    private readonly Dictionary<int, IReadOnlyList<int>> _descendantIndexes = new();

    public TreeRangeIndex(TreeSnapshot snapshot)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        BuildIndex();
    }

    public IReadOnlyList<int> GetDescendantLineIndexes(int dirLineIndex)
    {
        if (_descendantIndexes.TryGetValue(dirLineIndex, out var list))
        {
            return list;
        }

        return EmptyIndexes;
    }

    public IEnumerable<string> GetDescendantFilePaths(int dirLineIndex)
    {
        foreach (var index in GetDescendantLineIndexes(dirLineIndex))
        {
            var node = _snapshot.Lines[index];
            if (!node.IsDirectory)
            {
                yield return node.RelativePath;
            }
        }
    }

    public DirectoryCoverage ComputeCoverage(int dirLineIndex, SelectionSet set)
    {
        if (set == null)
        {
            throw new ArgumentNullException(nameof(set));
        }

        int totalFiles = 0;
        int selectedFiles = 0;

        foreach (var index in GetDescendantLineIndexes(dirLineIndex))
        {
            var node = _snapshot.Lines[index];
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

    private void BuildIndex()
    {
        var lines = _snapshot.Lines;
        for (int i = 0; i < lines.Count; i++)
        {
            var node = lines[i];
            if (!node.IsDirectory)
            {
                continue;
            }

            int baseDepth = node.Depth;
            var descendants = new List<int>();
            for (int j = i + 1; j < lines.Count; j++)
            {
                var candidate = lines[j];
                if (candidate.Depth <= baseDepth)
                {
                    break;
                }

                descendants.Add(j);
            }

            _descendantIndexes[i] = descendants;
        }
    }
}
