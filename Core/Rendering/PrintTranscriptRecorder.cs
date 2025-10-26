namespace Gitree.Core.Rendering;

public sealed class PrintTranscript
{
    private readonly List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries => _entries;

    internal void AddEntry(Entry entry) => _entries.Add(entry);
}

public sealed class Entry
{
    public Entry(string relativePath, string displayName, bool isDirectory, int depth, bool isLastSibling, string printedText)
    {
        RelativePath = relativePath;
        DisplayName = displayName;
        IsDirectory = isDirectory;
        Depth = depth;
        IsLastSibling = isLastSibling;
        PrintedText = printedText;
    }

    public string RelativePath { get; }
    public string DisplayName { get; }
    public bool IsDirectory { get; }
    public int Depth { get; }
    public bool IsLastSibling { get; }
    public string PrintedText { get; }
}

public sealed class PrintTranscriptRecorder
{
    private readonly PrintTranscript _transcript = new();

    public PrintTranscript Transcript => _transcript;

    public void RecordLine(string relativePath, string displayName, bool isDirectory, int depth, bool isLastSibling, string printedText)
    {
        var entry = new Entry(relativePath, displayName, isDirectory, depth, isLastSibling, printedText);
        _transcript.AddEntry(entry);
    }
}
