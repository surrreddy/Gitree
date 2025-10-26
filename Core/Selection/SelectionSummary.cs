using Gitree.Core.Snapshot;

namespace Gitree.Core.Selection;

public sealed class SelectionSummary
{
    public SelectionSummary(int selectedFiles, int fullDirs, int partialDirs)
    {
        SelectedFiles = selectedFiles;
        FullDirs = fullDirs;
        PartialDirs = partialDirs;
    }

    public int SelectedFiles { get; }
    public int FullDirs { get; }
    public int PartialDirs { get; }

    public static SelectionSummary Compute(TreeSnapshot snapshot, TreeRangeIndex index, SelectionSet selection)
    {
        int fullDirs = 0;
        int partialDirs = 0;

        for (int i = 0; i < snapshot.Count; i++)
        {
            var node = snapshot.Lines[i];
            if (!node.IsDirectory)
            {
                continue;
            }

            var coverage = index.ComputeCoverage(i, selection);
            if (coverage.TotalFiles == 0)
            {
                continue;
            }

            if (coverage.SelectedFiles == 0)
            {
                continue;
            }

            if (coverage.SelectedFiles == coverage.TotalFiles)
            {
                fullDirs++;
            }
            else
            {
                partialDirs++;
            }
        }

        return new SelectionSummary(selection.SelectedFileCount, fullDirs, partialDirs);
    }
}
