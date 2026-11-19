# msbuild.awk — List the references an MSBuild file declares, as key<TAB>value rows for compare.awk.
#
# Variables:
#   kind=version   PackageVersion name and version, as Directory.Packages.props declares them
#   kind=package   PackageReference name, and its version when the project pins one
#   kind=project   ProjectReference folder, resolved to a repository path
#   dir            the folder of the file, against which a ProjectReference resolves

function attribute(s, name,   pattern) {
  pattern = name "=\"[^\"]*\""
  if (match(s, pattern)) return substr(s, RSTART + length(name) + 2, RLENGTH - length(name) - 3)
  return ""
}

# ..\..\lib\Foo\Foo.csproj seen from admin/Api resolves to lib/Foo. The project file name is
# dropped, because the folder is what the rest of the repository names.
function resolve(base, relative,   parts, n, kept, m, i, out) {
  gsub(/\\/, "/", relative)
  if (relative ~ /^\$\(/) return relative
  n = split(base "/" relative, parts, "/")
  m = 0
  for (i = 1; i <= n; i++) {
    if (parts[i] == "" || parts[i] == ".") continue
    if (parts[i] == "..") {
      if (m > 0) m--
      continue
    }
    kept[++m] = parts[i]
  }
  out = ""
  for (i = 1; i < m; i++) out = out (out == "" ? "" : "/") kept[i]
  return out
}

kind == "version" && /<PackageVersion[ \t]/ {
  name = attribute($0, "Include")
  if (name == "") name = attribute($0, "Update")
  if (name != "") print name "\t" attribute($0, "Version")
}

kind == "package" && /<PackageReference[ \t]/ {
  name = attribute($0, "Include")
  if (name != "") print name "\t" attribute($0, "Version")
}

kind == "project" && /<ProjectReference[ \t]/ {
  name = attribute($0, "Include")
  if (name != "") print resolve(dir, name) "\t"
}
