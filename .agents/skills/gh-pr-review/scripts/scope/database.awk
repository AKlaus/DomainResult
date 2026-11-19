# database.awk — List the tables, views, functions, procedures, and types that one SQL script
# creates, alters, or drops.
#
# Output, one tab-separated row per statement: kind (table, view, function, procedure, or type),
# name, and statement:
#   created    CREATE TABLE, VIEW, TYPE, FUNCTION, or PROCEDURE
#   replaced   CREATE OR REPLACE, CREATE OR ALTER, or ALTER a view, function, or procedure
#   altered    ALTER TABLE or TYPE, CREATE INDEX or TRIGGER on the table, or sp_rename
#   data       INSERT INTO, UPDATE, or DELETE FROM the table
#   dropped    DROP TABLE, VIEW, TYPE, FUNCTION, or PROCEDURE
#
# This is a pattern scanner, not a SQL parser. It reads T-SQL and PostgreSQL as this repository
# writes them. A name drops its quotes or brackets and a dbo or public schema. The bookkeeping
# table that every migration inserts into (_migrations_history, MigrationsHistory), temporary
# #tables, and common table expressions are not listed. A function body is skipped, so the tables
# it reads or writes do not read as changed. A DO block is not a function body: its statements run
# once, in the migration. SQL that builds a statement in a string at run time, such as a loop over
# pg_tables, lists nothing.

{
  line = $0
  sub(/\r$/, "", line)
  sub(/--.*$/, "", line)
  text = text " " line
}

