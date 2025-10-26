namespace Gitree.Core.View;

public sealed class ExpandState
{
    private readonly HashSet<int> _expandedIndexes = new();
    private readonly bool _rootExpandedByDefault;

    public ExpandState(bool rootExpandedByDefault)
    {
        _rootExpandedByDefault = rootExpandedByDefault;
    }

    public bool IsExpanded(int dirLineIndex)
    {
        if (dirLineIndex <= 0)
        {
            return _rootExpandedByDefault;
        }

        return _expandedIndexes.Contains(dirLineIndex);
    }

    public void Expand(int dirLineIndex)
    {
        if (dirLineIndex <= 0)
        {
            return;
        }

        _expandedIndexes.Add(dirLineIndex);
    }

    public void Collapse(int dirLineIndex)
    {
        if (dirLineIndex <= 0)
        {
            return;
        }

        _expandedIndexes.Remove(dirLineIndex);
    }

    public void Toggle(int dirLineIndex)
    {
        if (dirLineIndex <= 0)
        {
            return;
        }

        if (!_expandedIndexes.Remove(dirLineIndex))
        {
            _expandedIndexes.Add(dirLineIndex);
        }
    }
}
