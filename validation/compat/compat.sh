#!/usr/bin/env bash
# Compat harness — proves Morph is a drop-in for AutoMapper v14 on the scenarios covered
# in shared/Tests/CompatTests.cs.
#
# Flow:
#   1. Build + test shared/ against Morph (Consumer.Morph.csproj)
#   2. Rewrite `using Morph;` → `using AutoMapper;` into gen/
#   3. Build + test gen/ against AutoMapper 14.0.0 NuGet (Consumer.AutoMapper.csproj)
#   4. Report pass/fail for both legs
#
# If both green, the scenarios in CompatTests are drop-in compatible.

set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPORT="$HERE/compat-report.md"
MORPH_LOG="$HERE/morph-leg.log"
AM_LOG="$HERE/automapper-leg.log"

echo "=== Morph/AutoMapper-v14 compat harness ==="

# --- Leg 1: Morph ---
echo "[1/3] Building + testing Morph leg…"
set +e
dotnet test "$HERE/Consumer.Morph.csproj" -c Debug --nologo --logger:"console;verbosity=normal" > "$MORPH_LOG" 2>&1
MORPH_RC=$?
MORPH_SUMMARY=$(grep -E "^(Total tests|     Passed|     Failed|     Skipped):" "$MORPH_LOG" | tr '\n' ' ')
MORPH_SUMMARY="${MORPH_SUMMARY:-no-summary}"
set -e

# --- Leg 2: rewrite to AutoMapper ---
echo "[2/3] Rewriting shared/ → gen/ with 'using Morph;' → 'using AutoMapper;'…"
rm -rf "$HERE/gen"
mkdir -p "$HERE/gen"
# Copy shared/ tree preserving structure, rewriting .cs files. Use an array to avoid
# subshell/pipe pitfalls under `set -e`.
mapfile -t REL_FILES < <(cd "$HERE/shared" && find . -type f -name '*.cs')
for rel in "${REL_FILES[@]}"; do
  rel="${rel#./}"
  mkdir -p "$HERE/gen/$(dirname "$rel")"
  sed 's|^using Morph;|using AutoMapper;|g' \
    "$HERE/shared/$rel" > "$HERE/gen/$rel"
done
echo "  rewrote ${#REL_FILES[@]} files into gen/"

# --- Leg 3: AutoMapper ---
echo "[3/3] Building + testing AutoMapper v14 leg…"
set +e
dotnet test "$HERE/Consumer.AutoMapper.csproj" -c Debug --nologo --logger:"console;verbosity=normal" > "$AM_LOG" 2>&1
AM_RC=$?
AM_SUMMARY=$(grep -E "^(Total tests|     Passed|     Failed|     Skipped):" "$AM_LOG" | tr '\n' ' ')
AM_SUMMARY="${AM_SUMMARY:-no-summary}"
set -e

# --- Report ---
RESULT="UNKNOWN"
if [ $MORPH_RC -eq 0 ] && [ $AM_RC -eq 0 ]; then
  RESULT="**PASS** — both legs green, drop-in compat proved on covered scenarios"
elif [ $MORPH_RC -ne 0 ] && [ $AM_RC -eq 0 ]; then
  RESULT="**MORPH FAIL** — AutoMapper v14 passes but Morph fails. Real Morph regression."
elif [ $MORPH_RC -eq 0 ] && [ $AM_RC -ne 0 ]; then
  RESULT="**AUTOMAPPER FAIL** — Morph passes but AutoMapper v14 fails. Fixture bug."
else
  RESULT="**BOTH FAIL** — fixture likely wrong."
fi

cat > "$REPORT" <<EOF
# Morph/AutoMapper-v14 Compat Report

Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)

Result: $RESULT

## Leg 1 — Morph
\`\`\`
$MORPH_SUMMARY
\`\`\`

## Leg 2 — AutoMapper 14.0.0 NuGet
\`\`\`
$AM_SUMMARY
\`\`\`

## Interpretation
If both legs show the same number of passes, the scenarios in \`shared/Tests/CompatTests.cs\`
behave identically under AutoMapper v14 and Morph. That's the drop-in claim for the covered
surface.

See \`morph-leg.log\` and \`automapper-leg.log\` for per-test output.
EOF

echo
echo "=== Report: $REPORT ==="
cat "$REPORT"
echo
[ $MORPH_RC -eq 0 ] && [ $AM_RC -eq 0 ] && exit 0 || exit 1
