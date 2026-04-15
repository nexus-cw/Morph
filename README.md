# Morph

A lightweight, MIT-licensed object-to-object mapper for .NET. API-compatible with the core of [AutoMapper v14](https://github.com/AutoMapper/AutoMapper/tree/v14.0.0).

## Why

AutoMapper moved to a commercial license in 2024. Morph mirrors the API surface most consumers actually use, so existing codebases can swap it in with minimal changes — in many cases, a single `using` statement change.

## Install

```
dotnet add package Morph
```

Target frameworks: `netstandard2.0` (broad compatibility — .NET Framework 4.7.2+, .NET Core 2.0+, Xamarin, Unity) and `net10.0`.

## Quick start

```csharp
using Morph;

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Source, Destination>()
       .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
});

var mapper = config.CreateMapper();
var dest = mapper.Map<Destination>(source);
```

## What's in scope

- `Profile` + `CreateMap`
- `ForMember` + `MapFrom` (lambda, expression, member access)
- `ReverseMap`
- `ConstructUsing`
- `ConvertUsing<T>` / `ITypeConverter<,>`
- `Map(source, existingDestination)` overload
- Multi-profile registration
- Property-by-name convention mapping
- Primitive coercion (`decimal`, `DateTime`, etc.)
- Collections (List, empty, nested)
- Runtime-type dispatch
- `AssertConfigurationIsValid`

See [`docs/api-surface.md`](https://github.com/nexus-cw/Morph/blob/main/docs/api-surface.md) for the authoritative feature matrix.

## What's not in this release

- `ProjectTo` / EF integration
- Inheritance maps
- Flattening
- Open generics
- Custom naming conventions
- `ForCtorParam`

These are on the roadmap. If your code relies on them, Morph v0.1 is not yet a drop-in.

## Security

Morph is hardened against [CVE-2026-32933](https://github.com/AutoMapper/AutoMapper/security/advisories/GHSA-rvv3-g6hj-g44x) (uncontrolled recursion on self-referential graphs causing `StackOverflowException`). Morph enforces a configurable `MaxDepth` (default 32) and throws a catchable `AutoMapperMappingException` instead.

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.MaxDepth = 64; // default 32
    cfg.CreateMap<Node, NodeDto>();
});
```

## Compatibility verification

Morph ships with a dual-compilation compat harness under `validation/compat/` that runs the same consumer code twice — once against Morph, once against AutoMapper 14.0.0 from NuGet. Both legs currently green on 21 scenarios covering the features listed above. See [`validation/compat/README.md`](https://github.com/nexus-cw/Morph/blob/main/validation/compat/README.md).

## Status

**v0.1.0-alpha** — alpha release. Core API-compatible subset of AutoMapper v14. Expect bugs; file issues at [github.com/nexus-cw/Morph/issues](https://github.com/nexus-cw/Morph/issues).

## License

MIT. See [`LICENSE`](https://github.com/nexus-cw/Morph/blob/main/LICENSE).
