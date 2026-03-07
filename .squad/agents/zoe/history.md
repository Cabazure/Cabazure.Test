# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the test project `Cabazure.Test.Tests`. The unique challenge: we're testing a testing library, and our tests must use that library themselves (dogfooding). Edge cases to watch: sealed classes, value types, types without parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

### Completed Test Coverage (2026-03-07, Phases 1-10)

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

**Test Results:**
- Phase 10 end: **111 tests passing** (all existing + new customization coverage)
- Phase 11 (CancellationToken): **116 tests passing** (5 new CancellationToken tests added)

**Key Testing Patterns Established:**
- **Namespace collision workaround:** Use `new AutoFixture.Fixture()` when ambient `Fixture` namespace exists
- **Sealed class testing:** Composition/wrapping pattern via `ICustomization` (cannot subclass)
- **Static collection mutations:** Protect with try/finally to restore state (prevent cross-test pollution)
- **FluentAssertions limits:** DateOnly/TimeOnly lack comparison operators; use `.Year` property or `.Not.Be(MinValue)` patterns
- **Dogfooding theory tests:** Use `[Theory, AutoNSubstituteData]` where applicable for library-under-test validation
- **Cloning requirement:** JsonElement must use `.Clone()` to survive beyond JsonDocument GC

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### 2026-03-07: Initial Test Coverage Created

**Task:** Write comprehensive tests for SutFixture and AutoNSubstituteDataAttribute

**Test Files Created:**
- `tests/Cabazure.Test.Tests/Fixture/SutFixtureTests.cs` (15 test methods)
  - Covers: Create, CreateMany, Freeze (both overloads), Substitute
  - Test scenarios: value types, interfaces, abstract classes, concrete classes with dependencies
  - Using plain `[Fact]` attributes (can't dogfood yet since we're testing the library itself)
  
- `tests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataAttributeTests.cs` (5 test methods)
  - Covers: various parameter types, `[Frozen]` attribute interaction, multiple parameters
  - Using `[Theory, AutoNSubstituteData]` for dogfooding

**Blockers Found:**
1. Implementation has xUnit v3 API issues - `DataAttribute` base class not found
2. Missing `using AutoFixture.Kernel;` for `ISpecimenBuilder`
3. Documented in `.squad/decisions/inbox/zoe-test-gaps.md` for Kaylee

