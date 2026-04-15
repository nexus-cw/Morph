# Morph

A lightweight object-to-object mapper for .NET. API-compatible with the core of AutoMapper.

## Why

AutoMapper moved to a commercial license in 2024. Morph is an MIT-licensed replacement that mirrors the API surface most consumers actually use, so existing codebases can swap it in with minimal changes.

## Scope

Morph aims for drop-in compatibility with the **common AutoMapper working set**, not full parity. If you use profiles, `CreateMap`, `ForMember`, `Ignore`, custom resolvers, reverse maps, and convention-based property mapping, Morph will work for you. If you depend on deep feature areas like `ProjectTo`/EF integration, inheritance maps, or value transformers, those are on the roadmap but not in v0.1.

See `docs/api-surface.md` for the authoritative feature matrix.

## Status

**v0.1 — in development.** No NuGet package yet.

## Target Frameworks

- `netstandard2.0` — maximum consumer reach (.NET Framework 4.7.2+, .NET Core 2.0+, Xamarin, Unity)
- `net10.0` — latest .NET, modern language features

## License

MIT. See `LICENSE`.
