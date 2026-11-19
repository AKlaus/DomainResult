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

Pass real newlines when calling `create`, `describe`, `comment`, `reply`, `edit`, or `vote`.
A double-quoted `"\n"` reaches GitHub as visible `\n` text.

How you write a real newline depends on **the shell that parses the argument**, not on the script,
which is bash either way. Use the form for your own shell:

```bash
# bash, zsh — ANSI-C quoting
bash .agents/skills/gh-pr-review/scripts/run.sh describe 42 $'### Why\n\nThe lease...'
```

```powershell
# PowerShell — a *literal* here-string; it has no ANSI-C quoting, and $'...' arrives as text
$body = @'
### Why

The lease on $HOME is never released.
'@
bash .agents/skills/gh-pr-review/scripts/run.sh describe 42 $body
```

Use the literal `@'...'@`, not the expandable `@"..."@`. An expandable here-string substitutes
`$HOME`, `${VAR}`, and `$(...)` before bash ever sees them, so a comment quoting a shell variable,
a C# interpolated string, or a PowerShell snippet posts with the value spliced in and no warning.
Inside `@'...'@` every character is literal, apostrophes and double quotes included, so the text
itself needs no escaping.

**Run this from PowerShell 7 (`pwsh`), not Windows PowerShell 5.1.** The version matters at the
hand-off to bash, not inside the here-string. 5.1 builds the native command line without quoting
the argument, so bash applies its own word splitting and the body arrives as *many* arguments
rather than one: `He said "lease" here.` becomes `He`, `said`, `lease`, `here.`, with the quotes
dropped, the newlines gone, and non-ASCII re-encoded as mojibake. Do not try to escape your way
out of it — `\"` inside the here-string restores none of that on 5.1, and on 7 it posts the
backslashes as literal text. Check with `$PSVersionTable.PSVersion` and switch to `pwsh` if it
reads 5.

`run.sh` now rejects the split rather than truncating it. Every action that reads fixed argument
slots refuses a call carrying more, so a body split into words fails loudly instead of posting its
first word. The quoting guard in `.agents/skills/_shared/scripts/common.sh` cannot see this class
of damage — it inspects the text it is given, and after a split that text is just a short first
word — which is why the arity check sits at the entry point instead.

Never nest one form inside the other. `bash -c '... $'"'"'...'"'"''` from PowerShell does carry the
newlines, and it silently eats every apostrophe on the way: `the author's lease` posts as
`the authors lease`, with no error and nothing in the response to notice. From PowerShell 7 and
from bash, both forms above carry apostrophes, em dashes, and other non-ASCII text through
unharmed.

After posting multiline content, read it back and confirm the stored content contains no literal
`\n` and no leading `$`. PowerShell strips the apostrophes out of `$'...'` on the way to bash, so
unexpanded ANSI-C quoting arrives as a bare leading `$` — `$'### Why\n...'` posts as
`$### Why\n...`. The scripts warn on stderr when they spot either fingerprint, but the warning is a
tripwire, not a guarantee — it cannot see a character the shell dropped.
