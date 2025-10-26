using System.Collections.Generic;

namespace Gitree.Core.Selection;

public sealed class SelectionSet
{
    private readonly HashSet<string> _selectedFiles = new();

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
        if (relPaths is null)
        {
            return;
        }

        foreach (var path in relPaths)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            _selectedFiles.Add(path);
        }
    }

    public void DeselectFiles(IEnumerable<string> relPaths)
    {
        if (relPaths is null)
        {
            return;
        }

        foreach (var path in relPaths)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            _selectedFiles.Remove(path);
        }
    }

    public int SelectedFileCount => _selectedFiles.Count;
}
