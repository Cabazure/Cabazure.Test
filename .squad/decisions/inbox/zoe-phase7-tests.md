# Decision Note: Phase 7 Tests Completed — Zoe

**Author:** Zoe (QA Lead)
**Date:** 2026-03-07
**Related task:** Phase 7 — TestAssemblyInitializer + `SutFixtureCustomizations` / `CustomizeWithAttribute` test gaps

---

## Decision 1: Add `InternalsVisibleTo` to unlock direct testing

**Context:** `SutFixtureCustomizations.All` and `CustomizeWithAttribute.Instantiate()` are both `internal`. Prior tests worked around this via integration (attribute behaviour) and reflection. That's fragile — an indirect path can pass vacuously if the internal implementation changes.

**Decision:** Created `src/Cabazure.Test/AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("Cabazure.Test.Tests")]`. This is standard practice for library test projects and improves test precision.

**Impact:** Tests can now directly assert `SutFixtureCustomizations.All` contents and call `attr.Instantiate()` without a reflection trampoline.

---

## Decision 2: `ProjectWideValue` must be `public`

**Context:** `ProjectWideValue` is defined in `TestAssemblyInitializer.cs` (test project). It is used as a `[Theory]` parameter in `SutFixtureCustomizationsTests.ProjectWideCustomization_IsApplied_WhenAutoNSubstituteDataUsed`. xUnit requires theory parameters to have consistent visibility — if the method is `public`, the parameter type must also be `public`.

**Decision:** `ProjectWideValue` is declared `public record`. It lives in the test assembly, so there's no leakage concern.

---

## Decision 3: `CountTestCustomization` is private and anonymous

**Context:** `Add_MultipleCustomizations_AllCountGrowsByExactAmount` needs to add two customizations. If it registered a named public type, another test could observe that type in `All` and make incorrect inferences.

**Decision:** `CountTestCustomization` is a `private sealed class` nested inside `SutFixtureCustomizationsTests`. No other code can reference it; its presence in `All` is invisible to other assertions.

---

## Observation: Static registry cannot be cleared

`SutFixtureCustomizations` is append-only. The `Add_MultipleCustomizations_AllCountGrowsByExactAmount` test uses a "count before → add 2 → assert count + 2" pattern rather than asserting an absolute value. This keeps the test correct regardless of what other tests (or `[ModuleInitializer]`) have added before it runs.

**Recommendation (future):** Document in public API that callers must use assembly-private types as customization targets to prevent cross-test contamination.
