# Morph — API Surface (v0.1)

This document defines the public API surface Morph v0.1 ships with, and maps each surface element to its AutoMapper equivalent. Anything not listed here is **out of scope for v0.1** and will be addressed in later versions.

## Design principle

> If a typical AutoMapper consumer can swap `using AutoMapper;` for `using Morph;` and their code compiles + runs + behaves identically, Morph has done its job.

"Typical" here means the 80% case — profiles, `CreateMap`, `ForMember`, `Ignore`, custom resolvers, reverse maps, convention-based member mapping. The long tail (`ProjectTo`, inheritance, value transformers, null substitution, validation) is v0.2+.

## Namespace

All v0.1 API ships under `Morph`. Drop-in migration path: add `using Morph;` and delete `using AutoMapper;`. For consumers that can't touch `using` statements, a `using AutoMapper = Morph;` alias in a shared file works.

---

## v0.1 in scope

### Core types

| Morph type               | AutoMapper equivalent             | Notes                                              |
|--------------------------|-----------------------------------|----------------------------------------------------|
| `IMapper`                | `IMapper`                         | Core mapping entry point                           |
| `MapperConfiguration`    | `MapperConfiguration`             | Config builder; creates an `IMapper`               |
| `IMapperConfigurationExpression` | `IMapperConfigurationExpression` | Fluent config surface passed to `MapperConfiguration` ctor |
| `Profile`                | `Profile`                         | Logical grouping of map definitions                |
| `IMappingExpression<TSrc,TDest>` | `IMappingExpression<TSrc,TDest>` | Per-map fluent config                              |
| `IMemberConfigurationExpression<TSrc,TDest,TMember>` | same | Per-member fluent config (returned by `ForMember`) |
| `ResolutionContext`      | `ResolutionContext`               | Per-resolve state (items, parent context)          |
| `IValueResolver<TSrc,TDest,TMember>` | same                     | Custom resolver interface                          |
| `ITypeConverter<TSrc,TDest>` | same                          | Full-type custom conversion                        |
| `AutoMapperMappingException` | `AutoMapperMappingException`  | Yes, we reuse the name — it's in consumer catch blocks |

### `IMapper` methods

```csharp
TDest Map<TDest>(object source);
TDest Map<TSrc, TDest>(TSrc source);
TDest Map<TSrc, TDest>(TSrc source, TDest destination);
object Map(object source, Type srcType, Type destType);
IConfigurationProvider ConfigurationProvider { get; }
```

### `IMappingExpression<TSrc,TDest>` methods (v0.1 subset)

```csharp
IMappingExpression<TSrc, TDest> ForMember<TMember>(
    Expression<Func<TDest, TMember>> destMember,
    Action<IMemberConfigurationExpression<TSrc, TDest, TMember>> memberOpts);

IMappingExpression<TSrc, TDest> ForMember(
    string destMemberName,
    Action<IMemberConfigurationExpression<TSrc, TDest, object>> memberOpts);

IMappingExpression<TDest, TSrc> ReverseMap();

IMappingExpression<TSrc, TDest> ConvertUsing<TConverter>() where TConverter : ITypeConverter<TSrc, TDest>;
IMappingExpression<TSrc, TDest> ConvertUsing(Func<TSrc, TDest> converter);
IMappingExpression<TSrc, TDest> ConvertUsing(Func<TSrc, TDest, TDest> converter);

IMappingExpression<TSrc, TDest> ConstructUsing(Func<TSrc, ResolutionContext, TDest> ctor);
IMappingExpression<TSrc, TDest> ConstructUsing(Func<TSrc, TDest> ctor);
```

### `IMemberConfigurationExpression` methods (v0.1 subset)

```csharp
void MapFrom<TSource>(Expression<Func<TSrc, TSource>> sourceMember);
void MapFrom(Func<TSrc, TDest, TMember> resolver);
void MapFrom<TResolver>() where TResolver : IValueResolver<TSrc, TDest, TMember>;
void Ignore();
void UseValue(TMember value);
void Condition(Func<TSrc, bool> condition);
```

### Convention-based mapping (default behavior)

- Match public instance properties by exact name (case-sensitive)
- Match public fields by exact name (after properties)
- If destination member type ≠ source member type, attempt a nested `Map<TSrcMember, TDestMember>` if a map exists; else throw
- Primitive type conversions: use `Convert.ChangeType` for the supported set (numeric widening, string↔primitive, DateTime parsing where unambiguous)
- Collections: `IEnumerable<TSrc>` → `List<TDest>` / `TDest[]` / `ICollection<TDest>` / `IEnumerable<TDest>` all supported; element-wise mapping via the declared map

### Profiles

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<Source, Dest>();
        CreateMap<Source, Dest>().ReverseMap();
    }
}

