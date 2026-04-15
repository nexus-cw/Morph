# Morph / AutoMapper v14 Compat Harness

This directory contains the **drop-in compatibility harness** that proves Morph behaves identically to AutoMapper v14 on realistic consumer scenarios.

## What it does

One set of consumer code (`shared/`) is compiled and tested **twice**:

1. **Morph leg** (`Consumer.Morph.csproj`) — refs `../../src/Morph/Morph.csproj`
2. **AutoMapper leg** (`Consumer.AutoMapper.csproj`) — refs `AutoMapper` 14.0.0 NuGet, after `compat.sh` sed-rewrites `using Morph;` → `using AutoMapper;` into `gen/`

Both projects run the same xunit test file. Green on both → the scenarios in `shared/Tests/CompatTests.cs` are drop-in compatible: a real v14 consumer changing their `using AutoMapper;` line to `using Morph;` will see identical behavior for these scenarios.

## Current coverage

21 tests across these scenarios (all Morph v0.1 in-scope):

| Scenario | Tests |
|----------|-------|
| Property-by-name convention | 3 |
| `ForMember` + `MapFrom` (lambda, expression, enum→string) | 3 |
| Collections (List, empty, nested) | 2 |
| `ReverseMap` | 2 |
| `ConstructUsing` | 1 |
| `ConvertUsing<T>` / `ITypeConverter<,>` | 1 |
| `Map(source, existingDestination)` overload | 1 |
| Multi-profile registration | 1 |
| Runtime-type dispatch | 2 |
| Primitive coercion (decimal, DateTime) | 2 |
| `AssertConfigurationIsValid` | 1 |
| End-to-end flow | 2 |

## How to run

```bash
cd validation/compat
./compat.sh
```

Requires `dotnet` 10.x and bash. First run pulls AutoMapper 14.0.0 from NuGet.

## Interpreting the report

`compat-report.md` (regenerated each run) shows both legs' test counts.

- **Both green, same count** — drop-in compat proved on covered scenarios
- **Morph fails, AutoMapper passes** — a real Morph regression. Fix Morph.
- **Morph passes, AutoMapper fails** — the fixture uses an API pattern that isn't valid v14 code. Fix the fixture.
- **Both fail** — fixture is wrong.

## What this does NOT prove

- Drop-in compat on features Morph v0.1 doesn't implement (`ProjectTo`, flattening, inheritance maps, `ForCtorParam`, naming conventions, open generics, etc.)
- Behavioral identity on every possible edge case — only the enumerated scenarios
- Performance parity — this is a correctness harness, not a benchmark

## Why this shape, not "run AutoMapper's tests against Morph"

AutoMapper's own tests reach deep into internal types (`IGlobalConfiguration`, `TypeMap`, `PropertyMap`, `ProjectTo`) that Morph v0.1 intentionally doesn't expose. A "run their tests" harness fails at compile on those internals, which says nothing about drop-in compatibility for actual consumer code.

This harness flips that: the code is **what a consumer writes** (Profiles, `Map<T>` calls, Shouldly assertions). If that code runs identically under both libraries, a migration works. That's the question this answers.

## Legal

AutoMapper 14.0.0 is © Jimmy Bogard, MIT-licensed. This harness pulls it from NuGet as a runtime dependency only — AutoMapper source is not copied into Morph. The shared consumer code under `shared/` is original and MIT-licensed (same as Morph).
