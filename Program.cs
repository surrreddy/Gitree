using System.Text;
using System.Text.RegularExpressions;
using Gitree.App;
using Gitree.Core.Rendering;
using Gitree.Core.Snapshot;
using Gitree.UI;

internal sealed class CliSettings
{
    public string TargetPath { get; set; } = ".";
    public int? Depth { get; set; } = null;          // null = unlimited; 0 = root only
    public bool UseAscii { get; set; } = false;
    public bool FilesOnly { get; set; } = false;     // default false
    public bool IncludeHidden { get; set; } = false; // default false
    public List<string> MatchGlobs { get; } = new();
    public List<string> IgnoreGlobs { get; } = new();
    public bool EnableTui { get; set; } = false;
}

internal sealed class GitIgnoreRule
{
    public Regex Regex { get; }
    public bool IsNegation { get; }
    public bool DirectoryOnly { get; }
    public bool Anchored { get; }
    public string Raw { get; }

    public GitIgnoreRule(Regex regex, bool neg, bool dirOnly, bool anchored, string raw)
    {
        Regex = regex; IsNegation = neg; DirectoryOnly = dirOnly; Anchored = anchored; Raw = raw;
    }
}

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (!TryParseArgs(args, out var settings, out var error))
            {
                if (!string.IsNullOrWhiteSpace(error)) Console.Error.WriteLine(error);
                PrintUsage();
                return AppConfig.ExitErr;
            }

            bool uiRequested = Features.IsPhase1Enabled(args);
            settings.EnableTui = uiRequested;

            string root = Path.GetFullPath(settings.TargetPath);
            string giPath = Path.Combine(root, ".gitignore");
            if (!File.Exists(giPath))
            {
                Console.WriteLine($"❌ .gitignore NOT found in: {root}");
                Console.WriteLine("This tool requires a .gitignore at the project root.");
                return AppConfig.ExitMissingGitIgnore;
            }

            var rules = LoadGitIgnore(giPath);
            var extraIgnore = settings.IgnoreGlobs.Select(g => GlobToRegex(g)).ToList();
            var extraMatch  = settings.MatchGlobs .Select(g => GlobToRegex(g)).ToList();

            var style = settings.UseAscii ? TreeStyle.Ascii : TreeStyle.Unicode;
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            var recorder = new PrintTranscriptRecorder();

            var rootInfo = new DirectoryInfo(root).Name;
            Console.WriteLine(rootInfo);
            recorder.RecordLine(string.Empty, rootInfo, isDirectory: true, depth: 0, isLastSibling: true, printedText: rootInfo);

            int remain = settings.Depth.HasValue ? Math.Max(0, settings.Depth.Value) : int.MaxValue;

            var prefixStack = new List<bool>(); // true => there are more siblings at this level
            var topEntries = ListEntries(root).ToList();
            for (int idx = 0; idx < topEntries.Count; idx++)
            {
                var entry = topEntries[idx];
                bool isLast = idx == topEntries.Count - 1;
                RenderNode(
                    entry, root, rules, extraIgnore, extraMatch,
                    settings, style, prefixStack, isLast, remain,
                    recorder, depth: 1
                );
            }

            if (uiRequested)
            {
                var snapshot = TreeSnapshotBuilder.BuildFromTranscript(recorder.Transcript);
                var screen = new Screen(AppConfig.ConsoleSupportsBasicAnsi());
                return new TuiLoop().Run(snapshot, screen, Features.Phase2StatusHint);
            }

            return AppConfig.ExitOk;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return AppConfig.ExitErr;
        }
    }

    // ---------------- CLI parsing ----------------

    private static bool TryParseArgs(string[] args, out CliSettings settings, out string error)
    {
        settings = new CliSettings();
        error = string.Empty;

        int i = 0;
        if (i < args.Length && !IsSwitch(args[i]))
        {
            settings.TargetPath = args[i];
            i++;
        }

        while (i < args.Length)
        {
            string a = args[i];

            if (Eq(a, "--depth"))
            {
                if (!RequireValue(args, ++i, "--depth", out string? v, out error))
                    return false;
                if (!int.TryParse(v, out int depth) || depth < 0)
                {
                    error = "Invalid value for --depth. Must be a non-negative integer.";
                    return false;
                }
                settings.Depth = depth;
                i++;
                continue;
            }
            if (Eq(a, "--ascii")) { settings.UseAscii = true; i++; continue; }
            if (Eq(a, "--files-only")) { settings.FilesOnly = true; i++; continue; }
            if (Eq(a, "--hidden")) { settings.IncludeHidden = true; i++; continue; }
            if (Eq(a, "--match"))
            {
                if (!RequireValue(args, ++i, "--match", out string? v, out error)) return false;
                settings.MatchGlobs.Add(v!); i++; continue;
            }
            if (Eq(a, "--ignore"))
            {
                if (!RequireValue(args, ++i, "--ignore", out string? v, out error)) return false;
                settings.IgnoreGlobs.Add(v!); i++; continue;
            }
            if (Eq(a, "--ui")) { settings.EnableTui = true; i++; continue; }

            error = $"Unknown option: {a}";
            return false;
        }

        string full = Path.GetFullPath(settings.TargetPath);
        if (!Directory.Exists(full))
        {
            error = $"Path does not exist: {settings.TargetPath}";
            return false;
        }
        return true;
    }

    private static bool IsSwitch(string s) => s.StartsWith("-", StringComparison.Ordinal);
    private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool RequireValue(string[] args, int idx, string opt, out string? value, out string error)
    {
        error = string.Empty; value = null;
        if (idx >= args.Length || IsSwitch(args[idx])) { error = $"Missing value for {opt}."; return false; }
        value = args[idx]; return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
Usage:
  gitree [path] [options]

Options:
  --depth <N>        Limit recursion depth (0 = just root). Default: unlimited.
  --ascii            Use ASCII tree characters instead of Unicode.
  --files-only       Print only files (omit directories that would be empty).
  --hidden           Include hidden/system items (still subject to .gitignore).
  --match  <glob>    Post-filter include by glob (can be repeated).
  --ignore <glob>    Post-filter exclude by glob (can be repeated).

Notes:
  • The target path must contain a .gitignore; otherwise the tool exits.
  • Globs support *, ?, ** and / anchors (gitignore-like semantics).
");
    }

    // ---------------- Filesystem helpers ----------------

    private readonly record struct Entry(string FullPath, bool IsDirectory)
    {
        public string Name => IsDirectory ? new DirectoryInfo(FullPath).Name : Path.GetFileName(FullPath);
    }

    private static IEnumerable<Entry> ListEntries(string dir)
    {
        IEnumerable<string> ents;
        try { ents = Directory.EnumerateFileSystemEntries(dir); }
        catch { yield break; }

        foreach (var p in ents)
        {
            bool isDir = false;
            try { isDir = Directory.Exists(p); } catch { /* ignore */ }
            yield return new Entry(p, isDir);
        }
    }

    private static bool IsHiddenFs(string path)
    {
        try
        {
            var a = File.GetAttributes(path);
            return a.HasFlag(FileAttributes.Hidden) || a.HasFlag(FileAttributes.System);
        }
        catch { return false; }
    }

    private static string RelForward(string fullPath, string root)
    {
        var rel = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return rel.TrimStart('/');
    }

    // ---------------- Gitignore parsing & matching ----------------

    private static List<GitIgnoreRule> LoadGitIgnore(string gitignorePath)
    {
        var lines = File.ReadAllLines(gitignorePath);
        var rules = new List<GitIgnoreRule>(lines.Length);

        foreach (var raw in lines)
        {
            var line = raw.Replace("\t", " ").TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            bool neg = false;
            if (line.StartsWith("!")) { neg = true; line = line[1..]; }

            if (line.StartsWith(@"\#")) line = "#" + line[2..];
            if (line.StartsWith(@"\!")) line = "!" + line[2..];

            bool dirOnly = false;
            if (line.EndsWith("/")) { dirOnly = true; line = line.TrimEnd('/'); }

            bool anchored = line.StartsWith("/");
            string pattern = anchored ? line[1..] : line;
            if (pattern.Length == 0) continue;

            var rx = BuildGitGlobRegex(pattern, anchored);
            rules.Add(new GitIgnoreRule(rx, neg, dirOnly, anchored, raw));
        }
        return rules;
    }

    private static Regex GlobToRegex(string glob)
    {
        bool anchored = glob.StartsWith("/");
        var pat = anchored ? glob[1..] : glob;
        return BuildGitGlobRegex(pat, anchored);
    }

    private static Regex BuildGitGlobRegex(string pattern, bool anchored)
    {
        // Git-like: ** crosses dirs, * not across '/', ? single, '/' is separator.
        // FIX: support bracket character classes like [Bb]in/
        var sb = new StringBuilder();
        sb.Append(anchored ? "^" : "(^|.*/)");
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];

            if (c == '*')
            {
                if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                    {
                        sb.Append("(.*/)?"); i += 2; continue; // **/
                    }
                    sb.Append(".*"); i += 1; continue;          // **
                }
                sb.Append("[^/]*"); continue;                   // *
            }

            if (c == '?') { sb.Append("[^/]"); continue; }

            if (c == '/')
            {
                sb.Append("/"); continue;
            }

            if (c == '[')
            {
                // Parse bracket class [ ... ]
                int j = i + 1;
                if (j >= pattern.Length) { sb.Append(@"\["); continue; } // trailing '[' literal

                // Find closing ']'
                bool found = false;
                for (; j < pattern.Length; j++)
                {
                    if (pattern[j] == ']')
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // No closing ']' -> treat '[' literally
                    sb.Append(@"\[");
                    continue;
                }

                // Extract class content between i+1 and j-1
                string cls = pattern.Substring(i + 1, j - i - 1);
                bool negate = cls.Length > 0 && cls[0] == '!';
                int startK = negate ? 1 : 0;

                sb.Append('[');
                if (negate) sb.Append('^');

                // Copy characters; escape those meaningful to regex char classes except '-' (allowed for ranges)
                for (int k = startK; k < cls.Length; k++)
                {
                    char cc = cls[k];
                    // allow '-' for ranges; if you want literal '-', users typically place it first/last
                    if (cc is '\\' or '^' or ']' )
                    {
                        sb.Append('\\').Append(cc);
                    }
                    else
                    {
                        sb.Append(cc);
                    }
                }
                sb.Append(']');

                i = j; // advance past closing ']'
                continue;
            }

            // Default: escape literal
            sb.Append(Regex.Escape(c.ToString()));
        }
        sb.Append("(/.*)?$");
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    private static bool IsIgnoredByGit(string relForward, bool isDir, List<GitIgnoreRule> rules)
    {
        bool? state = null;
        foreach (var r in rules)
        {
            if (r.DirectoryOnly && !isDir) continue;
            if (r.Regex.IsMatch(relForward)) state = !r.IsNegation; // last match wins
        }
        return state == true;
    }

    private static bool MatchesAny(IEnumerable<Regex> regs, string rel) =>
        regs.Any(r => r.IsMatch(rel));

    // ---------------- Printing logic ----------------

    private static void RenderNode(
        Entry entry,
        string root,
        List<GitIgnoreRule> rules,
        List<Regex> extraIgnore,
        List<Regex> extraMatch,
        CliSettings settings,
        TreeStyle style,
        List<bool> prefixStack,
        bool isLast,
        int remainingDepth,
        PrintTranscriptRecorder recorder,
        int depth)
    {
        string rel = RelForward(entry.FullPath, root);

        // Hidden filter first
        if (!settings.IncludeHidden && IsHiddenFs(entry.FullPath)) return;

        // .gitignore-based ignore (+ extraIgnore)
        if (IsIgnoredByGit(rel, entry.IsDirectory, rules)) return;
        if (extraIgnore.Count > 0 && MatchesAny(extraIgnore, rel)) return;

        if (entry.IsDirectory)
        {
            var children = ListEntries(entry.FullPath).ToList();
            bool exceeded = remainingDepth == 0;

            // Should we print this directory?
            bool printDir;
            if (!settings.FilesOnly)
            {
                // Normal: always print (subject to ignore)
                printDir = true;
            }
            else
            {
                // files-only: print dir if it has any printable descendants,
                // or if dir itself matches an explicit --match filter.
                bool dirMatches = extraMatch.Count > 0 && MatchesAny(extraMatch, rel);
                bool hasPrintable = !exceeded && HasPrintableDescendants(
                    children, root, rules, extraIgnore, extraMatch, settings, remainingDepth - 1
                );
                printDir = dirMatches || hasPrintable;
            }

            if (!printDir) return;

            // Print this directory line first
            string prefix = BuildPrefixString(prefixStack, style);
            WritePrefix(prefixStack, style);
            string lineText = (isLast ? style.Last : style.Mid) + " " + entry.Name;
            Console.WriteLine(lineText);
            recorder.RecordLine(rel, entry.Name, isDirectory: true, depth: depth, isLastSibling: isLast, printedText: prefix + lineText);

            if (exceeded)
            {
                if (children.Count > 0)
                {
                    // Print a single ellipsis as a child line
                    prefixStack.Add(!isLast);
                    string childPrefix = BuildPrefixString(prefixStack, style);
                    WritePrefix(prefixStack, style);
                    string ellipsisText = style.Last + " " + "…";
                    Console.WriteLine(ellipsisText);
                    string ellipsisRel = string.IsNullOrEmpty(rel) ? "…" : rel + "/…";
                    recorder.RecordLine(ellipsisRel, "…", isDirectory: false, depth: depth + 1, isLastSibling: true, printedText: childPrefix + ellipsisText);
                    prefixStack.RemoveAt(prefixStack.Count - 1);
                }
                return;
            }

            // Recurse into children
            prefixStack.Add(!isLast);
            for (int idx = 0; idx < children.Count; idx++)
            {
                var ch = children[idx];
                bool chIsLast = idx == children.Count - 1;
                RenderNode(
                    ch, root, rules, extraIgnore, extraMatch, settings,
                    style, prefixStack, chIsLast, remainingDepth - 1,
                    recorder, depth + 1
                );
            }
            prefixStack.RemoveAt(prefixStack.Count - 1);
        }
        else
        {
            // File must pass match filters if provided
            if (extraMatch.Count > 0 && !MatchesAny(extraMatch, rel)) return;

            string prefix = BuildPrefixString(prefixStack, style);
            WritePrefix(prefixStack, style);
            string lineText = (isLast ? style.Last : style.Mid) + " " + entry.Name;
            Console.WriteLine(lineText);
            recorder.RecordLine(rel, entry.Name, isDirectory: false, depth: depth, isLastSibling: isLast, printedText: prefix + lineText);
        }
    }

    private static bool HasPrintableDescendants(
        List<Entry> children,
        string root,
        List<GitIgnoreRule> rules,
        List<Regex> extraIgnore,
        List<Regex> extraMatch,
        CliSettings settings,
        int remainingDepth)
    {
        foreach (var ch in children)
        {
            if (!settings.IncludeHidden && IsHiddenFs(ch.FullPath)) continue;

            string rel = RelForward(ch.FullPath, root);
            if (IsIgnoredByGit(rel, ch.IsDirectory, rules)) continue;
            if (extraIgnore.Count > 0 && MatchesAny(extraIgnore, rel)) continue;

            if (!ch.IsDirectory)
            {
                if (extraMatch.Count == 0 || MatchesAny(extraMatch, rel))
                    return true;
            }
            else
            {
                if (remainingDepth < 0) continue; // no room to descend
                // Directory itself may count if it matches match-glob explicitly.
                if (extraMatch.Count > 0 && MatchesAny(extraMatch, rel))
                    return true;

                if (remainingDepth >= 0)
                {
                    var grand = ListEntries(ch.FullPath).ToList();
                    if (HasPrintableDescendants(grand, root, rules, extraIgnore, extraMatch, settings, remainingDepth - 1))
                        return true;
                }
            }
        }
        return false;
    }

    private static void WritePrefix(List<bool> prefixStack, TreeStyle style)
    {
        // Each level: either a vertical bar (more siblings) or spaces.
        foreach (bool more in prefixStack)
        {
            Console.Write(more ? style.Vert : style.Space);
            Console.Write(' ');
        }
    }

    private static string BuildPrefixString(List<bool> prefixStack, TreeStyle style)
    {
        if (prefixStack.Count == 0) return string.Empty;

        var sb = new StringBuilder(prefixStack.Count * 2);
        foreach (bool more in prefixStack)
        {
            sb.Append(more ? style.Vert : style.Space);
            sb.Append(' ');
        }
        return sb.ToString();
    }
}

// ---------------- Rendering styles ----------------

internal sealed class TreeStyle
{
    public string Vert { get; }
    public string Mid { get; }
    public string Last { get; }
    public string Space { get; }

    private TreeStyle(string vert, string mid, string last, string space)
    {
        Vert = vert; Mid = mid; Last = last; Space = space;
    }

    public static TreeStyle Unicode { get; } = new TreeStyle("│", "├─", "└─", " ");
    public static TreeStyle Ascii   { get; } = new TreeStyle("|", "|--", "`--", " ");
}
