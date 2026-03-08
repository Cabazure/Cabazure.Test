# Decision: Phase 38 Caching Safety Review — Approved

**Author:** Mal (Lead & Architect)  
**Date:** 2026-07-14  
**Branch:** squad/38-perf-optimizations

## Context

Code review of reflection caching introduced in Phase 38 perf optimizations. Three files add static caching for reflection metadata to avoid redundant reflection/allocation per test invocation.

## Review Summary

All five caches were analyzed for state leakage risks:

| Cache | Location | Safe? | Reasoning |
|-------|----------|-------|-----------|
| `InitializedTypes` | FixtureFactory | ✅ | Tracks constructor initialization facts only |
| `MethodCustomizations` | FixtureFactory | ✅ | `ICustomization.Customize()` is idempotent on `this` |
| `TypeCustomizations` | FixtureFactory | ✅ | Same as above |
| `ParameterCache` | FixtureDataExtensions | ✅ | Matchers are pure predicates; attributes create new customizations on each call |
| `_snapshot` | FixtureCustomizationCollection | ✅ | Volatile field with lock-protected invalidation |

## Key Invariants Verified

1. **`ICustomization.Customize(IFixture)` never mutates `this`** — this is the AutoFixture contract. All implementations in this library were verified to only mutate the fixture argument.

2. **`IRequestSpecification.IsSatisfiedBy(object)` is pure** — no side effects, no mutable state. AutoFixture's matchers (EqualRequestSpecification, type-based matchers) are stateless predicates.

3. **Thread safety under xUnit 3 parallel execution** — all caches use `ConcurrentDictionary` or explicit locking. Cached values are immutable or effectively immutable (arrays of stateless objects).

4. **Per-test isolation maintained** — each test gets `new Fixture()`. Cached customizations are *applied* to each fixture, not shared between fixtures.

## Decision

**APPROVED** — no changes required before merge.

## Notes for Future Work

- If a new `ICustomization` implementation is added that stores mutable state on `this`, it would break the caching invariant. This is unlikely given AutoFixture's design philosophy, but worth noting in code comments if the library grows significantly.
- The `CustomizeAttribute.GetCustomization(parameter)` pattern (creates new `ICustomization` each call) vs `CustomizeWithAttribute.Instantiate()` pattern (caches the instance) are both valid because both resulting `ICustomization` implementations are stateless.
