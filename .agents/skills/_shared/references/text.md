# Shared Text Conventions

The skills in `.agents/skills/` use these conventions. The `AGENTS.md` Communication style
section governs the prose itself.

## Title areas

A PR title and a commit subject open with `Area: `. Use the area of the code the change is about:

| Area | Code |
|---|---|
| `Common` | `src/Common`, the `DomainResult.Common` package |
| `Mvc` | `src/Mvc`, the `DomainResult` package |
| `Samples` | `samples/` |
| `Tests` | `tests/` only, with no change to a package |
| `CI` | `.github/`, and build settings such as `Directory.Build.props` |
| `Docs` | `README.md` only |
| `Agents` | `AGENTS.md` and `.agents/` |

A change to both packages takes the area of the package it is about, usually `Common`.
`.agents/skills/gh-pr-review/references/writing.md` gives the rest of a PR title.

## Multiline content

Pass real newlines in a body, such as `$'...'` in bash or zsh. A double-quoted `"\n"` posts as
visible `\n` text. From PowerShell, use PowerShell 7 and a literal here-string (`@'...'@`):
Windows PowerShell 5.1 splits the body into several arguments, which `run.sh` refuses.

After posting, read the text back. The scripts warn on stderr about a literal `\n` or a leading
`$`, but they cannot see a character that the shell dropped.
