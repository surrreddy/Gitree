using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gitree.Core.Output;

public sealed class ExportSpec
{
    public ExportSpec(string projectRoot, IReadOnlyList<string> printedTreeLines, IReadOnlyList<string> selectedRelativeFilePaths)
    {
        ProjectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
        PrintedTreeLines = CreateReadOnlyCopy(printedTreeLines);
        SelectedRelativeFilePaths = CreateSortedReadOnlyCopy(selectedRelativeFilePaths);
    }

    public string ProjectRoot { get; }

    public IReadOnlyList<string> PrintedTreeLines { get; }

    public IReadOnlyList<string> SelectedRelativeFilePaths { get; }

    private static IReadOnlyList<string> CreateReadOnlyCopy(IReadOnlyList<string> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<string>();
        }

        var copy = new string[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            copy[i] = source[i] ?? string.Empty;
        }

        return new ReadOnlyCollection<string>(copy);
    }

    private static IReadOnlyList<string> CreateSortedReadOnlyCopy(IReadOnlyList<string> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            if (!string.IsNullOrEmpty(item))
            {
                set.Add(item.Replace('\\', '/'));
            }
        }

        if (set.Count == 0)
        {
            return Array.Empty<string>();
        }

        var ordered = set.ToList();
        ordered.Sort(StringComparer.OrdinalIgnoreCase);
        return new ReadOnlyCollection<string>(ordered);
    }
}
