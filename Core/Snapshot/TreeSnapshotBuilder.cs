using Gitree.Core.Rendering;

namespace Gitree.Core.Snapshot;

public static class TreeSnapshotBuilder
{
    public static TreeSnapshot BuildFromTranscript(PrintTranscript transcript)
    {
        var nodes = new List<TreeNode>(transcript.Entries.Count);
        var printed = new List<string>(transcript.Entries.Count);
        foreach (var entry in transcript.Entries)
        {
            var node = new TreeNode(
                entry.RelativePath,
                entry.DisplayName,
                entry.IsDirectory,
                entry.Depth,
                entry.IsLastSibling
            );
            nodes.Add(node);
            printed.Add(entry.PrintedText);
        }

        return new TreeSnapshot(nodes, printed);
    }
}
