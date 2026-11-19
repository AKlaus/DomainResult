# classify.awk — Sort each file of a `git diff --numstat -M` listing into one review category.
#
# Output, one tab-separated row per file: category, added, deleted, path. The categories are
#   binary        Git reports no line counts
#   generated     generated front-end clients and test snapshots
#   auxiliary     lockfiles, solution files, repository settings such as .gitignore, and
#                 experiments and tooling: anything under .agents/, .claude/, sandbox/, or a
#                 *ScratchPad* folder
#   docs          every Markdown file, and docs/ folders
#   test          test projects, test folders, and JavaScript or TypeScript *.test.* or *.spec.* files
#   production    everything else; migration scripts and the migration classes that run them included
# references/scope.md says which categories count against the budget. pr_scope.sh reads the
# category from here only, so a path pattern lives in one place.

# A renamed file arrives as "old => new" or "dir/{old => new}/file". The new path is the one that
# the rest of the report, and GitHub, names.
function new_path(p,   inner) {
  if (index(p, " => ") == 0) return p
  if (match(p, /\{[^}]* => [^}]*\}/)) {
    inner = substr(p, RSTART + 1, RLENGTH - 2)
    sub(/^.* => /, "", inner)
    p = substr(p, 1, RSTART - 1) inner substr(p, RSTART + RLENGTH)
    gsub(/\/\/+/, "/", p)
    sub(/^\//, "", p)
    return p
  }
  sub(/^.* => /, "", p)
  return p
}

BEGIN { FS = "\t" }

NF < 3 { next }

{
  added = $1
  deleted = $2
  path = new_path($3)
  # A leading slash lets every pattern match a folder name at the start of the path too.
  q = "/" path

  if (added == "-") {
    category = "binary"
    added = 0
    deleted = 0
  } else if (q ~ /\.gen\.[cm]?[jt]sx?$/ || q ~ /\.snap$/) {
    category = "generated"
  } else if (q ~ /\/(yarn\.lock|package-lock\.json|packages\.lock\.json|pnpm-lock\.yaml|poetry\.lock|uv\.lock)$/ ||
             q ~ /\.(sln|slnf|DotSettings|DotSettings\.user)$/ || q ~ /^\/\.yarn\// ||
             q ~ /\/\.(gitignore|gitattributes|editorconfig|eslintignore|prettierignore|yarnrc\.yml|DS_Store)$/ ||
             q ~ /\/\.prettierrc[^\/]*$/ ||
             q ~ /^\/(\.agents|\.claude|sandbox)\// || q ~ /\/[^\/]*[Ss]cratch[Pp]ad[^\/]*\//) {
    category = "auxiliary"
  } else if (q ~ /\.md$/ || q ~ /\/docs\//) {
    category = "docs"
  } else if (q ~ /\/[^\/]*\.Tests?\// || q ~ /\/[Tt]ests?\// || q ~ /\/__tests__\// ||
             q ~ /\.(test|spec)\.[cm]?[jt]sx?$/ || q ~ /\/test_[^\/]*\.py$/ || q ~ /_test\.py$/) {
    category = "test"
  } else {
    category = "production"
  }

  printf "%s\t%d\t%d\t%s\n", category, added, deleted, path
}
