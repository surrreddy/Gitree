using Gitree.Core.Rendering;

namespace Gitree.Core.Snapshot;

public static class TreeSnapshotBuilder
{
    public static TreeSnapshot BuildFromTranscript(PrintTranscript transcript)
    {
        if (transcript is null)
        {
            return new TreeSnapshot(Array.Empty<TreeNode>(), Array.Empty<string>());
        }

        var nodes = new List<TreeNode>(transcript.Entries.Count);
        var printedLines = new List<string>(transcript.Entries.Count);

        foreach (var entry in transcript.Entries)
        {
            nodes.Add(new TreeNode(entry.RelativePath, entry.DisplayName, entry.IsDirectory, entry.Depth, entry.IsLastSibling));
            printedLines.Add(entry.PrintedText);
        }

        return new TreeSnapshot(nodes, printedLines);
    }
}
