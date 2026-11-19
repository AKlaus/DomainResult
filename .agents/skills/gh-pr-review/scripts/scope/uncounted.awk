# uncounted.awk — Count the changed lines of a `git diff -M` patch that the budget does not count.
#
# Output, one tab-separated row per file that has such lines: uncounted lines (added plus
# deleted), path. pr_scope.sh subtracts them from the counts of classify.awk. Two kinds of line
# do not count:
#   comment   a line that holds only a comment, or a line inside a block comment, in a file type
#             whose comment syntax comment_syntax() knows. A code line with a trailing comment
#             counts.
#   rename    a deleted line and the added line that replaces it, if the two differ only in
#             identifiers, no replaced identifier remains in the code of the new file, no
#             replacing identifier occurs in the code of the old file, and another such line pair
#             in the diff makes the same replacement.
# The patch must carry whole files as context (-U with a large number), so that the block-comment
# state is exact from line 1 and both files are known in full. references/scope.md explains both
# kinds.

BEGIN {
  # A swap between two of these, in any case, changes behaviour, so it is never a rename.
  split("true false null undefined none nil public private protected internal " \
        "static readonly const let var void async await sealed abstract virtual override new " \
        "int uint long ulong short ushort byte sbyte float double decimal bool boolean string " \
        "char object number bigint any unknown never and or not is as in if else while for " \
        "return break continue throw", words, " ")
  for (i in words) keyword[words[i]] = 1
}

# ── Patch structure ──────────────────────────────────────────────────────────────────────────────

/^diff --git / {
  end_file()
  in_header = 1
  path = ""
  next
}

in_header && /^--- a\// {
  path = strip_path(substr($0, 7))
  next
}

in_header && /^\+\+\+ b\// {
  path = strip_path(substr($0, 7))
  next
}

/^@@ / {
  flush_block()
  if (in_header) comment_syntax(path)
  in_header = 0
  open_block["old"] = 0
  open_block["new"] = 0
  next
}

in_header { next }

/^ / {
  flush_block()
  text = substr($0, 2)
  if (!is_comment(text, "old")) add_words(text, old_words)
  if (!is_comment(text, "new")) add_words(text, new_words)
  next
}

/^-/ {
  if (added_count > 0) flush_block()
  text = substr($0, 2)
  if (is_comment(text, "old")) {
    uncounted[path]++
  } else {
    deleted_line[++deleted_count] = text
    add_words(text, old_words)
  }
  next
}

/^\+/ {
  text = substr($0, 2)
  if (is_comment(text, "new")) {
    uncounted[path]++
  } else {
    added_line[++added_count] = text
    add_words(text, new_words)
  }
  next
}

END {
  end_file()
  for (i = 1; i <= pair_count; i++) {
    n = split(pair_keys[i], keys, SUBSEP)
    renamed = 1
    for (k = 1; k <= n; k++) {
      if (key_pairs[keys[k]] < 2) { renamed = 0; break }
    }
    if (renamed) uncounted[pair_path[i]] += 2
  }
  for (p in uncounted) printf "%d\t%s\n", uncounted[p], p
}

# Git appends a tab to a ---/+++ path that holds a space.
function strip_path(p) {
  sub(/\t$/, "", p)
  return p
}

# ── Comments ─────────────────────────────────────────────────────────────────────────────────────