var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
// or
var config = new MapperConfiguration(cfg => cfg.AddProfile(new MyProfile()));
// or
var config = new MapperConfiguration(cfg => cfg.AddProfiles(typeof(MyProfile).Assembly));
```

### Configuration validation

```csharp
config.AssertConfigurationIsValid();
```

Throws if any declared map has an unmapped destination member (and that member isn't `Ignore()`d).

---

## v0.1 out of scope (deferred)

These are real AutoMapper features that Morph will NOT ship in v0.1. Listed explicitly so consumers can check before migrating.

### Deferred to v0.2

- `IQueryable.ProjectTo<TDest>()` — EF/LINQ projection. Large implementation (expression-tree rewriting), unlocks EF integration specifically. High value for consumers that use it; zero value for consumers that don't.
- Open generics: `CreateMap(typeof(Source<>), typeof(Dest<>))`
- `Include<TOther>()` / inheritance maps
- `ForAllMembers(...)` / `ForAllOtherMembers(...)`
- `BeforeMap` / `AfterMap` hooks

### Deferred to v0.3+

- Value transformers (`AddValueTransformer`)
- Null substitution (`NullSubstitute(...)`)
- Flattening conventions (e.g. `Customer.Address.City` → `CustomerAddressCity`)
- `PreserveReferences()` / circular reference handling
- `PreCondition(...)`
- Custom naming conventions (e.g. `SourceMemberNamingConvention`)
- `IMappingExpression.ForCtorParam(...)` (constructor param mapping beyond what `ConstructUsing` covers)
- `ForPath(...)` (nested path mapping)
- Dynamic/ExpandoObject source support
- DataReader → POCO mapping (AutoMapper.Data)

### Explicitly NOT planned

- `Mapper.Initialize` / static `Mapper` singleton — AutoMapper itself deprecated this in 9.0. Morph ships instance-only from day one.

---

## Breaking differences from AutoMapper

Where Morph *deliberately* diverges. Called out so migrators aren't surprised.

1. **No `AddMaps(assembly)` scanning for standalone `CreateMap` calls** — only profiles are scanned. Rationale: implicit map discovery makes debugging configuration painful. `AddProfiles(assembly)` scans for `Profile`-derived types only.

2. **No `MissingTypeMapException` silent handling in `Map<T>`** — if a map isn't configured and types don't trivially match, we throw. AutoMapper has had several quiet modes over its history; we pick one (strict) and stay there.

3. **`AssertConfigurationIsValid()` is opinionated** — it treats any public settable destination property that has no matching source, no `MapFrom`, and no `Ignore()` as an error. AutoMapper's default is the same, but Morph doesn't offer an off-switch.

4. **Default `MaxDepth` of 32 on nested maps** — prevents stack exhaustion from self-referential or adversarial object graphs. This is the mitigation for the CVE-2026-32933 pattern (AutoMapper ≤15.1.0 / ≤16.1.0 had no default cap and was vulnerable to DoS via `StackOverflowException`, which .NET cannot catch). Morph throws `AutoMapperMappingException` on overflow, which *is* catchable. Configurable via `MapperConfiguration.MaxDepth = N`.

---

## Public surface file layout

```
src/Morph/
├── IMapper.cs
├── Mapper.cs                         // internal impl of IMapper
├── MapperConfiguration.cs
├── IMapperConfigurationExpression.cs
├── MapperConfigurationExpression.cs  // internal
├── Profile.cs
├── IConfigurationProvider.cs
├── ITypeConverter.cs
├── IValueResolver.cs
├── ResolutionContext.cs
├── AutoMapperMappingException.cs     // name retained for consumer catch-block compat
├── Expressions/
│   ├── IMappingExpression.cs
│   ├── MappingExpression.cs          // internal
│   ├── IMemberConfigurationExpression.cs
│   └── MemberConfigurationExpression.cs
├── Execution/
│   ├── TypeMap.cs                    // internal — resolved map plan per (TSrc,TDest)
│   ├── MapPlanBuilder.cs             // internal — turns fluent config into executable plan
│   ├── PropertyMapper.cs             // internal — per-member executor
│   └── CollectionMapper.cs           // internal — IEnumerable<T> → target collection
└── Conventions/
    └── NameMatcher.cs                // internal — default exact-name convention
```

---

## Test plan

`tests/Morph.Tests/` — xUnit.

v0.1 ship criteria — all tests in these groups green:

1. **Basic — `BasicMapping.cs`:** primitive property mapping, nested object mapping, collection mapping
2. **Profiles — `ProfileTests.cs`:** `AddProfile<T>`, `AddProfile(instance)`, `AddProfiles(assembly)`
3. **ForMember — `ForMemberTests.cs`:** `MapFrom(expression)`, `MapFrom(func)`, `MapFrom<TResolver>()`, `Ignore()`, `UseValue(...)`, `Condition(...)`
4. **ReverseMap — `ReverseMapTests.cs`:** symmetric round-trip on mapped types
5. **ConvertUsing — `ConvertUsingTests.cs`:** `ITypeConverter`-based, func-based (both overloads)
6. **ConstructUsing — `ConstructUsingTests.cs`:** custom constructor with + without `ResolutionContext`
7. **Validation — `ValidationTests.cs`:** `AssertConfigurationIsValid()` catches unmapped members; passes when members are `Ignore()`d
8. **Collections — `CollectionTests.cs`:** source `IEnumerable<T>` → dest `List<T>` / `T[]` / `ICollection<T>` / `IEnumerable<T>`
9. **Drop-in — `AutoMapperCompatTests.cs`:** port 10-15 representative AutoMapper sample tests with `using Morph;` substituted for `using AutoMapper;` — they must pass verbatim.

Drop-in compat group is the acceptance gate. If those pass, Morph has hit v0.1.

---

## Open questions

1. **Consumer target for validation.** Operator has real AutoMapper consumers at work. Which one do we point Morph at for the drop-in test? Knowing which features the consumer actually uses would let me prioritize correctly.
2. **Package distribution.** NuGet.org publishing is a separate decision from "build works." Shipping to nuget.org vs. a private feed first?
3. **Source-generator path.** AutoMapper 13.x uses expression-tree-based mapping (runtime compile). Morph v0.1 matches that. A source-generator approach (compile-time map generation) would be faster at runtime but is a v1.x decision. Flagged here so we don't back ourselves into an expression-tree corner.