function emit(kind, raw, statement,   n) {
  n = raw
  gsub(/[\[\]"]/, "", n)
  sub(/^(dbo|public)\./, "", n)
  if (n == "" || n ~ /^#/ || tolower(n) ~ /migrations_?history$/) return
  print kind "\t" n "\t" statement
}

# The earliest of two positions, where 0 means not found.
function earliest(a, b) {
  if (a == 0) return b
  if (b == 0) return a
  return a < b ? a : b
}

# Marks lower[from, to) for removal. Cuts arrive in order and do not overlap.
function cut(from, to) {
  cut_from[++cuts] = from
  cut_to[cuts] = to
}

# Removes each function body from lower and cased alike, so a position in one is a position in the
# other. PostgreSQL quotes a body from "AS $tag$" to the closing "$tag$". T-SQL runs a body from the
# AS after the header to the GO that ends the batch, or to the quote that ends an EXEC string.
function skip_bodies(   base, s, tag, from, close_at, i, kept_lower, kept_cased, at) {
  cuts = 0
  base = 0
  s = lower
  while (match(s, /[ \t]as[ \t]+\$[a-z0-9_]*\$/)) {
    tag = substr(s, RSTART, RLENGTH)
    sub(/^[ \t]as[ \t]+/, "", tag)
    from = base + RSTART + 1
    base += RSTART + RLENGTH - 1
    s = substr(lower, base + 1)
    close_at = index(s, tag)
    base = close_at ? base + close_at - 1 + length(tag) : length(lower)
    cut(from, base + 1)
    s = substr(lower, base + 1)
  }
  if (cuts == 0) {
    base = 0
    s = lower
    while (match(s, /[ ('](create|alter)[ \t]+(or[ \t]+alter[ \t]+)?(function|procedure|proc|trigger)[ \t]/)) {
      base += RSTART + RLENGTH - 1
      s = substr(lower, base + 1)
      at = index(s, " as ")
      if (at == 0) break
      from = base + at + 3
      s = substr(lower, from)
      close_at = earliest(index(s, " go "), index(s, "')"))
      base = close_at ? from + close_at - 2 : length(lower)
      cut(from, base + 1)
      s = substr(lower, base + 1)
    }
  }
  kept_lower = ""
  kept_cased = ""
  at = 1
  for (i = 1; i <= cuts; i++) {
    kept_lower = kept_lower substr(lower, at, cut_from[i] - at)
    kept_cased = kept_cased substr(cased, at, cut_from[i] - at)
    at = cut_to[i]
  }
  lower = kept_lower substr(lower, at)
  cased = kept_cased substr(cased, at)
}

# The position in lower of the table behind an alias. "UPDATE j SET ... FROM Jobs j" updates Jobs.
# A common table expression ("WITH x AS (") is no table, so it gives 0. Any other name is its own
# table, at name_at. Sets resolved_length to the length of the table name.
function resolve(name, name_at, after,   base, s, end_at, m, at, from_length, alias) {
  base = after - 1
  s = substr(lower, after)
  end_at = earliest(index(s, ";"), index(s, " go "))
  if (end_at) s = substr(s, 1, end_at)
  while (match(s, "[ ,](from|join)" WS IDENT "(" WS "as)?" WS IDENT)) {
    m = substr(s, RSTART, RLENGTH)
    at = base + RSTART
    base += RSTART + RLENGTH - 1
    s = substr(s, RSTART + RLENGTH)
    match(m, IDENT "$")
    alias = substr(m, RSTART, RLENGTH)
    if (alias != name) continue
    match(m, "^[ ,](from|join)" WS)
    from_length = RLENGTH
    match(substr(m, from_length + 1), "^" IDENT)
    resolved_length = RLENGTH
    return at + from_length
  }
  s = lower
  while (match(s, "(with|,)" WS IDENT WS "as" WS "\\(")) {
    m = substr(s, RSTART, RLENGTH)
    s = substr(s, RSTART + RLENGTH)
    sub("^(with|,)" WS, "", m)
    match(m, "^" IDENT)
    if (substr(m, 1, RLENGTH) == name) return 0
  }
  resolved_length = length(name)
  return name_at
}

# Prints a row for each match of prefix, identifier, and suffix. The identifier is the name. With
# aliases set, an alias resolves to its table, and a common table expression is skipped.
function scan(prefix, suffix, kind, statement, aliases,   base, s, name_at, m, n) {
  base = 0
  s = lower
  while (match(s, prefix IDENT suffix)) {
    m = substr(s, RSTART, RLENGTH)
    name_at = base + RSTART
    base += RSTART + RLENGTH - 1
    s = substr(lower, base + 1)
    match(m, "^" prefix)
    name_at += RLENGTH
    m = substr(m, RLENGTH + 1)
    match(m, "^" IDENT)
    n = substr(m, 1, RLENGTH)
    resolved_length = length(n)
    if (aliases) name_at = resolve(n, name_at, base + 1)
    if (name_at) emit(kind, substr(cased, name_at, resolved_length), statement)
  }
}

# sp_rename 'dbo.Table' renames a table, and sp_rename 'dbo.Table.Column' a column of the table.
function scan_renames(   base, s, m, at) {
  base = 0
  s = lower
  while (match(s, /sp_rename[ \t]+n?'[^']*'/)) {
    m = substr(s, RSTART, RLENGTH)
    at = base + RSTART + index(m, "'")
    base += RSTART + RLENGTH - 1
    s = substr(lower, base + 1)
    m = substr(cased, at, RLENGTH - index(m, "'") - 1)
    sub(/^(dbo|public)\./, "", m)
    if (m ~ /\./) sub(/\.[^.]*$/, "", m)
    emit("table", m, "altered")
  }
}

END {
  gsub(/\/\*([^*]|\*+[^*\/])*\*+\//, " ", text)
  gsub(/[ \t]+/, " ", text)
  cased = " " text " "
  lower = tolower(cased)

  PART = "(\\[[^]]*\\]|\"[^\"]*\"|[a-z_#][a-z0-9_$#]*)"
  IDENT = PART "(\\." PART ")*"
  WS = "[ \t]+"

  skip_bodies()

  scan("[ (']create" WS "(unlogged" WS ")?table" WS "(if" WS "not" WS "exists" WS ")?", "", "table", "created")
  scan("[ (']alter" WS "table" WS "(if" WS "exists" WS ")?(only" WS ")?", "", "table", "altered")
  scan("[ (']drop" WS "table" WS "(if" WS "exists" WS ")?", "", "table", "dropped")
  scan("[ (']create" WS "(unique" WS ")?((non)?clustered" WS ")?index" WS "(concurrently" WS ")?(if" WS "not" WS "exists" WS ")?(" IDENT WS ")?on" WS "(only" WS ")?", "", "table", "altered")
  scan("[ (']create" WS "(or" WS "(replace|alter)" WS ")?trigger" WS IDENT WS "[a-z ,]*" WS "on" WS, "", "table", "altered")
  scan_renames()
  scan("[ (']insert" WS "into" WS, "", "table", "data", 1)
  # Both dialects allow an alias: UPDATE t alias SET, UPDATE alias SET ... FROM t alias, and
  # DELETE alias FROM t alias.
  scan("[ (']update" WS, "(" WS "[a-z_][a-z0-9_]*)?" WS "set" WS, "table", "data", 1)
  scan("[ (']delete" WS "(" IDENT WS ")?from" WS, "", "table", "data", 1)
  scan("[ (']create" WS "type" WS, "", "type", "created")
  scan("[ (']alter" WS "type" WS, "", "type", "altered")
  scan("[ (']drop" WS "type" WS "(if" WS "exists" WS ")?", "", "type", "dropped")
  scan("[ (']create" WS "(materialized" WS ")?view" WS "(if" WS "not" WS "exists" WS ")?", "", "view", "created")
  scan("[ (']create" WS "or" WS "(replace|alter)" WS "(materialized" WS ")?view" WS, "", "view", "replaced")
  scan("[ (']alter" WS "(materialized" WS ")?view" WS "(if" WS "exists" WS ")?", "", "view", "replaced")
  scan("[ (']drop" WS "(materialized" WS ")?view" WS "(if" WS "exists" WS ")?", "", "view", "dropped")
  scan("[ (']create" WS "function" WS, "", "function", "created")
  scan("[ (']create" WS "or" WS "(replace|alter)" WS "function" WS, "", "function", "replaced")
  scan("[ (']alter" WS "function" WS, "", "function", "replaced")
  scan("[ (']drop" WS "function" WS "(if" WS "exists" WS ")?", "", "function", "dropped")
  scan("[ (']create" WS "(procedure|proc)" WS, "", "procedure", "created")
  scan("[ (']create" WS "or" WS "(replace|alter)" WS "(procedure|proc)" WS, "", "procedure", "replaced")
  scan("[ (']alter" WS "(procedure|proc)" WS, "", "procedure", "replaced")
  scan("[ (']drop" WS "(procedure|proc)" WS "(if" WS "exists" WS ")?", "", "procedure", "dropped")
}
