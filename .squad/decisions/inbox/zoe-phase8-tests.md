# Zoe Phase 8 Test Migration — Edge Cases & Decisions

**Date:** 2026-03-07  
**Author:** Zoe (QA Lead)

## Notable Decision: Unused fixture removed from Substitute tests

In the old `SutFixtureTests`, two tests called `fixture.Substitute<T>()` on a `SutFixture` instance:
- `Substitute_ReturnsNSubstituteProxy`
- `Substitute_ReturnsDifferentInstances_OnMultipleCalls`

After migration, both tests call `NSubstitute.Substitute.For<T>()` directly — the fixture object was only there to call `fixture.Substitute<T>()`. I removed the now-unused `var fixture = FixtureFactory.Create()` line from these two tests to keep them clean and self-evident.

**Rationale:** An unused variable would be a compiler warning and mislead readers into thinking the fixture contributes to the assertion.

## Observation: SutFixtureCustomizations naming unchanged

`SutFixtureCustomizations` (the global registry class) still uses the "SutFixture" prefix. If Kaylee renames this class as part of a future phase, `SutFixtureCustomizationsTests.cs` will need corresponding updates. No action needed now — the class name is not in scope for Phase 8.

## Observation: `using Cabazure.Test.Tests.Fixture` still needed

All four attribute test files keep `using Cabazure.Test.Tests.Fixture;` because they reference `FixtureFactoryTests.IMyInterface` etc. The namespace is unchanged — only the class name changed from `SutFixtureTests` to `FixtureFactoryTests`.
