using System;
using System.Text;

namespace Gitree.Core.Output;

public sealed class ExportFormatter
{
    private readonly ExportSpec _spec;
    private readonly string _sectionSeparator;
    private readonly string _newline;

    public ExportFormatter(ExportSpec spec, string sectionSeparator, string newline)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _sectionSeparator = sectionSeparator ?? throw new ArgumentNullException(nameof(sectionSeparator));
        _newline = newline ?? Environment.NewLine;
    }

    public string BuildText(Func<string, string> fileContentResolver)
    {
        if (fileContentResolver == null)
        {
            throw new ArgumentNullException(nameof(fileContentResolver));
        }

        var builder = new StringBuilder();

        AppendPrintedTree(builder);
        builder.Append(_newline); // blank line separating tree from sections

        foreach (var relativePath in _spec.SelectedRelativeFilePaths)
        {
            AppendSection(builder, relativePath, fileContentResolver(relativePath) ?? string.Empty);
        }

        return builder.ToString();
    }

    private void AppendPrintedTree(StringBuilder builder)
    {
        foreach (var line in _spec.PrintedTreeLines)
        {
            builder.Append(line ?? string.Empty);
            builder.Append(_newline);
        }
    }

    private void AppendSection(StringBuilder builder, string relativePath, string content)
    {
        builder.Append(_sectionSeparator).Append(_newline);
        builder.Append('`').Append(relativePath).Append('`').Append(':').Append(_newline);
        builder.Append("```").Append(_newline);

        string resolvedContent = content ?? string.Empty;
        builder.Append(resolvedContent);
        if (resolvedContent.Length > 0 && !EndsWithLineEnding(resolvedContent))
        {
            builder.Append(_newline);
        }

        builder.Append("```").Append(_newline);
        builder.Append(_sectionSeparator).Append(_newline);
    }

    private bool EndsWithLineEnding(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.EndsWith(_newline, StringComparison.Ordinal))
        {
            return true;
        }

        // Support bare "\n" endings even if the newline differs (e.g., reading LF files on Windows)
        return text.EndsWith("\n", StringComparison.Ordinal);
    }
}
