namespace Gitree.Core.Snapshot;

public sealed class TreeNode
{
    public TreeNode(string relativePath, string displayName, bool isDirectory, int depth, bool isLastSibling)
    {
        RelativePath = relativePath;
        DisplayName = displayName;
        IsDirectory = isDirectory;
        Depth = depth;
        IsLastSibling = isLastSibling;
    }

    public string RelativePath { get; }
    public string DisplayName { get; }
    public bool IsDirectory { get; }
    public int Depth { get; }
    public bool IsLastSibling { get; }
}
