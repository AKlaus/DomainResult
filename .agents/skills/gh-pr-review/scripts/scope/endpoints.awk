# endpoints.awk — List the HTTP endpoints of one ASP.NET Core controller file.
#
# Output, one tab-separated row per endpoint: verb, route, policy, first line, last line, file. The
# route joins the controller [Route] prefix and the action template. The policy is the action's
# own Authorize or AllowAnonymous attribute, else the controller's. The line range runs from the
# action's doc comment to its last line of code, so a changed line inside it marks the endpoint as
# changed. Blank lines, #region lines, and the class's closing brace after an action are outside it.
#
# Variables:
#   file             the path to print in each row
#   inherited_route  the [Route] prefix of a partial class whose own file declares none
#   inherited_policy the class policy of that partial class
#   mode=class       print only "route<TAB>policy" of the first class, to feed the two above
#
# This is a line scanner, not a C# parser. It relies on the layout the back-end rules produce:
# attributes on their own lines above the member, and braces that balance outside literals. A file
# can hold several controller classes.

BEGIN {
  depth = 0
  member_depth = -1
  in_attribute = 0
  in_declaration = 0
  expression_body = 0
  block_start = 0
  attributes = ""
  span_open = 0
}

# Removes string and char literals and line comments, so their braces and brackets do not count.
function strip_code(s) {
  gsub(/"([^"\\]|\\.)*"/, "", s)
  gsub(/'([^'\\]|\\.)'/, "", s)
  sub(/\/\/.*$/, "", s)
  return s
}

function open_brackets(s,   code) {
  code = strip_code(s)
  return gsub(/\[/, "", code) - gsub(/\]/, "", code)
}

