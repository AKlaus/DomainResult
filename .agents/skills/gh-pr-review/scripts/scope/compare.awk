# compare.awk — Compare two key<TAB>value lists: the base (first file) and the head (second file).
#
# Output, one tab-separated row per difference: change, key, value. The change is Added, Removed,
# or Changed; a Changed value reads "old -> new". A key with the same value on both sides prints
# nothing. Either file can be empty, as it is for a file the change adds or deletes.

BEGIN { FS = "\t" }

$1 == "" { next }

FILENAME == ARGV[1] {
  base[$1] = $2
  next
}

{
  seen[$1] = 1
  if (!($1 in base)) {
    print "Added\t" $1 "\t" $2
  } else if (base[$1] != $2) {
    print "Changed\t" $1 "\t" base[$1] " -> " $2
  }
}

END {
  for (key in base) {
    if (!(key in seen)) print "Removed\t" key "\t" base[key]
  }
}