# Sets the line-comment prefixes and the block-comment delimiters of the file type of p.
function comment_syntax(p,   name, ext) {
  line1 = ""; line2 = ""; block_open = ""; block_close = ""
  name = tolower(p)
  sub(/^.*\//, "", name)
  ext = name
  if (!sub(/^.*\./, "", ext)) ext = ""

  if (ext ~ /^(cs|ts|tsx|mts|cts|js|jsx|mjs|cjs|scss|less|css|bicep|java|kt|go|rs|c|h|cpp|hpp|swift|proto)$/) {
    line1 = "//"; block_open = "/*"; block_close = "*/"
  } else if (ext ~ /^(sql|pgsql|psql)$/) {
    line1 = "--"; block_open = "/*"; block_close = "*/"
  } else if (ext ~ /^(ps1|psm1|psd1)$/) {
    line1 = "#"; block_open = "<#"; block_close = "#>"
  } else if (ext ~ /^(py|sh|bash|zsh|yaml|yml|toml|conf|r|rb|tf|dockerfile)$/ || name ~ /^(dockerfile|makefile)/) {
    line1 = "#"
  } else if (ext ~ /^(ini|cfg)$/) {
    line1 = ";"; line2 = "#"
  } else if (ext ~ /^(xml|csproj|props|targets|config|resx|nuspec|xaml|html|htm|svg)$/) {
    block_open = "<!--"; block_close = "-->"
  }
}

# Returns 1 if the line holds only comment text on one side ("old" or "new") of the patch, and
# carries that side's block-comment state to the next line. A block comment opens only at the
# start of a line, so a "/*" inside a string never opens one.
function is_comment(s, side,   p) {
  sub(/^[ \t]+/, "", s)
  sub(/[ \t\r]+$/, "", s)
  if (open_block[side]) {
    p = index(s, block_close)
    if (p == 0) return 1
    open_block[side] = 0
    return rest_is_comment(substr(s, p + length(block_close)), side)
  }
  return s != "" && starts_comment(s, side)
}

function rest_is_comment(s, side) {
  sub(/^[ \t]+/, "", s)
  return s == "" || starts_comment(s, side)
}

function starts_comment(s, side,   p) {
  if (line1 != "" && substr(s, 1, length(line1)) == line1) return 1
  if (line2 != "" && substr(s, 1, length(line2)) == line2) return 1
  if (block_open == "" || substr(s, 1, length(block_open)) != block_open) return 0
  s = substr(s, length(block_open) + 1)
  p = index(s, block_close)
  if (p == 0) {
    open_block[side] = 1
    return 1
  }
  return rest_is_comment(substr(s, p + length(block_close)), side)
}

# ── Renames ──────────────────────────────────────────────────────────────────────────────────────

# Pairs the deleted and added code lines of one change block in order, and keeps each pair that
# differs only in identifiers until end_file() knows both files in full.
function flush_block(   i, n, keys) {
  n = deleted_count < added_count ? deleted_count : added_count
  for (i = 1; i <= n; i++) {
    keys = rename_keys(deleted_line[i], added_line[i])
    if (keys == "") continue
    file_pair_keys[++file_pair_count] = keys
    file_pair_replaced[file_pair_count] = replaced
    file_pair_replacing[file_pair_count] = replacing
  }
  deleted_count = 0
  added_count = 0
}

# Records the pairs of the file that ends here that swap an old name for a new one. A replaced
# identifier that remains in the new file, such as a [Fact] that became [Theory], or a replacing
# identifier that the old file already used, such as orderedFields that became fields, marks an
# edit, not a rename. END decides which recorded pairs are renames.
function end_file(   i, j, n, key) {
  flush_block()
  for (i = 1; i <= file_pair_count; i++) {
    if (any_in(file_pair_replaced[i], new_words) || any_in(file_pair_replacing[i], old_words)) continue
    pair_path[++pair_count] = path
    pair_keys[pair_count] = file_pair_keys[i]
    n = split(file_pair_keys[i], key, SUBSEP)
    for (j = 1; j <= n; j++) key_pairs[key[j]]++
  }
  file_pair_count = 0
  split("", old_words)
  split("", new_words)
}

# Returns 1 if any of the SUBSEP-joined words is in set.
function any_in(words, set,   n, i, w) {
  n = split(words, w, SUBSEP)
  for (i = 1; i <= n; i++) {
    if (w[i] in set) return 1
  }
  return 0
}

# Adds the words of a code line to set.
function add_words(s, set,   n, i, w) {
  n = split(s, w, /[^A-Za-z0-9_]+/)
  for (i = 1; i <= n; i++) set[w[i]] = 1
}

# Returns the replacements that turn line d into line a, joined by SUBSEP, or "" if the two lines
# differ in anything but identifiers. Whitespace does not matter. Each pair counts a replacement
# once, so a replacement must recur in another pair to count as a rename. Sets replaced and
# replacing to the identifiers of d and of a that differ, each joined by SUBSEP.
function rename_keys(d, a,   dt, at, n, i, k, keys) {
  n = tokenise(d, dt)
  if (n != tokenise(a, at)) return ""
  keys = ""
  replaced = ""
  replacing = ""
  for (i = 1; i <= n; i++) {
    if (dt[i] == at[i]) continue
    if (dt[i] !~ /^[A-Za-z_]/ || at[i] !~ /^[A-Za-z_]/) return ""
    if ((tolower(dt[i]) in keyword) || (tolower(at[i]) in keyword)) return ""
    k = rename_key(dt[i], at[i])
    # A swap of digits only, such as net8 to net9 or v1 to v2, changes a version, not a name.
    if (k ~ /^[0-9]*\t[0-9]*$/) return ""
    replaced = replaced == "" ? dt[i] : replaced SUBSEP dt[i]
    replacing = replacing == "" ? at[i] : replacing SUBSEP at[i]
    if (index(SUBSEP keys SUBSEP, SUBSEP k SUBSEP)) continue
    keys = keys == "" ? k : keys SUBSEP k
  }
  return keys
}

# Reduces a replacement to the part that changed, so that Widget -> Gadget, widgetId -> gadgetId,
# and IWidgetService -> IGadgetService are one replacement. A change of case only keeps its case.
function rename_key(o, n,   lo, ln, key) {
  lo = tolower(o)
  ln = tolower(n)
  key = changed_part(lo, ln)
  return key == "\t" ? changed_part(o, n) : key
}

function changed_part(o, n,   lo, ln) {
  while (o != "" && n != "" && substr(o, 1, 1) == substr(n, 1, 1)) {
    o = substr(o, 2)
    n = substr(n, 2)
  }
  while ((lo = length(o)) > 0 && (ln = length(n)) > 0 && substr(o, lo, 1) == substr(n, ln, 1)) {
    o = substr(o, 1, lo - 1)
    n = substr(n, 1, ln - 1)
  }
  return o "\t" n
}

# Splits s into words (letters, digits, and underscores) and single other characters.
function tokenise(s, tok,   n) {
  n = 0
  while (s != "") {
    if (match(s, /^[ \t\r]+/)) {
      s = substr(s, RLENGTH + 1)
    } else if (match(s, /^[A-Za-z0-9_]+/)) {
      tok[++n] = substr(s, 1, RLENGTH)
      s = substr(s, RLENGTH + 1)
    } else {
      tok[++n] = substr(s, 1, 1)
      s = substr(s, 2)
    }
  }
  return n
}
