# Decision: Phase 8 — FixtureFactory API Design

**Proposed by:** Kaylee (Core .NET Developer)  
**Date:** 2026-03-07  
**Status:** Proposed

---

## Context

`SutFixture` was a thin wrapper around AutoFixture's `IFixture` that added minimal value; its only
purpose was to give `AutoNSubstituteDataHelper` a concrete type on which to call generic methods
(`Create<T>`, `Freeze<T>`). This forced the helper to use `MakeGenericMethod` reflection at runtime
solely to bridge compile-time generics from a runtime `Type`.

AutoFixture's kernel APIs make reflection unnecessary:
- `new SpecimenContext(fixture).Resolve(type)` — non-generic specimen resolution (no reflection).
- `SpecimenBuilderNodeFactory.CreateTypedNode(type, new FixedBuilder(value))` — non-generic freeze
  (same mechanism `FreezingCustomization` uses internally); insert at index 0 in
  `fixture.Customizations`.

---

## Decision

### 8a. Introduce `FixtureFactory` as the public entry point

**Decision:** Replace `SutFixture` with a `public static class FixtureFactory` in the root
`Cabazure.Test` namespace.

**Rationale:**
- `IFixture` is a well-known, fully-featured API; wrapping it in `SutFixture` provided no
  additional functionality.
- Returning `IFixture` directly gives consumers the full AutoFixture surface without a proxy
  layer.
- A static factory is the idiomatic pattern for returning configured instances without exposing
  construction complexity.

### 8b. Three overloads — two public, one internal

| Overload | Access | Purpose |
|---|---|---|
| `Create()` | public | No-arg convenience; delegates to `Create([])`. |
| `Create(params ICustomization[])` | public | Applies `AutoNSubstituteCustomization` first, then each supplied customization. |
| `Create(MethodInfo)` | internal | Full priority stack for theory data attributes. |

**Rationale:**
- The `MethodInfo` overload is an implementation detail of the attribute pipeline; making it
  public would expose an internal contract to consumers.
- The two public overloads are sufficient for direct `[Fact]` usage.

### 8c. Eliminate reflection from `AutoNSubstituteDataHelper`

**Decision:** Use AutoFixture kernel APIs directly.

- `CreateValue` → `new SpecimenContext(fixture).Resolve(type)`
- `FreezeValue` → `fixture.Customizations.Insert(0, SpecimenBuilderNodeFactory.CreateTypedNode(type, new FixedBuilder(value)))`

**Rationale:**
- `MakeGenericMethod` was the only reason `SutFixture` existed as a concrete type in the helper.
- Kernel APIs are the canonical, non-reflective way to perform these operations; they are already
  what AutoFixture's own extension methods delegate to.
- Removes a runtime failure mode (reflection errors only surfaced at test execution time).

### 8d. Delete `SutFixture` and the `Fixture/` subdirectory

**Decision:** `SutFixture.cs` is deleted; `AssemblyInitializer.cs` is moved to the project root
(`Cabazure.Test` namespace) and the `Fixture/` directory is removed entirely.

**Rationale:**
- The `Fixture/` directory only existed to house `SutFixture`; `AssemblyInitializer` was placed
  there historically but belongs at the project root since it is a library-level concern.
- Removing the directory reduces navigational friction and eliminates a namespace that served no
  grouping purpose.

---

## Consequences

- **Breaking change for consumers:** Any code that directly instantiates `SutFixture` must be
  updated to use `FixtureFactory.Create()` or `FixtureFactory.Create(customizations)` instead.
  The returned `IFixture` provides a superset of the functionality `SutFixture` exposed.
- `AutoNSubstituteDataAttribute` and its three variants are unchanged from a user perspective;
  the fixture they inject into theory parameters is still configured identically.
- `SutFixtureCustomizations.Add` and `[CustomizeWith]` continue to work without modification.
