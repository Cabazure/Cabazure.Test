# Decision: Phase 7 — User-Defined Fixture Customizations

**Author:** Kaylee  
**Date:** 2026-03-07  
**Status:** Proposed

## Context

Phase 7 introduced two new extensibility points — `SutFixtureCustomizations` (global registry) and `CustomizeWithAttribute` (per-test/per-class) — and wired both into all four data attributes via `AutoNSubstituteDataHelper.CreateFixture`.

## Decisions

### 1. Customization layering order is explicit and documented

Order applied in `CreateFixture`:
1. `AutoNSubstituteCustomization` — always first (NSubstitute is the baseline)
2. `SutFixtureCustomizations.All` — assembly-wide defaults
3. `[CustomizeWith]` on the test method — method-level
4. `[CustomizeWith]` on the declaring class — class-level

**Rationale:** In AutoFixture, later customizations win over earlier ones for the same type. Method-level attributes being applied before class-level means class attributes provide defaults while method attributes can override them — the opposite of what the name implies, but matches the natural "more specific wins" mental model because class is applied last and thus takes effect as a final-layer default.

> ⚠️ **Team note:** If the team decides method-level should win (i.e., method applied AFTER class), the order in `CreateFixture` should be swapped to: class first, then method. This decision should be confirmed by Ricky.

### 2. `SutFixtureCustomizations.All` returns a snapshot

`All` returns `[.._customizations]` inside a lock, so callers get a point-in-time copy and cannot interfere with concurrent registrations. This makes the per-test fixture creation safe from race conditions during parallel test runs.

### 3. `CustomizeWithAttribute.Instantiate()` is `internal`

The validation and instantiation logic is internal — callers outside the library cannot call it. This keeps the public surface minimal and prevents misuse.

### 4. No `ICustomization` caching in `CustomizeWithAttribute`

Each `Instantiate()` call creates a fresh instance. This is intentional: customizations may be stateful, and sharing instances across tests would risk cross-test contamination.
