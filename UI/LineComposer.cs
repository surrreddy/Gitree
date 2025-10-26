using Gitree.Core.Selection;
using Gitree.Core.Snapshot;

namespace Gitree.UI;

public static class LineComposer
{
    public static string Compose(
        TreeNode node,
        int lineIndex,
        SelectionSet selection,
        TreeRangeIndex index,
        bool hasDescendants,
        bool isExpanded,
        bool useUnicodeGlyphs)
    {
        if (node == null)
        {
            return string.Empty;
        }

        string glyph = ComposeGlyph(node, hasDescendants, isExpanded, useUnicodeGlyphs);
        string checkbox = node.IsDirectory
            ? ComposeDirectoryCheckbox(lineIndex, selection, index)
            : ComposeFileCheckbox(node, selection);

        string printed = node.PrintedText ?? string.Empty;
        return $"{glyph} {checkbox} {printed}";
    }

    private static string ComposeGlyph(TreeNode node, bool hasDescendants, bool isExpanded, bool useUnicodeGlyphs)
    {
        if (node.IsDirectory)
        {
            if (!hasDescendants)
            {
                return " ";
            }

            if (useUnicodeGlyphs)
            {
                return isExpanded ? "▾" : "▸";
            }

            return isExpanded ? "-" : "+";
        }

        return " ";
    }

    private static string ComposeFileCheckbox(TreeNode node, SelectionSet selection)
    {
        bool isSelected = selection.IsFileSelected(node.RelativePath);
        return isSelected ? "[x]" : "[ ]";
    }

    private static string ComposeDirectoryCheckbox(int lineIndex, SelectionSet selection, TreeRangeIndex index)
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
