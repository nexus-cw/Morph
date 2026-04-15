# Morph Validation

External validation that Morph is a drop-in replacement for AutoMapper v14 on its in-scope feature surface.

## Harness

- **`compat/`** — dual-compilation drop-in harness. One shared consumer codebase compiled twice (Morph leg + AutoMapper 14.0.0 leg) and run against the same tests. Green on both legs means a real consumer can swap `using AutoMapper;` → `using Morph;` and get identical behavior on the covered scenarios. See `compat/README.md`.

That's the validation strategy. An earlier approach — cloning AutoMapper's own test suite and sed-rewriting it to target Morph — was removed: AutoMapper's tests reach into internal types (`IGlobalConfiguration`, `TypeMap`, `ProjectTo`) Morph v0.1 intentionally doesn't expose, so the harness fails at compile on those internals and says nothing about consumer-level drop-in compat. The compat harness flips that: the code under test is what a consumer writes, not what AutoMapper's maintainers test internally.

## Scope

What the compat harness proves — and, equally important, what it doesn't — is documented in `compat/README.md`. For the authoritative feature matrix (v0.1 in-scope vs deferred), see `../docs/api-surface.md`.
