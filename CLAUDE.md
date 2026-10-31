# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Shell

- Always use PowerShell 7 (`pwsh`) for shell commands. Never use bash,
  including Git Bash.
- Write command examples in `pwsh`, in this file, in docs and in PR
  descriptions, and tag their code blocks `pwsh`.

## Project

Comment Reflower is a Visual Studio extension (VSIX) that reflows the text in
source code comments so the word wrapping is even. It supports Visual Studio
2017, 2019, 2022 and 2026.

| Project | Purpose |
| --- | --- |
| `CommentReflower` | VSIX for Visual Studio 2017/2019 |
| `CommentReflower2022` | VSIX for Visual Studio 2022/2026 |
| `CommentReflowerAI` | Separate VSIX for Visual Studio 2026 with its own copy of the package code and its own GUIDs. Its settings are in Tools > Options (Unified Settings), defined in `registration.json` and read by `UnifiedSettings.cs`; it has no settings dialog |
| `CommentReflowerShared` | Shared project: the package (`CommentReflowerPackage.cs`), commands (`CommentReflower.vsct`) and the WinForms settings dialog (`Settings.cs`) |
| `CommentReflowerSharedLib` | Shared project: the reflow engine (`CommentReflowerObj.cs`), parameter aligner, and settings model (`ParameterSet.cs`, `CommentBlock.cs`, `BulletPoint.cs`, `BreakFlowStrings.cs`) |
| `CommentReflowerTest` | Console regression test runner |
| `CommentReflowerHelp` | CHM help for `CommentReflower` and `CommentReflower2022`. Built with HTML Help Workshop |

`CommentReflower` and `CommentReflower2022` use the CHM help in
`CommentReflowerHelp`. The Markdown docs under `docs/`, converted from the CHM
help, are for `CommentReflowerAI` only.

## Building

Find MSBuild with `vswhere`:

```pwsh
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
```

Then build each extension project:

```pwsh
& $msbuild CommentReflowerAI\CommentReflowerAI.csproj -restore -p:Configuration=Debug -v:minimal -nologo
```

```pwsh
& $msbuild CommentReflower2022\CommentReflower2022.csproj -restore -p:Configuration=Debug -v:minimal -nologo
```

```pwsh
& $msbuild CommentReflower\CommentReflower.csproj -restore -p:Configuration=Debug -v:minimal -nologo
```

The projects are SDK-style, so C# Dev Kit in VS Code can load them. `dotnet
build` works for `CommentReflowerTest` and `CommentReflowerAI` but not for
`CommentReflower` and `CommentReflower2022`: the .NET SDK's MSBuild can't
embed the bitmaps in `Settings.resx`.
Build those with Visual Studio's MSBuild as shown above.

## Testing

`CommentReflowerTest` is a console app built with the `UNIT_TEST` symbol. It
runs the reflow engine against mock editor objects (`TestFileWrapper`,
`TestTextPoint`), reads `Input/`, writes `Output/`, and compares the output
with `Compare/`.

- It uses paths like `..\..\Input\Regression.cpp`, so run it from
  `CommentReflowerTest\bin\Debug` (or `bin\Release`).
- It waits for a key press at the end, so give it input when running it
  non-interactively.
- It always exits with code 0. Read the console output for "does not match"
  or "Test Failed" to find failures.

## Code conventions

- Match the surrounding code's naming and style. The library uses `m`-prefixed
  public fields and camelCase method names from its original authors.

## Git workflow

- `ai-generated` holds all of Claude's changes. It is permanent and will never
  be merged into `master`. Never commit to it directly.
- Anything that refers to a branch, such as links to the docs on GitHub,
  should use `ai-generated`, not `master`.
- For each change, create a topic branch from `ai-generated` with a name
  specific to the change (e.g. `docs/markdown-help`), and open a PR with base
  `ai-generated`.
- Use `--force-with-lease` whenever you force-push.

## Pull requests and commit messages

- The commit message is the PR title as the subject line, followed by the PR
  description copied exactly.
- Commit with `git commit --cleanup=verbatim`. The default cleanup deletes
  lines starting with `#`, which removes `##` headings.
- If the PR description changes, amend the commit message to match.
- Both the PR description and the commit message end with a `Co-Authored-By`
  line for the model that did the work, e.g.
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Don't add a
  "Generated with" footer.
- PR descriptions cover what changed, why, what reviewers should know, how it
  was verified, and what is not included. List every change to user-facing
  text.

## Conversions and documentation

- When converting content between formats (for example CHM/HTML to Markdown),
  keep the text exactly as it is, including typos and grammar mistakes. Change
  only the markup. Grammar and style fixes go in a separate, later change.
- Copy examples and code blocks from the source with a script, not by hand.
- Before saying text is unchanged, compare it word for word against the
  source. If a text change is unavoidable, list it in the PR.

## Reporting

- Report what was actually verified. If something wasn't built, run or checked
  (for example how Markdown looks on GitHub), say so.
- `git` warns that LF line endings will be replaced by CRLF in new files.
  That comes from `core.autocrlf` and does no harm.
