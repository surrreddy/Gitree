using System.Collections.Generic;

namespace Gitree.Core.Rendering;

public sealed class PrintTranscript
{
    private readonly List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries => _entries;

    public void Add(Entry entry)
    {
        _entries.Add(entry);
    }
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
    public PrintTranscriptRecorder()
    {
        Transcript = new PrintTranscript();
    }

    public PrintTranscript Transcript { get; }

    public void RecordLine(string relativePath, string displayName, bool isDirectory, int depth, bool isLastSibling, string printedText)
    {
        var entry = new Entry(relativePath, displayName, isDirectory, depth, isLastSibling, printedText);
        Transcript.Add(entry);
    }
}
