# Curated AutoMapper v14.0.0 Test Subset

The harness runs only these test files from `AutoMapper/AutoMapper@v14.0.0`. Each is picked because its scenarios map to a Morph v0.1 in-scope feature and because the test's API usage stays within Morph's v0.1 surface (no `ProjectTo`, no flattening, no inheritance maps, no value transformers).

Tests outside this list are **not failures** — they exercise v0.2+ features by design. See `../docs/api-surface.md` §v0.1 in/out.

## Subset

| AutoMapper file                      | Morph v0.1 feature covered                                                |
|--------------------------------------|---------------------------------------------------------------------------|
| `CustomMapping.cs`                   | `ForMember`, `MapFrom`, custom resolvers                                  |
| `Profiles.cs`                        | `Profile` subclassing, `AddProfile`                                       |
| `ReverseMapping.cs`                  | `ReverseMap()` round-trip                                                 |
| `Constructors.cs`                    | `ConstructUsing` + constructor-based destination                          |
| `TypeConverters.cs`                  | `ITypeConverter<,>`, `ConvertUsing<TConverter>`                           |
| `ConditionalMapping.cs`              | `Condition(...)` on members                                               |
| `FillingExistingDestination.cs`      | `Map(source, destination)` overload                                       |
| `General.cs`                         | Basic property-by-name convention                                         |

8 files. If all pass against Morph, the v0.1 drop-in claim holds on the feature surface the design doc commits to.

## What the harness does to each file

1. Replaces `using AutoMapper;` with `using Morph;`
2. Replaces `using AutoMapper.Configuration;` etc. with commented-out lines if that namespace has no Morph equivalent
3. Leaves Shouldly assertions as-is (harness project includes Shouldly as a NuGet reference)
4. Leaves `[Fact]` and xunit-native surfaces alone
5. If a test file uses `AutoMapperSpecBase`, the harness pulls that fixture into the temp project too, with `using AutoMapper;` → `using Morph;`

## Expected deviations

Some imported tests may fail on purpose — where AutoMapper and Morph deliberately diverge (see `../docs/api-surface.md` §"Breaking differences"). These are tracked in `expected-deviations.md` as a running list so regressions don't hide behind them.