**Dependencies Updated:**
- Added AutoFixture 4.18.1, AutoFixture.AutoNSubstitute 4.18.1
- Added FluentAssertions 7.0.0, NSubstitute 5.3.0
- Corrected xUnit to use `xunit.v3` package version 3.2.2 (not `xunit` 3.x which doesn't exist)

**Edge Cases for Future Coverage:**
- Sealed classes, generic types, multiple constructors
- Types without parameterless constructors, circular dependencies
- Record types, structs with constructors, collections

**Status:** Tests written and ready. Waiting for Kaylee to fix implementation compilation errors before tests can run.

### 2026-03-07: Field Naming Convention Applied to Tests

**Update:** Following the finalized field naming convention decision (camelCase, no prefix), the test project's field names have been aligned with the production codebase. This ensures consistency across the entire library.

### Phase 9: TypeCustomization<T> Test Coverage

**Date:** 2026-03-07

**TypeCustomizationTests.cs Created:**
- File: `tests/Cabazure.Test.Tests/Customizations/TypeCustomizationTests.cs`
- **15 new comprehensive tests** covering all API surfaces
- All tests passing (106 total: 15 new + 91 existing)

**Test Coverage Areas:**
1. Constructor validation (null-guard)
2. Customize method with ICustomization compliance
3. Factory receiving and invoking on IFixture
4. Wrapping pattern demonstration (composition over inheritance)
5. Add<T>(factory) overload with local fixture isolation
6. Add(ISpecimenBuilder) overload for power users
7. Build<T> integration with custom factories
8. Constructor parameter interception
9. Multiple customization calls
10. Cross-fixture isolation

**Key Test Patterns Established:**
- Use fully qualified `AutoFixture.Fixture` to avoid namespace collision with `Cabazure.Test.Tests.Fixture`
- For sealed classes: composition pattern replaces subclassing in tests
- Explicit casting for ambiguous null parameters in overloaded APIs
- Local fixtures for direct TypeCustomization<T> testing (no global state pollution)
- Factory verification with actual IFixture calls (not stubs/mocks)

**FixtureCustomizationCollectionTests.cs Update:**
- Fixed ambiguous `Add(null!)` call by casting to `(ICustomization)null!`
- Resolved 3-way overload ambiguity

**Build Quality:**
- 0 errors, 0 warnings
- All tests run in isolation mode

**Decision documented in:** `.squad/decisions.md` (TypeCustomization<T> Test Patterns)

**Field Naming Convention (Organization-wide):**
- **Private instance fields:** camelCase, no prefix (e.g., `fixture`, `customizations`)
- **Private static fields:** camelCase, no prefix (no `s_` prefix)
- **Special case:** Keywords like `lock` are replaced with appropriate names (e.g., `syncLock`)

**Rationale:**
- Aligns with Cabazure sibling repos (organization-wide consistency)
- Matches .NET design guidelines for private members (BCL preference for camelCase)
- Enforced via `.editorconfig` analyzer rules
- Tests now follow the same naming convention as production code (no dogfooding inconsistency)

### 2026-03-07: Phase 7 Tests Completed — TestAssemblyInitializer + Missing Coverage

**Task:** Add `TestAssemblyInitializer` with project-wide customization and fill test gaps in `SutFixtureCustomizationsTests` and `CustomizeWithAttributeTests`.

**New files / additions:**
- `tests/Cabazure.Test.Tests/TestAssemblyInitializer.cs` — `[ModuleInitializer]` registers `ProjectWideTestCustomization`; defines `ProjectWideValue` (public record)
- `src/Cabazure.Test/AssemblyInfo.cs` — `[assembly: InternalsVisibleTo("Cabazure.Test.Tests")]` so `SutFixtureCustomizations.All` and `CustomizeWithAttribute.Instantiate()` are directly accessible from tests

**Tests added to `SutFixtureCustomizationsTests.cs` (+3):**
- `ProjectWideCustomization_IsApplied_WhenAutoNSubstituteDataUsed` — end-to-end: `[ModuleInitializer]` → `SutFixtureCustomizations` → fixture → `[AutoNSubstituteData]`
- `All_AfterModuleInitializer_ContainsProjectWideCustomization` — directly asserts `All` contains the project-wide registration
- `Add_MultipleCustomizations_AllCountGrowsByExactAmount` — verifies count grows by exactly 2 after two `Add` calls

**Tests added to `CustomizeWithAttributeTests.cs` (+2):**
- `Constructor_WithNullType_ThrowsArgumentNullException` — direct ctor null guard
- `Instantiate_WithTypeWithoutPublicParameterlessCtor_ThrowsInvalidOperationException` — calls `attr.Instantiate()` directly (enabled by `InternalsVisibleTo`)

**Final test count:** 56 passed, 0 failed (was 39).

**Design notes:**
- `ProjectWideValue` must be `public` (not `internal`) to be a valid `[Theory]` parameter type
- `InternalsVisibleTo` enables direct testing of `All` and `Instantiate()` — removes indirect/reflective test fragility
- `CountTestCustomization` is a private nested class so it doesn't pollute the global registry with a recognizable type



**Task:** Write tests for `SutFixtureCustomizations`, `CustomizeWithAttribute`, and the updated `AutoNSubstituteDataHelper.CreateFixture(MethodInfo)`.

**Test Files Created:**
- `tests/Cabazure.Test.Tests/Customizations/SutFixtureCustomizationsTests.cs` (5 test methods)
  - Covers: null guard on `Add`, and global customization applied by all four data attributes
  - Static constructor registers `GlobalCustomization` for the class-scoped `CustomizedDomainValue` record
  - Safe against inter-test pollution: nested type is only used in this class

- `tests/Cabazure.Test.Tests/Attributes/CustomizeWithAttributeTests.cs` (8 test methods)
  - Covers: method-level, class-level (nested class), multi-stacked, invalid type exception, and all three remaining data attributes
  - Invalid-type test uses `BindingFlags` reflection to obtain a private static helper method decorated with `[CustomizeWith(typeof(string))]` and verifies that `AutoNSubstituteDataAttribute.GetData` throws `InvalidOperationException` synchronously
  - All four data attributes verified with `[CustomizeWith]`

**Key design decisions:**
- Static constructor pattern for `SutFixtureCustomizations` tests to ensure exactly-once registration
- Nested type (`CustomizedDomainValue`) scoped to the test class prevents registry pollution
- Invalid-type test uses a private static helper method as the reflection target — avoids needing `InternalsVisibleTo` while still exercising the `Instantiate()` path through `GetData`

**Build status:** ✅ Compiles clean (`dotnet build`, 0 errors, 0 warnings).

### README Updated — Full Library Documentation

**Task:** Rewrite README.md to reflect the current shipped API.

**What changed:**
- Removed all references to the now-deleted `SutFixture` class
- Replaced old `build.yml` badge with the correct `ci.yml` badge
- Added full documentation for `FixtureFactory.Create()` and `FixtureFactory.Create(params ICustomization[])`
- Added documentation and examples for all four theory data attributes: `AutoNSubstituteData`, `InlineAutoNSubstituteData`, `MemberAutoNSubstituteData`, `ClassAutoNSubstituteData`
- Added documentation for `AutoNSubstituteCustomization` (ConfigureMembers=true, GenerateDelegates=true)
- Added documentation for `RecursionCustomization` (OmitOnRecursionBehavior)
- Added documentation for `ImmutableCollectionCustomization` (all 8 immutable collection types)
- Added documentation for `SutFixtureCustomizations` with `[ModuleInitializer]` pattern
- Added documentation for `[CustomizeWith]` at method and class level
- Added documentation for `[Frozen]` with a realistic multi-dependency example
- Committed as `docs: update README with full library documentation`

**Key observation:** The old README still described `SutFixture` which was replaced by `FixtureFactory` in Phase 8. The README had drifted significantly from the shipped API.

### 2026-03-07: Phase 8 Tests Migrated — SutFixture → FixtureFactory/IFixture

**Task:** Update test files to use the new `FixtureFactory`/`IFixture` API.

**Files Changed:**
- **Deleted** `tests/Cabazure.Test.Tests/Fixture/SutFixtureTests.cs`
- **Created** `tests/Cabazure.Test.Tests/Fixture/FixtureFactoryTests.cs`
  - Class renamed `SutFixtureTests` → `FixtureFactoryTests`
  - All helper types (`IMyInterface`, `MyAbstractClass`, `MyConcreteClass`, `MyServiceWithDependency`) preserved as nested types
  - `new SutFixture()` → `FixtureFactory.Create()`
  - `fixture.Freeze(instance)` → `fixture.Inject(instance)` (AutoFixture's `FixtureRegistrar.Inject<T>`)
  - `fixture.Substitute<T>()` → `NSubstitute.Substitute.For<T>()` — tests that used fixture only to call `Substitute<T>()` no longer need the fixture var
  - Added `using Cabazure.Test;` + `using AutoFixture;`, removed `using Cabazure.Test.Fixture;`
- **Updated** `AutoNSubstituteDataAttributeTests.cs` — `SutFixtureTests.*` → `FixtureFactoryTests.*`
- **Updated** `InlineAutoNSubstituteDataAttributeTests.cs` — same rename
- **Updated** `MemberAutoNSubstituteDataAttributeTests.cs` — same rename
- **Updated** `ClassAutoNSubstituteDataAttributeTests.cs` — same rename
- **No changes needed** in `SutFixtureCustomizationsTests.cs` or `CustomizeWithAttributeTests.cs` — they do not use `SutFixture` directly.

**Key design note:** `Substitute_ReturnsNSubstituteProxy` and `Substitute_ReturnsDifferentInstances_OnMultipleCalls` no longer need a fixture object — they now call `Substitute.For<T>()` directly and omit the unused `FixtureFactory.Create()` call for cleanliness.

**Status:** File edits complete. Build verification pending Kaylee's `FixtureFactory` source changes.


### 2026-03-07: Phase 8 Tests Migrated — SutFixture → FixtureFactory/IFixture

**Task:** Update test files to use the new `FixtureFactory`/`IFixture` API.

**Files Changed:**
- **Deleted** `tests/Cabazure.Test.Tests/Fixture/SutFixtureTests.cs`
- **Created** `tests/Cabazure.Test.Tests/Fixture/FixtureFactoryTests.cs`
  - Class renamed `SutFixtureTests` → `FixtureFactoryTests`
  - All helper types (`IMyInterface`, `MyAbstractClass`, `MyConcreteClass`, `MyServiceWithDependency`) preserved as nested types
  - `new SutFixture()` → `FixtureFactory.Create()`
  - `fixture.Freeze(instance)` → `fixture.Inject(instance)` (AutoFixture's `FixtureRegistrar.Inject<T>`)
  - `fixture.Substitute<T>()` → `NSubstitute.Substitute.For<T>()` — tests that used fixture only to call `Substitute<T>()` no longer need the fixture var
  - Added `using Cabazure.Test;` + `using AutoFixture;`, removed `using Cabazure.Test.Fixture;`
- **Updated** `AutoNSubstituteDataAttributeTests.cs` — `SutFixtureTests.*` → `FixtureFactoryTests.*`
- **Updated** `InlineAutoNSubstituteDataAttributeTests.cs` — same rename
- **Updated** `MemberAutoNSubstituteDataAttributeTests.cs` — same rename
- **Updated** `ClassAutoNSubstituteDataAttributeTests.cs` — same rename
- **No changes needed** in `SutFixtureCustomizationsTests.cs` or `CustomizeWithAttributeTests.cs` — they do not use `SutFixture` directly.

**Key design note:** `Substitute_ReturnsNSubstituteProxy` and `Substitute_ReturnsDifferentInstances_OnMultipleCalls` no longer need a fixture object — they now call `Substitute.For<T>()` directly and omit the unused `FixtureFactory.Create()` call for cleanliness.

**Status:** File edits complete. Build verification pending Kaylee's `FixtureFactory` source changes.

### 2026-03-07: Phase 8 — SutFixtureCustomizations Replaced by FixtureFactory.Customizations

**Notification:** Kaylee has refactored the fixture customization API. The standalone `SutFixtureCustomizations` static class has been removed and its functionality merged into `FixtureFactory.Customizations` (a `FixtureCustomizationCollection` type).

**Impact for Zoe:**
- All test code already uses `FixtureFactory` (Phase 8 migration complete)
- No direct `SutFixtureCustomizations` references in test files
- For future updates: if test code needs to add project-wide customizations, it will use `FixtureFactory.Customizations.Add(...)` (previously `SutFixtureCustomizations.Add(...)`)
- The `[ModuleInitializer]` pattern in `TestAssemblyInitializer` remains unchanged

**Decision Reference:**
- Decision #7: SutFixtureCustomizations → FixtureFactory.Customizations Refactor (API consolidation)
- Decision #8: Phase 8 — FixtureFactory API Design (full design)

### README Rewritten -- Reflects FixtureFactory API (Phase 8+)

**Task:** Rewrite README.md to accurately reflect the current public API after Phase 8 removed SutFixture.

**What changed:**
- Replaced SutFixture / new SutFixture() / using Cabazure.Test.Fixture; with FixtureFactory.Create() / using Cabazure.Test;
- Added Quick Start examples for InlineAutoNSubstituteData, SutFixtureCustomizations + [ModuleInitializer], and [CustomizeWith]
- Expanded Features table to cover all 11 public API surface items: FixtureFactory, all four data attributes, [Frozen], SutFixtureCustomizations, [CustomizeWith], RecursionCustomization, ImmutableCollectionCustomization, and auto-substitution
- Packages and Compatibility sections preserved unchanged

**Key lesson:** README drift is a real risk after refactors. The Phase 8 API change (SutFixture to FixtureFactory) was not reflected in the README -- always treat the README as a deliverable alongside any public API change.

### 2026-03-07: JsonElement and DateOnly/TimeOnly Customization Tests Written

**Task:** Write comprehensive tests for two new customizations being implemented by Kaylee.

**Test Files Created:**
1. `tests/Cabazure.Test.Tests/Customizations/JsonElementCustomizationTests.cs` (6 test methods)
   - Covers: null guard, ValueKind verification, property enumeration, clone/standalone verification
   - Tests: FixtureFactory integration and property-on-object scenarios
   - Key assertion: `JsonElement` is cloned and survives GC (verified with GC.Collect())
   
2. `tests/Cabazure.Test.Tests/Customizations/DateOnlyTimeOnlyCustomizationTests.cs` (7 test methods)
   - Covers: null guard, DateOnly non-default values, TimeOnly non-zero ticks
   - Tests: FixtureFactory default integration (customization is in defaults)
   - Tests: object with both properties populated correctly
   - Key assertion pattern: `result.Year.Should().BeGreaterThan(1)` used instead of `BeGreaterThan(DateOnly.MinValue)` because `DateOnlyAssertions` doesn't have comparison operators

**Test Pattern Learnings:**
- **JsonElement testing:** Must verify `ValueKind`, property enumeration, and clone independence (GC-safe)
- **DateOnly/TimeOnly testing:** FluentAssertions doesn't provide comparison operators for these types — use `.Year` property access or `NotBe(MinValue)` patterns
- **Randomness verification:** Create multiple values and assert at least one differs from `MinValue` (TimeOnly) to verify non-trivial generation
- **Nested test classes:** Used `HasJsonElementProperty` and `HasDateTimeOnlyProperties` to test property-on-object scenarios

**Design decisions:**
- JsonElementCustomization is opt-in (not in defaults) — tests explicitly add it via `FixtureFactory.Create(new JsonElementCustomization())`
- DateOnlyTimeOnlyCustomization is in defaults — tests use `FixtureFactory.Create()` without explicit customization parameter
- Both follow the pattern established in `ImmutableCollectionCustomizationTests.cs`

**Build Status:** ✅ Compiles clean (`dotnet build tests\Cabazure.Test.Tests\Cabazure.Test.Tests.csproj`, 0 errors, 0 warnings after fixing FluentAssertions DateOnly comparison issue)

**Not Run:** Tests not executed yet — Kaylee is implementing the customization source files in parallel.

### Session Integration — JsonElement & DateOnly/TimeOnly Customizations (2026-03-07)

**Cross-Team Update from Kaylee:**
- Kaylee completed implementation of both customizations (`JsonElementCustomization` and `DateOnlyTimeOnlyCustomization`)
- Updated `FixtureCustomizationCollection` to seed `DateOnlyTimeOnlyCustomization` by default (opt-in for `JsonElementCustomization`)
- Updated README with full documentation and usage examples
- Committed: `feat(customizations): add JsonElementCustomization and DateOnlyTimeOnlyCustomization`

**Test Execution Result:**
- All 91 tests passing (13 new from this session + 78 existing)
- No test failures or gaps detected
- Code/test alignment is complete

**Decisions Merged into `.squad/decisions.md`:**
- Decision #10: DateOnly/TimeOnly and JsonElement Customization Defaults
- Decision #11: Test Coverage for JsonElement and DateOnly/TimeOnly Customizations

**Status:** Both customizations production-ready. Full alignment between implementation and test coverage achieved.

### 2026-03-07: Phase 9 — TypeCustomization<T> Tests Completed

**Task:** Write comprehensive tests for the new `TypeCustomization<T>` generic customization and the two new overloads in `FixtureCustomizationCollection`.

**Implementation being tested:**
- `src/Cabazure.Test/Customizations/TypeCustomization.cs` — sealed generic `TypeCustomization<T> : ICustomization` with factory function
- `src/Cabazure.Test/FixtureCustomizationCollection.cs` — two new overloads: `Add<T>(Func<IFixture, T> factory)` and `Add(ISpecimenBuilder builder)`

**Test File Created:**
- `tests/Cabazure.Test.Tests/Customizations/TypeCustomizationTests.cs` (15 test methods)
  
**Coverage areas:**

1. **TypeCustomization<T> Core Tests:**
   - `Create_ReturnsDelegateResult_WhenTypeMatchesExactly` — factory returns 42, fixture creates 42
   - `Create_ReturnsDelegateResult_ForReferenceType` — string factory with constant value
   - `Create_ReturnsNoSpecimen_ForNonMatchingType` — int customization doesn't affect string creation
   - `Customize_ThrowsArgumentNullException_WhenFixtureIsNull` — null guard on Customize
   - `Constructor_ThrowsArgumentNullException_WhenFactoryIsNull` — null guard on constructor
   - `Factory_ReceivesIFixture_WithWorkingCreate` — factory receives working IFixture instance

2. **Wrapping Pattern Test:**
   - `TypeCustomization_CanBeWrappedInCustomClass` — demonstrates wrapping in custom ICustomization (cannot subclass sealed class)

3. **FixtureCustomizationCollection Overload Tests:**
   - `Add_WithFactory_CreatesAndAddsTypeCustomization` — convenience method creates TypeCustomization<T>
   - `Add_WithFactory_ThrowsArgumentNullException_WhenFactoryIsNull` — null guard
   - `Add_WithSpecimenBuilder_RegistersBuilder` — NSubstitute mock builder registered and invoked
   - `Add_WithSpecimenBuilder_ThrowsArgumentNullException_WhenBuilderIsNull` — null guard

4. **Integration Tests:**
   - `TypeCustomization_CanUseIFixture_Build_T` — factory uses `fixture.Build<T>().With(...).Create()` pattern
   - `Create_UsesFactory_ForConstructorParameter` — factory intercepts constructor parameters
   - `Create_UsesFactory_MultipleTimesForMultipleRequests` — factory invoked on each Create() call
   - `Create_DoesNotInterfere_WithOtherCustomizations` — verify isolation from other types

**Test Pattern Learnings:**
- **Namespace collision avoided:** Must use `new AutoFixture.Fixture()` because there is a `Fixture` namespace in tests, causing ambiguity with `AutoFixture.Fixture` type
- **Sealed class pattern:** `TypeCustomization<T>` is sealed (cannot subclass) — documented the wrapping pattern via `ICustomization` instead
- **Overload ambiguity fix:** `FixtureCustomizationCollection` now has 3 overloads of `Add`, making `null!` parameter ambiguous. Fixed existing test in `FixtureCustomizationCollectionTests.cs` with explicit cast: `(ICustomization)null!`
- **Local fixture creation:** Used `new AutoFixture.Fixture()` + direct `Customize()` calls to avoid polluting global `FixtureFactory.Customizations` state
- **NSubstitute ISpecimenBuilder:** Used `Substitute.For<ISpecimenBuilder>()` to verify builder registration works correctly

**Test Results:** ✅ All 106 tests passing (15 new + 91 existing)

**Build Status:** ✅ Compiles clean with 0 errors, 0 warnings

**Design Notes:**
- TypeCustomization<T> is `sealed` by design — composition/wrapping pattern is the intended reuse mechanism
- Factory receives a fully functional `IFixture` instance, enabling powerful patterns like `Build<T>().With(...).Create()`
- Tests use dogfooding (`[AutoNSubstituteData]`) where applicable for theory tests

## Learnings

### SpecimenRequestHelperTests (2026-03-07)
- Created 	ests/Cabazure.Test.Tests/Customizations/SpecimenRequestHelperTests.cs covering all 5 branches of SpecimenRequestHelper.GetRequestType
- Used a private nested TestSubject class with a constructor parameter (ool someParameter), a property (string? SomeProperty), and a field (int SomeField) to provide reflection targets for all MemberInfo test cases
- All 5 tests use [Fact] — pure static method, no AutoFixture/NSubstitute needed
- Build fails with CS0103 (SpecimenRequestHelper not found) because Kaylee's src/Cabazure.Test/Customizations/SpecimenRequestHelper.cs does not yet exist — test file is written and ready to compile once it lands
- CS0649 warning on SomeField (never assigned) is harmless — field is only accessed via reflection, not directly
- **Static helper test pattern:** Pure static switch-expression helpers need no AutoFixture/NSubstitute setup — plain `[Fact]` with local reflection lookups is the correct pattern; dogfooding ([AutoNSubstituteData]) adds no value when there are no injectable parameters
- **Self-contained test subject:** Defining a private nested class (`TestSubject`) inside the test class avoids importing external members; every MemberInfo branch (ParameterInfo, PropertyInfo, FieldInfo) is satisfied by a single small class
- **No `using System.Reflection;` needed:** `ImplicitUsings` is enabled for the test project (net9.0), so `System.Reflection` types are globally available without an explicit using directive


---

## Phase 10: SpecimenRequestHelper Test Coverage & Edge Case Analysis (2026-03-07)

### SpecimenRequestHelperTests Created
- File: tests/Cabazure.Test.Tests/Customizations/SpecimenRequestHelperTests.cs
- 5 tests covering all branches of SpecimenRequestHelper.GetRequestType
- Branches: ParameterInfo, PropertyInfo, FieldInfo, Type, and unknown (falls to null)
- Self-contained TestSubject nested class provides reflection targets
- Pure static method testing: [Fact] with no AutoFixture/NSubstitute setup needed

### Edge Cases Documented
- Null input handling: noted potential NullReferenceException with unsafe code
- MemberInfo subtypes (MethodInfo, EventInfo, ConstructorInfo) fall through to null
- CS0649 warning on TestSubject.SomeField harmless (accessed only via reflection)
- ParameterInfo sourced from constructors, consistent with AutoFixture pipeline

All 111 tests passing. QA approval for Phase 10 integration.

### 2026-03-07: CancellationTokenCustomization Tests Written

**Task:** Write comprehensive tests for CancellationTokenCustomization (implemented by Kaylee in parallel).

**Test File Created:**
- 	ests/Cabazure.Test.Tests/Customizations/CancellationTokenCustomizationTests.cs (5 test methods)

**Coverage Areas:**
1. **Default behavior: non-cancelled token** — CancellationTokenCustomization_CreatesToken_WithIsCancellationRequestedFalse
   - Creates fixture via FixtureFactory.Create(), resolves CancellationToken, asserts IsCancellationRequested == false
   - Verifies the fix for AutoFixture's default 
ew CancellationToken(true) bug

2. **CanBeCanceled is false** — CancellationTokenCustomization_CreatesToken_WithCanBeCanceledFalse
   - Documents the known limitation: the default token is non-cancellable by design
   - Produces CancellationToken.None / default(CancellationToken) / 
ew CancellationToken(false) (all equivalent)

3. **Multiple resolutions produce equal tokens** — CancellationTokenCustomization_CreatesTwoTokens_ThatAreEqual
   - From a single fixture, resolve CancellationToken twice
   - Both should be equal to each other and to CancellationToken.None
   - Demonstrates frozen-value behavior through AutoFixture's default caching

4. **Removable from defaults** — CancellationTokenCustomization_WhenRemoved_AllowsAutoFixtureDefault
   - Remove CancellationTokenCustomization from FixtureFactory.Customizations via Remove<T>()
   - Create fixture, resolve token, assert IsCancellationRequested == true (AutoFixture default is already-cancelled)
   - **Restore the customization afterward** using 	ry/finally to avoid cross-test pollution
   - This test verifies opt-out behavior as documented in the XML remarks

5. **Integration with [AutoNSubstituteData]** — CancellationTokenCustomization_WithAutoNSubstituteData_ProvidesNonCancelledToken
   - [Theory, AutoNSubstituteData] with CancellationToken ct parameter
   - Asserts ct.IsCancellationRequested == false (dogfooding verification)

**Test Patterns Learned:**
- **Static collection modification requires cleanup:** FixtureFactory.Customizations is static; tests that mutate it must restore to original state in inally block
- **try/finally pattern:** Used in WhenRemoved test to ensure restoration even if assertion fails
- **Dogfooding with theory parameters:** [Theory, AutoNSubstituteData] used to verify CancellationToken injection works correctly

**Build Result:**
- ✅ All 111 tests passing (5 new + 106 existing)
- 0 errors, 0 warnings
- Implementation exists and is registered in defaults (Kaylee completed in parallel)

**Key Design Note:**
The customization fixes a critical AutoFixture bug where default ool resolution as 	rue produces already-cancelled tokens (
ew CancellationToken(true)). This causes methods that check IsCancellationRequested at entry to fail silently in tests. The fix uses 
ew CancellationToken(false) which is equivalent to CancellationToken.None — a safe default that doesn't poison test data.

**Status:** Tests complete and passing. Implementation verified working as designed.

### Phase 12: Fixture Injection Tests (2026-03-07T17:33:43Z)

**Task:** Write tests for AutoNSubstituteDataHelper fixture instance injection.

**Test File Created:**
- 	ests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataHelperFixtureInjectionTests.cs (6 tests)

**Coverage:**
1. Theory_IFixtureParameter_IsNotNull — resolved value is not null
2. Theory_IFixtureParameter_IsFixtureInstance — resolved value is a concrete AutoFixture.Fixture
3. Theory_IFixtureParameter_IsSameInstanceResolvingOtherParams — injected fixture is same instance that resolved [Frozen] params
4. Theory_ConcreteFixtureParameter_IsInjected — concrete Fixture type also works
5. InlineData_WithIFixtureParameter_InjectsFixture — IFixture fills auto slot after explicit inline value
6. Theory_FrozenIFixtureParameter_IsInjectedNormally — [Frozen] on IFixture doesn't throw

**Test Results:** ✅ 122/122 passing (6 new + 116 existing)

**Key Pattern:** Test 3 (same-instance check using [Frozen] + ixture.Create<T>()) is the highest-value assertion — proves injection uses the shared fixture, not a new one.


### Phase 12: Fixture Injection Tests (2026-03-07T17:33:43Z)

**Task:** Write tests for AutoNSubstituteDataHelper fixture instance injection.

**Test File Created:** tests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataHelperFixtureInjectionTests.cs (6 tests)

**Coverage:**
1. Theory_IFixtureParameter_IsNotNull
2. Theory_IFixtureParameter_IsFixtureInstance
3. Theory_IFixtureParameter_IsSameInstanceResolvingOtherParams (same-instance check with [Frozen])
4. Theory_ConcreteFixtureParameter_IsInjected
5. InlineData_WithIFixtureParameter_InjectsFixture
6. Theory_FrozenIFixtureParameter_IsInjectedNormally

**Test Results:** 122/122 passing (6 new + 116 existing)

**Key Pattern:** Test 3 is highest-value: proves injected fixture is the shared instance, not a new one.