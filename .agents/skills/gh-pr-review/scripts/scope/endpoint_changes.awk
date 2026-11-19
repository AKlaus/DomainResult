# endpoint_changes.awk — Compare the endpoints of the touched controller classes at the base and
# the head.
#
# Arguments: the changed lines; the endpoints at the base; the same at the head. An endpoints row
# is the service, then the endpoints.awk output. A changed-lines row is file, first line, last
# line, and side. "head" holds added or edited lines, which change every head action they overlap.
# "base" holds the lines of a pure deletion, which change a base action only when they lie inside
# it and leave part of it. So deleting an action's first attribute changes it. Deleting a whole
# action changes neither its neighbours nor the action itself, which another file can still hold.
#
# Endpoints compare by service, verb, and route across all touched classes, so an action that
# moves to another file of a partial class, or to a renamed controller, is not a change. The
# service is part of the key, because two services can serve one route, such as
# POST api/admin/sync.
#
# Output, one tab-separated row per endpoint that differs: change, service, endpoint
# ("VERB route"), policy. The change is
#   Added            the head has the verb and route, the base does not
#   Removed          the base has it, the head does not
#   Policy changed   the authorisation policy differs; the policy reads "old -> new"
#   Changed          a changed line falls inside the action: doc comment, attributes, signature, or body

BEGIN { FS = "\t" }

FILENAME == ARGV[1] {
  ranges++
  range_file[ranges] = $1
  range_first[ranges] = $2 + 0
  range_last[ranges] = $3 + 0
  range_rule[ranges] = $4
  next
}

FILENAME == ARGV[2] {
  key = $1 "\t" $2 " " $3
  base_policy[key] = $4
  for (i = 1; i <= ranges; i++) {
    if (range_rule[i] == "base" && range_file[i] == $7 && range_first[i] >= $5 && range_last[i] <= $6 &&
        (range_first[i] > $5 || range_last[i] < $6)) {
      base_changed[key] = 1
    }
  }
  next
}

{
  key = $1 "\t" $2 " " $3
  seen[key] = 1
  if (!(key in base_policy)) {
    print "Added\t" key "\t" $4
    next
  }
  if (base_policy[key] != $4) {
    print "Policy changed\t" key "\t" base_policy[key] " -> " $4
    next
  }
  if (key in base_changed) {
    print "Changed\t" key "\t" $4
    next
  }
  for (i = 1; i <= ranges; i++) {
    if (range_rule[i] == "head" && range_file[i] == $7 && range_first[i] <= $6 && range_last[i] >= $5) {
      print "Changed\t" key "\t" $4
      next
    }
  }
}

END {
  for (key in base_policy) {
    if (!(key in seen)) print "Removed\t" key "\t" base_policy[key]
  }
}
