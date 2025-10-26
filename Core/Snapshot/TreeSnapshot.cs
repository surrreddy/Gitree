namespace Gitree.Core.Snapshot;

public sealed class TreeSnapshot
{
    private readonly IReadOnlyList<TreeNode> _lines;

    public TreeSnapshot(IReadOnlyList<TreeNode> lines)
    {
        _lines = lines?.ToArray() ?? Array.Empty<TreeNode>();
    }

    public IReadOnlyList<TreeNode> Lines => _lines;

    public int Count => _lines.Count;

    public bool IsEmpty => _lines.Count == 0;
}
