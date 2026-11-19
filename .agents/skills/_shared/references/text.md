# Shared Text Conventions

The skills in `.agents/skills/` use these conventions for the text they post.

## Multiline content

Pass real newlines in a body, such as `$'...'` in bash or zsh. A double-quoted `"\n"` posts as
visible `\n` text. From PowerShell, use PowerShell 7 and a literal here-string (`@'...'@`):
Windows PowerShell 5.1 splits the body into several arguments, which `run.sh` refuses.

After posting, read the text back. The scripts warn on stderr about a literal `\n` or a leading
`$`, but they cannot see a character that the shell dropped.
