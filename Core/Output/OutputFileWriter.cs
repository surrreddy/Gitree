using System;
using System.Collections.Generic;
using System.IO;
using Gitree.App;

namespace Gitree.Core.Output;

public sealed class OutputFileWriter
{
    public void Write(ExportSpec spec)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        var resolver = BuildResolver(spec);
        var formatter = new ExportFormatter(spec, AppConfig.SectionSeparator, Environment.NewLine);
        string text = formatter.BuildText(path => resolver.TryGetValue(path, out var content) ? content : string.Empty);

        string cwd = Directory.GetCurrentDirectory();
        string finalPath = Path.Combine(cwd, AppConfig.ExportFileName);
        string tempPath = finalPath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, text, AppConfig.ExportEncoding);
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(tempPath, finalPath);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup failures
                }
            }
        }
    }

    private static Dictionary<string, string> BuildResolver(ExportSpec spec)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in spec.SelectedRelativeFilePaths)
        {
            string normalized = relativePath ?? string.Empty;
            string systemRelative = normalized.Replace('/', Path.DirectorySeparatorChar);
            string combined = Path.Combine(spec.ProjectRoot, systemRelative);
            string fullPath = Path.GetFullPath(combined);

            try
            {
                string content = File.ReadAllText(fullPath, AppConfig.ExportEncoding);
                map[relativePath!] = content;
            }
            catch (Exception ex)
            {
                map[relativePath!] = $"[ERROR] Could not read file: {ex.Message}";
            }
        }

        return map;
    }
}
