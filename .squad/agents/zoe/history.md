# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the test project `Cabazure.Test.Tests`. The unique challenge: we're testing a testing library, and our tests must use that library themselves (dogfooding). Edge cases to watch: sealed classes, value types, types without parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

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

