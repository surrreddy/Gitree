using Gitree.Core.Selection;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public static class LineComposer
{
    public static string Compose(TreeNode node, string printedText, int lineIndex, SelectionSet selection, TreeRangeIndex index)
    {
        string checkbox = node.IsDirectory
            ? ComposeDirectory(lineIndex, selection, index)
            : ComposeFile(node, selection);

        string content = printedText ?? string.Empty;
        return $"{checkbox} {content}";
    }

    private static string ComposeFile(TreeNode node, SelectionSet selection)
    {
        return selection.IsFileSelected(node.RelativePath) ? "[x]" : "[ ]";
    }

    private static string ComposeDirectory(int lineIndex, SelectionSet selection, TreeRangeIndex index)
    {
        var coverage = index.ComputeCoverage(lineIndex, selection);
        if (coverage.TotalFiles == 0 || coverage.SelectedFiles == 0)
        {
            return "[ ]";
        }

        if (coverage.SelectedFiles == coverage.TotalFiles)
        {
            return "[x]";
        }

        return "[•]";
    }
}
