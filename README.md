
# Gitree

A tiny Windows-friendly CLI that prints a tree view of your project **while honoring `.gitignore`**.
If a `.gitignore` isn’t present in the target directory, the tool refuses to run (by design).

```
root
├─ src
│  ├─ app
│  │  └─ Program.cs
│  └─ lib
└─ README.md
```

## Features

* Requires a `.gitignore` in the target path (prevents misuse).
* Honors common gitignore semantics: comments, `!` negations, `/` anchors, `* ? **`, and dir-only rules.
* Windows hidden/system files are **not shown by default** (unless `--hidden`).
* Works from any folder once the EXE is added to your PATH.
* Optional ASCII output for easy copy/paste.

---

## Quick Start (Windows)

### Option A — Use a prebuilt EXE (recommended)

1. Download `gitree.exe` from your repo’s Releases page (or from `publish/` if you built it).
2. Put it somewhere on your **User PATH**, e.g.:

   * Create `C:\Tools\gitree\`
   * Copy `gitree.exe` into `C:\Tools\gitree\`
   * Add `C:\Tools\gitree\` to **User** PATH:

     * Start → “Edit the system environment variables” → *Environment Variables…*
       Under **User variables**, select `Path` → *Edit…* → *New* → `C:\Tools\gitree\` → OK
   * Open a **new** terminal and verify:

     ```
     where gitree
     ```

### Option B — Build from source

```powershell
git clone https://github.com/<you>/Gitree.git
cd Gitree
dotnet build
dotnet publish Gitree.csproj -c Release -r win-x64 `
  -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true `
  -o .\publish
# copy .\publish\gitree.exe to a folder on your PATH (e.g., C:\Tools\gitree\)
```

> The publish command outputs a single portable `gitree.exe` that includes the .NET runtime.

---

## Usage (Windows)

```
gitree [path] [options]
```

* **path** (optional): folder to inspect (defaults to `.`).
  Must contain a `.gitignore`, otherwise the tool exits.

### Options

* `--depth <N>` Limit recursion depth (0 = print only the root line). Default: unlimited.
* `--ascii` Use ASCII connectors (`|--`, `` `-- ``) instead of Unicode.
* `--files-only` Print only files. Directories are shown **only** if they contain printable files/descendants.
* `--hidden` Include hidden/system items (still subject to `.gitignore`).
* `--match "<glob>"` Include-only filter applied after `.gitignore`. Can be repeated.
* `--ignore "<glob>"` Extra exclude filter applied after `.gitignore`. Can be repeated.

### Examples

```powershell
# Basic usage (requires .gitignore in current folder)
gitree

# Limit depth to 2 levels
gitree --depth 2

# ASCII output (better for plain text tickets)
gitree --ascii --depth 3

# Only files (show dirs only when they contain printable files)
gitree --files-only --depth 2

# Include hidden/system files too
gitree --hidden

# Filter to only C# files; also ignore designer files
gitree --match "*.cs" --ignore "**/*.Designer.cs"

# Run on a subfolder that also has .gitignore
gitree src --depth 1
```

### Exit codes

* `0` — success
* `2` — `.gitignore` missing in the target path
* `1` — unexpected error (I/O, permissions, invalid args, etc.)

---

## How `.gitignore` is interpreted (short version)

* Lines starting with `#` are comments.
* Trailing `/` means directory-only rule (e.g., `build/`).
* `!` negations re-include a previously ignored path.
* `/` at the beginning anchors to the repo root (`/dist` matches only at root).
* Globs: `*` (no `/`), `?` (single non-`/`), `**` (can cross directories).
* **Last match wins.**
* We always traverse deeply enough to allow a negation rule to re-include a child.

---

## Development

Build & run:

```powershell
dotnet build
dotnet run -- --depth 2 --ascii
```

Publish a portable EXE:

```powershell
dotnet publish Gitree.csproj -c Release -r win-x64 `
  -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true `
  -o .\publish
```

---

## License

MIT (or your preferred license—add a `LICENSE` file).

---

If you want, I can also add a tiny `.gitattributes` and `.gitignore` snippet to keep `publish/` and build artifacts out of the repo, plus a ready-to-use GitHub Release notes template.
