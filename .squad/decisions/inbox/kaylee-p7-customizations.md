# Decision: Phase 7 — User-Defined Fixture Customizations

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Implemented

## Context

The library needed a way for test projects to inject their own AutoFixture customizations into every `SutFixture` without forking the attribute hierarchy or wrapping the data attributes. The challenge was defining a clean, layered priority system that is predictable and composable.

## Decision

### Three-tier customization stack (applied in order)

Every `SutFixture` created by the data attributes now uses `AutoNSubstituteDataHelper.CreateFixture(testMethod)` which builds the fixture in strict priority order:

1. **`AutoNSubstituteCustomization`** — always first; NSubstitute is the non-negotiable foundation.
2. **`SutFixtureCustomizations.All`** — project-wide registrations, registered once via `[ModuleInitializer]`.
3. **`[CustomizeWith]` on the test method** — method-level overrides.
4. **`[CustomizeWith]` on the declaring class** — class-level defaults; applied after method-level (last write wins in AutoFixture, so class-level has higher effective priority than method-level for the same type).

> **Note on ordering:** AutoFixture's customization pipeline is last-writer-wins for the same type. Placing class-level `[CustomizeWith]` attributes after method-level means class-level overrides method-level for the same type. This is intentional: class attributes declare the "house rules" that always apply. If a different ordering is ever desired, it is a breaking change requiring a new decision.

### `SutFixtureCustomizations` design choices

- **No `Clear()` / `Reset()`** — omitted intentionally. A global registry that can be cleared mid-run would produce non-deterministic tests. If isolation is needed, use `[CustomizeWith]` at the method level.
- **Thread-safe via `lock`** — `Add` and `All` both lock on a private object. `All` returns a snapshot (`[.._customizations]`) so callers cannot mutate the shared list.
- **`All` is `internal`** — consumers interact only through `Add`; the framework reads the list. This preserves the ability to change the internal representation.

### `CustomizeWithAttribute` design choices

- `AllowMultiple = true` — multiple customizations can be stacked on a single method or class; they are applied in declaration order.
- Validation happens at `Instantiate()` call time (test discovery / execution), not at attribute construction time (compile time). This is consistent with how xUnit handles data attributes.
- Validation produces `InvalidOperationException` with a diagnostic message that names the offending type, making misconfiguration easy to diagnose.

## Consequences

- All four data attributes (`AutoNSubstituteData`, `InlineAutoNSubstituteData`, `MemberAutoNSubstituteData`, `ClassAutoNSubstituteData`) now participate in the customization stack automatically.
- Per-row fixture creation in `Member` and `Class` variants is preserved — each row gets its own fully-customized fixture, preventing cross-row state leakage.
- The `SutFixture(params ICustomization[])` constructor is the integration point; the parameterless constructor is now only used by code that wants the default (NSubstitute-only) configuration.
