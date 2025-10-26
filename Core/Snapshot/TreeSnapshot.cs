namespace Gitree.Core.Snapshot;

public sealed class TreeSnapshot
{
    public TreeSnapshot(IReadOnlyList<TreeNode> lines, IReadOnlyList<string> printedLines)
    {
        Lines = lines?.ToArray() ?? Array.Empty<TreeNode>();
        PrintedLines = printedLines?.ToArray() ?? Array.Empty<string>();
    }

    public IReadOnlyList<TreeNode> Lines { get; }

    public IReadOnlyList<string> PrintedLines { get; }

    public int Count => Lines.Count;

    public bool IsEmpty => Lines.Count == 0;
}
