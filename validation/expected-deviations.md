# Expected Deviations from AutoMapper v14 Behavior

Tests in the curated subset (see `subset.md`) that are expected to fail or behave differently against Morph because of **deliberate design choices**, not bugs.

When a test here starts passing, remove it from this file. When a test fails for a *new* reason, investigate — it's likely a real regression.

## Active deviations

*(none yet — populated as the harness runs and deviations are characterized)*

## Reference

See `../docs/api-surface.md` §"Breaking differences from AutoMapper" for the authoritative list of design-level divergences:
1. No `AddMaps(assembly)` scanning for standalone `CreateMap` calls
2. `Map<T>` throws on missing maps where AutoMapper v14 has silent modes in some configurations
3. `AssertConfigurationIsValid()` is always opinionated; no off-switch
