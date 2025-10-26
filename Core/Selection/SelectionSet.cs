using System;
using System.Collections.Generic;

namespace Gitree.Core.Selection;

public sealed class SelectionSet
{
    private readonly HashSet<string> _selectedFiles = new(StringComparer.Ordinal);

    public int SelectedFileCount => _selectedFiles.Count;

    public bool IsFileSelected(string relPath)
    {
        if (string.IsNullOrEmpty(relPath))
        {
            return false;
        }

        return _selectedFiles.Contains(relPath);
    }

    public void SelectFiles(IEnumerable<string> relPaths)
    {
        if (relPaths == null)
        {
            return;
        }

        foreach (var path in relPaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _selectedFiles.Add(path);
            }
        }
    }

    public void DeselectFiles(IEnumerable<string> relPaths)
    {
        if (relPaths == null)
        {
            return;
        }

        foreach (var path in relPaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _selectedFiles.Remove(path);
            }
        }
    }
}
