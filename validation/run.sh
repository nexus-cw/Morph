#!/usr/bin/env bash
# Morph parity harness — runs curated AutoMapper v14.0.0 tests against Morph.
# See README.md for what this does and why. Tests subset defined in subset.md.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$HERE/.." && pwd)"
AM_DIR="$HERE/automapper-v14"
HARNESS_DIR="$HERE/harness"
TAG="v14.0.0"

# Subset — see subset.md for rationale. Keep in sync.
SUBSET_FILES=(
  "CustomMapping.cs"
  "Profiles.cs"
  "ReverseMapping.cs"
  "Constructors.cs"
  "TypeConverters.cs"
  "ConditionalMapping.cs"
  "FillingExistingDestination.cs"
  "General.cs"
)

echo "=== Morph parity harness ==="
echo "Repo root: $REPO_ROOT"

# Step 1 — clone AutoMapper v14.0.0 if not already present.
if [ ! -d "$AM_DIR" ]; then
  echo "[1/5] Cloning AutoMapper@$TAG into $AM_DIR (shallow)…"
  git clone --depth 1 --branch "$TAG" https://github.com/AutoMapper/AutoMapper.git "$AM_DIR"
else
  echo "[1/5] AutoMapper clone already present — reusing $AM_DIR"
fi

# Step 2 — prepare harness temp project.
echo "[2/5] Preparing harness temp project…"
rm -rf "$HARNESS_DIR"
mkdir -p "$HARNESS_DIR/Imported"

cat > "$HARNESS_DIR/Harness.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <NoWarn>\$(NoWarn);CS8019;CS0168;CS8632</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Shouldly" Version="4.2.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\Morph\\Morph.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="Shouldly" />
    <Using Include="Morph" />
  </ItemGroup>
</Project>
EOF

# Step 3 — import subset files with using-rewrites.
echo "[3/5] Importing ${#SUBSET_FILES[@]} subset files…"
for f in "${SUBSET_FILES[@]}"; do
  src="$AM_DIR/src/UnitTests/$f"
  if [ ! -f "$src" ]; then
    echo "  MISSING: $f (not found at $src)"
    continue
  fi
  dst="$HARNESS_DIR/Imported/$f"
  # Rewrite: using AutoMapper; -> using Morph; and AutoMapper.* -> Morph.* for namespaces we support
  sed \
    -e 's|^using AutoMapper;|using Morph;|g' \
    -e 's|^using AutoMapper\.\([A-Za-z0-9_]*\);|// using AutoMapper.\1;  // no Morph equivalent|g' \
    "$src" > "$dst"
  echo "  imported: $f"
done

# Also import AutoMapperSpecBase if any subset file uses it (common fixture).
if grep -lq "AutoMapperSpecBase" "$HARNESS_DIR/Imported/"*.cs 2>/dev/null; then
  echo "  one or more subset files use AutoMapperSpecBase — importing fixture"
  base_src="$AM_DIR/src/UnitTests/AutoMapperSpecBase.cs"
  if [ -f "$base_src" ]; then
    sed -e 's|^using AutoMapper;|using Morph;|g' "$base_src" > "$HARNESS_DIR/Imported/AutoMapperSpecBase.cs"
  fi
fi

# Step 4 — build harness.
echo "[4/5] Building harness against Morph…"
BUILD_LOG="$HERE/harness-build.log"
set +e
dotnet build "$HARNESS_DIR/Harness.csproj" -c Debug --nologo > "$BUILD_LOG" 2>&1
BUILD_RC=$?
set -e

if [ $BUILD_RC -ne 0 ]; then
  echo "  BUILD FAILED — see $BUILD_LOG"
  echo "  This usually means the subset includes a test that uses a Morph feature not in v0.1."
  echo "  Narrow subset.sh SUBSET_FILES or extend Morph."
  tail -40 "$BUILD_LOG"
  # Report the failure but still write a report.
  cat > "$HERE/last-run-report.md" <<EOF
# Morph Parity Run Report (BUILD FAILED)

Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)
AutoMapper tag: $TAG
Result: **BUILD FAILED**

See \`harness-build.log\` for compiler output. Most likely cause: a subset file references
an AutoMapper API not present in Morph v0.1. Either:
- narrow \`subset.md\` to only files that compile against Morph's current surface, or
- implement the missing feature and rerun.
EOF
  exit 1
fi

# Step 5 — run tests, capture result.
echo "[5/5] Running harness tests…"
TEST_LOG="$HERE/harness-test.log"
set +e
dotnet test "$HARNESS_DIR/Harness.csproj" -c Debug --nologo --logger:"console;verbosity=normal" > "$TEST_LOG" 2>&1
TEST_RC=$?
set -e

# Extract the summary line from test log.
SUMMARY=$(grep -E "^(Passed!|Failed!)" "$TEST_LOG" || echo "no-summary")

cat > "$HERE/last-run-report.md" <<EOF
# Morph Parity Run Report

Date: $(date -u +%Y-%m-%dT%H:%M:%SZ)
AutoMapper tag: $TAG
Subset size: ${#SUBSET_FILES[@]} files — see \`subset.md\`
Result: $([ $TEST_RC -eq 0 ] && echo '**PASS**' || echo '**FAIL**')

## Summary

\`\`\`
$SUMMARY
\`\`\`

## Full log

See \`harness-test.log\` for per-test detail.

## Interpretation

- **PASS:** Morph behaves identically to AutoMapper v14 on every test in the curated subset. The v0.1 drop-in claim holds on the covered surface.
- **FAIL:** See the log and cross-check against \`expected-deviations.md\`. Known-intentional divergences go there; everything else is a real regression.
EOF

echo
echo "=== Report written to $HERE/last-run-report.md ==="
cat "$HERE/last-run-report.md"

exit $TEST_RC
