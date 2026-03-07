# Decision Note: Phase 7 Test Design — Zoe

**Author:** Zoe (QA Lead)
**Date:** 2026-03-07
**Related task:** Phase 7 — User-Defined Fixture Customizations

---

## Observation 1: `SutFixtureCustomizations.All` is `internal`

`SutFixtureCustomizations.All` is marked `internal`, so tests cannot directly assert its
contents without `InternalsVisibleTo`. This is fine — the property is an implementation
detail and is fully observable through the data attributes' behaviour. No change needed,
but Kaylee should be aware that direct unit tests of `All` are not possible from the test
project unless an `[InternalsVisibleTo]` attribute is added to `Cabazure.Test.csproj`.

**Recommendation:** Leave `All` as `internal`. Observable via attribute integration tests.

---

## Observation 2: Static registry — ordering risk

`SutFixtureCustomizations` is append-only and never cleared. If multiple test assemblies
(or multiple test classes) register conflicting customizations for the same concrete type,
the last registration wins because `IFixture.Inject` overwrites the previous value.

Current mitigation in tests: use a uniquely-scoped nested record `CustomizedDomainValue`
inside `SutFixtureCustomizationsTests` so no other class would ever request that type.

**Recommendation (future):** Consider documenting in the XML doc that callers must scope
their registrations to types private to their assembly/test class to avoid cross-test
contamination. Alternatively, a `Clear()` method (test-only, perhaps `[EditorBrowsable(Never)]`)
could support fixture reset in integration test scenarios.

---

## Observation 3: `CustomizeWithAttribute.Instantiate()` is `internal`

Testing the invalid-type and missing-constructor guard paths requires calling `Instantiate()`
indirectly through `GetData`. The current approach works (reflective MethodInfo with a
decorated private static helper), but it's fragile — if `GetData`'s internal call site
changes, the test still passes vacuously.

**Recommendation:** Add `[assembly: InternalsVisibleTo("Cabazure.Test.Tests")]` to
`Cabazure.Test.csproj` so `Instantiate()` can be tested directly. This is standard
practice for library test projects and improves test precision.

---

## No blockers

All test files compile cleanly. Ready for coordinator to run after Kaylee confirms
implementation is complete.
