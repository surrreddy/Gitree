using System.Linq;

namespace Gitree.Core.Snapshot;

public sealed class TreeSnapshot
{
    private readonly IReadOnlyList<TreeNode> _lines;
    private readonly IReadOnlyList<string> _printedLines;

    public TreeSnapshot(IReadOnlyList<TreeNode> lines, IReadOnlyList<string> printedLines)
    {
        _lines = lines.ToList();
        _printedLines = printedLines.ToList();
    }

    public IReadOnlyList<TreeNode> Lines => _lines;

    public IReadOnlyList<string> PrintedLines => _printedLines;

    public int Count => _lines.Count;

    public bool IsEmpty => _lines.Count == 0;
}
