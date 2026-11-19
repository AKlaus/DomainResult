# routes.awk — List the route paths a front-end routing file declares, as key rows for compare.awk.
#
# Matches a path literal: path="/a", path={'/a'}, or path: '/a'. A path built from a variable,
# such as path={e.path}, is not a literal and is not listed; the table that feeds it is.

{
  s = " " $0
  while (match(s, /[^A-Za-z]path[ \t]*[:=][ \t]*\{?[ \t]*["'`][^"'`]*["'`]/)) {
    m = substr(s, RSTART + 1, RLENGTH - 1)
    s = substr(s, RSTART + RLENGTH)
    sub(/^path[ \t]*[:=][ \t]*\{?[ \t]*["'`]/, "", m)
    sub(/["'`]$/, "", m)
    print m "\t"
  }
}