function first_literal(s) {
  if (match(s, /"[^"]*"/)) return substr(s, RSTART + 1, RLENGTH - 2)
  return ""
}

# Interpolated route strings escape a literal brace as a doubled one: $"api/{{{Constants.Id}}}".
function unescape_route(s) {
  gsub(/\{\{\{/, "{", s)
  gsub(/\}\}\}/, "}", s)
  gsub(/\{\{/, "{", s)
  gsub(/\}\}/, "}", s)
  return s
}

function join(list, item) {
  return list == "" ? item : list "+" item
}

# The policies an attribute block names: Authorize(X) or Authorize(Policy = X) gives X, a bare
# Authorize gives "authenticated", and AllowAnonymous gives "anonymous". Empty when it names none.
function policies(block,   out, s, m, p, n, i, a, arg) {
  out = ""
  s = block
  while (match(s, /AllowAnonymous/)) {
    out = join(out, "anonymous")
    s = substr(s, RSTART + RLENGTH)
  }
  s = block
  while (match(s, /Authorize(Attribute)?[ \t]*(\([^)]*\))?/)) {
    m = substr(s, RSTART, RLENGTH)
    s = substr(s, RSTART + RLENGTH)
    p = ""
    if (match(m, /\(.*\)/)) {
      n = split(substr(m, RSTART + 1, RLENGTH - 2), arg, ",")
      for (i = 1; i <= n; i++) {
        a = arg[i]
        gsub(/[ \t"]/, "", a)
        if (a ~ /^Policy=/) p = substr(a, 8)
        else if (i == 1 && a != "" && a !~ /=/) p = a
      }
    }
    if (p == "") {
      p = "authenticated"
    } else {
      sub(/^.*\./, "", p)
    }
    out = join(out, p)
  }
  return out
}

# The route template that attribute arguments give: the literal, or {Constant} for a constant.
function template_of(args) {
  gsub(/(Name|Order)[ \t]*=[ \t]*("[^"]*"|[^,)]*)/, "", args)
  if (args ~ /"/) return unescape_route(first_literal(args))
  gsub(/^[ \t,]+|[ \t,]+$/, "", args)
  return args == "" ? "" : "{" args "}"
}

# The template of a [Route] attribute in the block, or empty.
function route_attribute(block,   s) {
  s = " " block
  if (!match(s, /[^A-Za-z.]Route[ \t]*\([^)]*\)/)) return ""
  s = substr(s, RSTART, RLENGTH)
  sub(/^[^(]*\(/, "", s)
  sub(/\)$/, "", s)
  return template_of(s)
}

# Fills verb[] and template[] from the block's Http* attributes and returns how many it found.
function http_verbs(block,   n, s, args) {
  n = 0
  s = block
  while (match(s, /Http(Get|Post|Put|Patch|Delete|Head|Options)/)) {
    n++
    verb[n] = toupper(substr(s, RSTART + 4, RLENGTH - 4))
    s = substr(s, RSTART + RLENGTH)
    template[n] = ""
    if (match(s, /^[ \t]*\([^)]*\)/)) {
      args = substr(s, RSTART, RLENGTH)
      sub(/^[^(]*\(/, "", args)
      sub(/\)$/, "", args)
      template[n] = template_of(args)
    }
  }
  return n
}

function combine(prefix, t, method,   r) {
  if (t ~ /^~?\//) {
    r = t
    sub(/^~?\//, "", r)
  } else if (prefix == "") {
    r = t
  } else if (t == "") {
    r = prefix
  } else {
    r = prefix "/" t
  }
  gsub(/\[controller\]/, controller, r)
  gsub(/\[action\]/, method, r)
  return r
}

function close_span(last_line,   i) {
  if (span_open) {
    for (i = 1; i <= pending; i++) print rows[i] "\t" span_start "\t" last_line "\t" file
  }
  span_open = 0
}

function start_member(first_line, declaration,   n, i, method, method_route, policy, t) {
  close_span(code_line)
  n = http_verbs(attributes)
  if (n == 0) return

  method = ""
  if (match(declaration, /[A-Za-z_][A-Za-z0-9_]*[ \t]*\(/)) {
    method = substr(declaration, RSTART, RLENGTH)
    sub(/[ \t]*\($/, "", method)
  }
  method_route = route_attribute(attributes)
  policy = policies(attributes)
  if (policy == "") policy = class_policy
  if (policy == "") policy = "none"

  pending = 0
  for (i = 1; i <= n; i++) {
    t = template[i] != "" ? template[i] : method_route
    rows[++pending] = verb[i] "\t" combine(class_route, t, method) "\t" policy
  }
  span_open = 1
  span_start = first_line
}

{
  line = $0
  sub(/\r$/, "", line)
  text = line
  sub(/^[ \t]+/, "", text)

  if (in_attribute) {
    attributes = attributes " " text
    in_attribute = open_brackets(attributes) > 0
    next
  }
  if (text == "") next
  # #region, #if and #pragma are not members. A #region line would otherwise open a declaration
  # that swallows the attributes of the action below it.
  if (text ~ /^#/) next

  at_member_level = (member_depth < 0 || depth == member_depth) && !in_declaration

  if (at_member_level && (text ~ /^\/[\/*]/ || text ~ /^\*/)) {
    if (!block_start) block_start = NR
    next
  }
  if (at_member_level && text ~ /^\[/) {
    if (!block_start) block_start = NR
    attributes = attributes " " text
    in_attribute = open_brackets(attributes) > 0
    next
  }

  if (at_member_level) {
    if (member_depth < 0) {
      if ((" " text) ~ /[ \t]class[ \t]/) {
        controller = text
        sub(/^.*class[ \t]+/, "", controller)
        match(controller, /^[A-Za-z_][A-Za-z0-9_]*/)
        controller = substr(controller, 1, RLENGTH)
        sub(/Controller$/, "", controller)
        class_route = route_attribute(attributes)
        class_policy = policies(attributes)
        if (class_route == "") class_route = inherited_route
        if (class_policy == "") class_policy = inherited_policy
        if (mode == "class") {
          print class_route "\t" class_policy
          exit
        }
        member_depth = depth + 1
      }
    } else if (text !~ /^[{}]/) {
      start_member(block_start ? block_start : NR, text)
      in_declaration = 1
    }
    attributes = ""
    block_start = 0
  }

  # A declaration can span lines, such as one parameter per line. It ends at its body or its `;`.
  # An expression body (`=>`) can wrap too, so it ends only at the `;` that closes it.
  code = strip_code(line)
  if (in_declaration && code ~ /=>/) expression_body = 1
  if (in_declaration && (code ~ /;/ || (!expression_body && code ~ /\{/))) {
    in_declaration = 0
    expression_body = 0
  }
  depth += gsub(/\{/, "", code) - gsub(/\}/, "", code)

  # The class closed: the next class in the file starts with its own route and policy. The body
  # brace can sit on the line after the declaration, so the class counts as open only once the
  # depth reaches its members.
  if (member_depth >= 0 && depth >= member_depth) class_open = 1
  if (class_open && depth < member_depth) {
    close_span(text ~ /^}[ \t;]*$/ ? code_line : NR)
    member_depth = -1
    class_open = 0
    class_route = ""
    class_policy = ""
  }

  # The last line that holds code of a member or the class. A member's doc comment and attributes
  # leave earlier, so they do not move it.
  code_line = NR
}

END {
  if (mode != "class") close_span(code_line)
}
