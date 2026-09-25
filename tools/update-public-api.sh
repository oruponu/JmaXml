#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
unshipped=src/JmaXml/PublicAPI.Unshipped.txt

if ! log=$(DOTNET_CLI_UI_LANGUAGE=en dotnet build src/JmaXml --no-incremental -p:TreatWarningsAsErrors=false 2>&1); then
  printf '%s\n' "$log" >&2
  echo "Build failed. $unshipped was not updated." >&2
  exit 1
fi

added=$(printf '%s\n' "$log" | awk -v q="'" '
  /warning RS0016:/ {
    prefix = "Symbol " q
    suffix = q " is not part of the declared public API"
    start = index($0, prefix)
    end = index($0, suffix)
    if (start == 0 || end <= start) {
      print "Unexpected RS0016 message: " $0 > "/dev/stderr"
      failed = 1
      next
    }
    print substr($0, start + length(prefix), end - start - length(prefix))
  }
  END { exit failed }' | LC_ALL=C sort -u)

{
  printf '#nullable enable\n'
  {
    tail -n +2 "$unshipped"
    if [ -n "$added" ]; then printf '%s\n' "$added"; fi
  } | LC_ALL=C sort -u
} > "$unshipped.tmp"
mv "$unshipped.tmp" "$unshipped"
echo "Added $(printf '%s' "$added" | grep -c . || true) symbols to $unshipped."
