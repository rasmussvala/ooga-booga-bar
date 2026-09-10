#!/usr/bin/env bash
# Unity project hygiene checks. Runs in CI and locally: ./scripts/check-project.sh
set -uo pipefail
cd "$(dirname "$0")/.."

fail=0
report() { echo "::error::$1"; fail=1; }

# Files/folders Unity itself ignores and therefore never generates a .meta for.
is_ignored() {
  case "$1" in
    */.*|.*) return 0 ;;   # hidden
    *~) return 0 ;;
    *.tmp) return 0 ;;
    */cvs|*/CVS) return 0 ;;
  esac
  return 1
}

echo "==> Checking .meta files"
while IFS= read -r path; do
  is_ignored "$path" && continue
  case "$path" in *.meta) continue ;; esac
  [ -e "$path.meta" ] || report "missing meta: $path.meta"
done < <(find Assets -mindepth 1 \( -type f -o -type d \) 2>/dev/null)

while IFS= read -r meta; do
  asset="${meta%.meta}"
  [ -e "$asset" ] || report "orphan meta (asset deleted?): $meta"
done < <(find Assets -name '*.meta' 2>/dev/null)

echo "==> Checking for unresolved merge conflicts"
if git grep -In -e '^<<<<<<< ' -e '^>>>>>>> ' -- Assets ProjectSettings Packages >/dev/null 2>&1; then
  git grep -In -e '^<<<<<<< ' -e '^>>>>>>> ' -- Assets ProjectSettings Packages
  report "merge conflict markers committed"
fi

echo "==> Checking for oversized non-LFS files (>20MB)"
while IFS= read -r f; do
  [ -f "$f" ] || continue
  size=$(wc -c < "$f")
  if [ "$size" -gt 20971520 ]; then
    report "file over 20MB ($((size/1048576))MB): $f -- compress it or add its type to .gitattributes"
  fi
done < <(git ls-files)

echo "==> Checking Git LFS"
if ! git lfs env >/dev/null 2>&1; then
  report "git-lfs not available -- GitHub Desktop bundles it; from a terminal run: brew install git-lfs && git lfs install"
else
  # A binary matching a filter=lfs rule in .gitattributes but stored as a raw
  # blob means someone committed it without git-lfs active on their machine.
  lfs_tracked=$(git lfs ls-files --name-only | sort)
  while IFS= read -r f; do
    case "$(git check-attr filter -- "$f")" in
      *": filter: lfs") ;;
      *) continue ;;
    esac
    grep -qxF "$f" <<< "$lfs_tracked" || report "committed outside LFS: $f -- run 'git lfs install' then 'git add --renormalize .'"
  done < <(git ls-files)
fi

echo "==> Checking for duplicate asset GUIDs"
dupes=$(grep -h '^guid: ' $(git ls-files '*.meta') 2>/dev/null | sort | uniq -d)
if [ -n "$dupes" ]; then
  echo "$dupes"
  report "duplicate GUIDs across .meta files (copy-pasted meta breaks references)"
fi

[ "$fail" -eq 0 ] && echo "All checks passed." || echo "Checks failed."
exit $fail
