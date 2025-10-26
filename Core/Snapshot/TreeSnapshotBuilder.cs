using Gitree.Core.Rendering;

namespace Gitree.Core.Snapshot;

public static class TreeSnapshotBuilder
{
    public static TreeSnapshot BuildFromTranscript(PrintTranscript transcript)
    {
        if (transcript == null)
        {
            return new TreeSnapshot(Array.Empty<TreeNode>());
        }

        var nodes = new List<TreeNode>(transcript.Entries.Count);
        foreach (var entry in transcript.Entries)
        {
            nodes.Add(new TreeNode(
                entry.RelativePath,
                entry.DisplayName,
                entry.IsDirectory,
                entry.Depth,
                entry.IsLastSibling,
                entry.PrintedText));
        }

        return new TreeSnapshot(nodes);
    }
}
