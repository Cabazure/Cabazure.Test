# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the test project `Cabazure.Test.Tests`. The unique challenge: we're testing a testing library, and our tests must use that library themselves (dogfooding). Edge cases to watch: sealed classes, value types, types without parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

### Completed Test Coverage (2026-03-07, Phases 1-12)

**Test Files Completed:**
- `FixtureFactoryTests.cs` (15 tests) — migrated from SutFixtureTests, covers Create, Create(customizations), inheritance/interface/abstract type handling
- `AutoNSubstituteDataAttributeTests.cs` — four data attribute variants all verified
- `SutFixtureCustomizationsTests.cs` (8 tests) — global registry Add/All behavior, project-wide customization integration
- `CustomizeWithAttributeTests.cs` (8 tests) — method-level, class-level, multi-stacked attribute handling
- `RecursionCustomizationTests.cs` (5 tests) — null guard, behavior replacement (ThrowingRecursionBehavior → OmitOnRecursionBehavior)
- `ImmutableCollectionCustomizationTests.cs` (10 tests) — all 8 collection types plus property population via PropertyInfo
- `JsonElementCustomizationTests.cs` (6 tests) — ValueKind verification, property enumeration, clone/GC safety
- `DateOnlyTimeOnlyCustomizationTests.cs` (7 tests) — valid (non-default) date/time generation, property population
- `TypeCustomizationTests.cs` (15 tests) — generic factory pattern, wrapping (sealed class), overload testing, integration with Build<T>
- `FixtureCustomizationCollectionTests.cs` (updated) — null guards, overload dispatch for Add<T>(factory) and Add(builder), snapshot enumeration
- `SpecimenRequestHelperTests.cs` (5 tests) — pattern-matching all 5 branches (ParameterInfo, PropertyInfo, FieldInfo, Type, unknown)
- `CancellationTokenCustomizationTests.cs` (5 tests) — non-cancelled default, CanBeCanceled=false, equal tokens, opt-out via Remove<T>, integration with [AutoNSubstituteData]
- `AutoNSubstituteDataHelperFixtureInjectionTests.cs` (6 tests) — IFixture injection, same-instance, concrete Fixture, InlineData, [Frozen] edge case

**Test Results:**
- Phase 10 end: **111 tests passing**
- Phase 11 (CancellationToken): **116 tests passing**
- Phase 12 (Fixture injection): **122 tests passing**

**Key Testing Patterns Established:**
- **Namespace collision workaround:** Use `new AutoFixture.Fixture()` when ambient `Fixture` namespace exists
- **Sealed class testing:** Composition/wrapping pattern via `ICustomization` (cannot subclass)
- **Static collection mutations:** Protect with try/finally to restore state (prevent cross-test pollution)
- **FluentAssertions limits:** DateOnly/TimeOnly lack comparison operators; use `.Year` property or `.Not.Be(MinValue)` patterns
- **Dogfooding theory tests:** Use `[Theory, AutoNSubstituteData]` where applicable for library-under-test validation
- **Cloning requirement:** JsonElement must use `.Clone()` to survive beyond JsonDocument GC
- **Same-instance assertion:** Use `[Frozen]` + `fixture.Create<T>()` to prove injected fixture is the shared instance

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### Key Patterns (Phases 1-11)

- **CancellationToken tests:** Must restore `FixtureFactory.Customizations` via try/finally when testing opt-out (Remove<T>()); static state must not pollute other tests
- **Overload ambiguity:** `FixtureCustomizationCollection.Add(null!)` is ambiguous with 3 overloads; cast explicitly: `(ICustomization)null!`
- **CS0649 on reflection-only fields:** Harmless in test files; suppress with `#pragma` or `[SuppressMessage]` if needed
- **SpecimenRequestHelper pattern:** Static pure-method tests use plain `[Fact]`; no AutoFixture/NSubstitute needed

### Phase 12: Fixture Injection Tests (2026-03-07T17:33:43Z)

**Task:** Write tests for `AutoNSubstituteDataHelper` fixture instance injection.

**Test File Created:** `tests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataHelperFixtureInjectionTests.cs` (6 tests)

**Coverage:**
1. `Theory_IFixtureParameter_IsNotNull` — resolved value is not null
2. `Theory_IFixtureParameter_IsFixtureInstance` — resolved value is a concrete `AutoFixture.Fixture`
3. `Theory_IFixtureParameter_IsSameInstanceResolvingOtherParams` — injected fixture is same instance that resolved `[Frozen]` params
4. `Theory_ConcreteFixtureParameter_IsInjected` — concrete `Fixture` type also works
5. `InlineData_WithIFixtureParameter_InjectsFixture` — IFixture fills auto slot after explicit inline value
6. `Theory_FrozenIFixtureParameter_IsInjectedNormally` — `[Frozen]` on IFixture doesn't throw

**Test Results:** 122/122 passing (6 new + 116 existing)

**Key Pattern:** Test 3 is highest-value: proves injected fixture is the shared instance, not a new one. Uses `FixtureFactoryTests.IMyInterface` as the frozen type (no new infrastructure needed).

### Phase 13: Substitute Attribute Refactor Tests (2026-03-07T18:44:29Z)

**Task:** Verify Substitute attribute behavior after Kaylee's ParameterInfo refactor.

**Test File Updated:** `tests/Cabazure.Test.Tests/Attributes/SubstituteAttributeTests.cs`

**Changes:**
- Updated to use canonical `using AutoFixture.AutoNSubstitute` (no custom SubstituteAttribute import)
- Tests now validate that AutoFixture.AutoNSubstitute.SubstituteAttribute fires naturally via ParameterInfo resolution

**Coverage:**
1. `Theory_SubstituteAttributeNull_CreatesParameterDefault` — null parameter passes through
2. `Theory_SubstituteAttributeInterface_CreatesSubstitute` — interface type substituted
3. `Theory_SubstituteAttributeAbstractClass_CreatesSubstitute` — abstract class substituted
4. `Theory_SubstituteAttributeConcreteNoInterface_CreatesSubstitute` — concrete type (non-substitutable) substituted
5. `Theory_SubstituteAttributeConcreteSubclass_CreatesSubstitute` — concrete subclass case

**Test Results:** 127/127 passing (5 tests validate refactor; 122 existing tests unaffected)