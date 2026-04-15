# Morph Parity Validation

This directory contains the **external validation harness** that proves Morph is a viable drop-in replacement for the MIT-licensed AutoMapper (pre-v15).

## What it does

The harness:
1. Clones `AutoMapper/AutoMapper` at tag `v14.0.0` (the last MIT-licensed release) into a gitignored local directory
2. Selects a curated subset of AutoMapper's own unit tests — tests that exercise features Morph v0.1 claims to support
3. Rewrites those test files' `using AutoMapper;` declarations to `using Morph;`
4. Points their project reference at this repo's `src/Morph/Morph.csproj` instead of AutoMapper's own project
5. Compiles and runs them
6. Writes the pass/fail result to `last-run-report.md`

If the harness passes, a consumer can replace their AutoMapper v14 NuGet dependency with Morph, change `using AutoMapper;` to `using Morph;`, rebuild, and their existing code will behave identically for the features Morph v0.1 covers.

## What it does NOT do

- **It does not copy AutoMapper source into this repo.** The clone is gitignored. Morph is not a fork.
- **It does not attempt to run the full AutoMapper test suite.** Morph v0.1 has deliberately narrower scope (see `../docs/api-surface.md` for what's in v0.1 vs deferred). Running tests for `ProjectTo`, flattening, inheritance maps, etc. would fail by design.

## How to run

```bash
cd validation
./run.sh
```

Requires: `git`, `dotnet` SDK (10.x), bash (Git Bash on Windows).

## Subset selection

See `subset.txt` for the exact list of AutoMapper test files included. Each entry is accompanied by a one-line rationale mapping it to a Morph v0.1 feature. The list is deliberately small — representative coverage of the v0.1 surface, not exhaustive.

## Outcomes

- **All green** — Morph v0.1 is a drop-in replacement for AutoMapper v14 on the covered surface.
- **Some red** — a real behavioral divergence from AutoMapper. Either a bug in Morph or a deliberate design difference (see `../docs/api-surface.md` §"Breaking differences from AutoMapper"). Inspect and fix or document.
- **Compile errors** — the subset list includes a test that depends on a Morph feature not yet implemented. Narrow the subset or implement the feature.

## Legal

AutoMapper is © Jimmy Bogard; v14.0.0 and earlier are MIT-licensed. This harness uses AutoMapper's tests as an external benchmark. The cloned source never enters this repo's commit history. Morph's own source is original and MIT-licensed.
