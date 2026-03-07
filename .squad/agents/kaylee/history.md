# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the guts of the library: `SutFixture`, AutoFixture customizations, the `ISpecimenBuilder` that routes interface/abstract-class requests to NSubstitute. Key challenge: AutoFixture doesn't natively create substitutes for abstract/interface types; we bridge that via `AutoNSubstituteCustomization`.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### 2026-03-07: Core Library Implementation

**AutoFixture + NSubstitute Integration:**
- `AutoNSubstituteCustomization` wraps `AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization` with `ConfigureMembers=true` and `GenerateDelegates=true` to ensure interfaces/abstract classes are automatically substituted
- The key insight: AutoFixture's specimen pipeline needs explicit customization to route abstract/interface requests to NSubstitute's `Substitute.For<T>()`
- Without customization, AutoFixture throws when it can't construct interfaces/abstract types

**SutFixture Design:**
- Wraps `IFixture` to provide a clean API: `Create<T>()`, `Freeze<T>()`, `Freeze<T>(instance)`, `Customize<T>()`, `Substitute<T>()`
- Two `Freeze` overloads: parameterless creates+registers, parameter-based registers existing instance
- `Substitute<T>()` creates a one-off substitute WITHOUT fixture registration (useful for test-specific mocks)
- Constructor accepts `params ICustomization[]` to allow extensibility beyond NSubstitute

**xUnit 3 DataAttribute Pattern:**
- xUnit 3 still supports `DataAttribute` base class with `GetData(MethodInfo)` returning `IEnumerable<object[]>`
- `AutoNSubstituteDataAttribute` uses reflection to:
  1. Instantiate `SutFixture` per test method invocation
  2. Resolve parameters left-to-right
  3. Freeze parameters marked with `[Frozen]` AFTER creation so subsequent params get the frozen instance
- Edge case: `Freeze<T>(instance)` requires finding the right overload via reflection (1-param, generic parameter type)

**Module Initializer:**
- `[ModuleInitializer]` runs before any test execution, perfect for global xUnit 3 config
- Currently empty (reserved for future use) but establishes the pattern
- Avoids static constructor ordering issues in test runners

### Phase 7: User-Defined Fixture Customizations

**SutFixtureCustomizations (global registry):**
- `static class SutFixtureCustomizations` in `Customizations/` namespace — thread-safe via `lock` on a private `object syncLock`
- `Add(ICustomization)` is the public surface; `All` is `internal` returning a snapshot copy so callers can't mutate the list
- Users call `Add()` from `[ModuleInitializer]` for assembly-wide effect

**CustomizeWithAttribute:**
- `AllowMultiple = true` on `Method | Class` targets; `internal Instantiate()` validates type + parameterless ctor before calling `Activator.CreateInstance`

### Phase 9: TypeCustomization<T> Generic Factory

**Date:** 2026-03-07

**TypeCustomization<T> Class:**
- New sealed generic class in `src/Cabazure.Test/Customizations/TypeCustomization.cs`
- Wraps `Func<IFixture, T>` factory for ergonomic type registration
- Implements `ICustomization` interface with full null-safety
- Factory receives `IFixture` (not `ISpecimenContext`) for high-level API access

**Rationale for IFixture:**
- Ergonomic: Users write `f => f.Create<DateOnly>()` instead of `context.Resolve(typeof(T))`
- Consistent: Aligns with existing codebase conventions
- Discoverable: IDE autocomplete exposes full AutoFixture fluent API
- Safe: IFixture instance captured during Customize ensures correct state

**FixtureCustomizationCollection API Extensions:**
- `Add<T>(Func<IFixture, T> factory)` — inline factory registration (primary API)
- `Add(ISpecimenBuilder builder)` — power-user escape hatch for kernel control
- Backward compatible with existing `Add(ICustomization)` method

**Build Results:**
- Release mode: 0 errors, 0 warnings (TreatWarningsAsErrors=true)
- Ready for QA testing

**Decision documented in:** `.squad/decisions.md` (TypeCustomization<T> Factory Pattern)
- Two validation checks produce `InvalidOperationException` with clear diagnostic messages: (1) does not implement `ICustomization`, (2) no public parameterless constructor

**CreateFixture layering order (critical):**
1. `AutoNSubstituteCustomization` — always first so NSubstitute is always the fallback
2. `SutFixtureCustomizations.All` — project-wide registrations
3. `[CustomizeWith]` on the test **method** — method-level overrides
4. `[CustomizeWith]` on the test **class** — class-level defaults (applied last; later = higher priority in AutoFixture)
- Note: class-level is applied AFTER method-level, which is intentional — class attributes act as defaults that can be overridden per-method if a later customization wins

**All 4 data attributes updated:**
- `AutoNSubstituteDataAttribute`, `InlineAutoNSubstituteDataAttribute`, `MemberAutoNSubstituteDataAttribute`, `ClassAutoNSubstituteDataAttribute` all replaced `new SutFixture()` with `AutoNSubstituteDataHelper.CreateFixture(testMethod)`
- Per-row fixture creation in `Member` and `Class` variants also updated — each row gets its own fully-customized fixture (no cross-row state leakage)

### Coding Style Alignment (Cabazure sibling repos)

**editorconfig / Directory.Build.props / LICENSE:**
- Replaced root `.editorconfig` with the full Cabazure style (matches Cabazure.Client), with one key difference: private fields use plain `camelcase` (no `_` prefix), private static fields also use `camelcase` (no `s_` prefix).
- Created `tests/.editorconfig` suppressing test-friendly diagnostics (CA1707, xUnit1051, CA2007, etc.) — keeps test code readable without noise.
- Created `Directory.Build.props` at repo root providing company-wide metadata (`Authors=Cabazure`, `Description`, `RepositoryUrl`, `DebugSymbols`, `DebugType`, `TreatWarningsAsErrors` in Release, deterministic build on CI).
- Created `LICENSE` (MIT, Copyright 2024 Cabazure).
- Removed duplicated properties from `Cabazure.Test.csproj` (`Authors`, `Description`, `RepositoryUrl`) now provided by `Directory.Build.props`. Kept `PackageId`, `Version`, `PackageTags`, `PackageLicenseExpression`, `PackageReadmeFile`, `NoWarn` (CA2255).

**Field naming convention enforced:**
- `SutFixture.cs`: `_fixture` → `fixture`
- `SutFixtureCustomizations.cs`: `_customizations` → `customizations`, `_lock` → `syncLock` (reserved keyword avoidance)
- Build: 0 warnings, 56/56 tests green after all changes.

### Phase 8: Replace SutFixture with FixtureFactory

**FixtureFactory (new public API):**
- Created `src/Cabazure.Test/FixtureFactory.cs` in root `Cabazure.Test` namespace (not a subfolder).
- `public static IFixture Create()` — no-arg convenience, delegates to `Create([])`.
- `public static IFixture Create(params ICustomization[])` — applies `AutoNSubstituteCustomization` first, then each supplied customization in order.
- `internal static IFixture Create(MethodInfo)` — full priority stack: AutoNSubstitute → SutFixtureCustomizations.All → `[CustomizeWith]` on method → `[CustomizeWith]` on class. Used only by data attributes via `AutoNSubstituteDataHelper`.

**AutoNSubstituteDataHelper (reflection eliminated):**
- `CreateFixture` now one-liner delegating to `FixtureFactory.Create(MethodInfo)` — returns `IFixture`, not `SutFixture`.
- `MergeValues` signature changed: `SutFixture` → `IFixture` (no callers required updating since they use `var`).
- `CreateValue` replaced `MakeGenericMethod(type)` call with `new SpecimenContext(fixture).Resolve(type)` — kernel API, zero reflection.
- `FreezeValue` replaced reflected `Freeze<T>(instance)` call with `fixture.Customizations.Insert(0, SpecimenBuilderNodeFactory.CreateTypedNode(type, new FixedBuilder(value)))` — same mechanism `FreezingCustomization` uses internally.
- Added `using AutoFixture.Kernel;`, removed `using Cabazure.Test.Fixture;` and `using Cabazure.Test.Customizations;`.

**SutFixture deleted:**
- `src/Cabazure.Test/Fixture/SutFixture.cs` — deleted.
- `src/Cabazure.Test/Fixture/AssemblyInitializer.cs` — moved to `src/Cabazure.Test/AssemblyInitializer.cs`; namespace updated from `Cabazure.Test.Fixture` to `Cabazure.Test`.
- `src/Cabazure.Test/Fixture/` directory (including `.gitkeep`) — deleted.

**4 data attributes updated:**
- Removed `using Cabazure.Test.Fixture;` from all four.
- Updated XML doc references from `SutFixture` to `FixtureFactory`.

**Supporting file XML docs cleaned:**
- `SutFixtureCustomizations.cs` — replaced `Fixture.SutFixture` cref with `IFixture` / `IFixture`.
- `AutoNSubstituteCustomization.cs` — replaced `Fixture.SutFixture` cref with `FixtureFactory`.

**Build result:** 0 errors, 0 warnings (Release TreatWarningsAsErrors is on).

### RecursionCustomization

**RecursionCustomization (new public API):**
- Created `src/Cabazure.Test/Customizations/RecursionCustomization.cs` in `Cabazure.Test.Customizations` namespace.
- `public sealed class` — consistent with `AutoNSubstituteCustomization` pattern.
- `Customize(IFixture)` uses `ArgumentNullException.ThrowIfNull(fixture)` — throws on null rather than silently ignoring it.
- Core logic: remove all `ThrowingRecursionBehavior` instances from `fixture.Behaviors`, then add `OmitOnRecursionBehavior`.
- XML doc on class; `/// <inheritdoc />` on `Customize` method.

**Test file:** `tests/Cabazure.Test.Tests/Customizations/RecursionCustomizationTests.cs`
- 5 `[Fact]` tests covering: null fixture throws, `ThrowingRecursionBehavior` removed, `OmitOnRecursionBehavior` added, recursive type does not throw, works with `FixtureFactory.Create(new RecursionCustomization())`.
- Uses `AutoFixture.Fixture` (fully qualified) because `Fixture` is also a namespace in the test project — namespace collision pitfall to remember.

**Build result:** 0 errors, 0 warnings, 61/61 tests green.

### ImmutableCollectionCustomization

**Verification — AutoFixture 4.18.1 does NOT handle immutable collections natively:**
- `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableHashSet<T>`, `ImmutableDictionary<TKey,TValue>` → `ObjectCreationExceptionWithPath` (throws)
- `ImmutableQueue<T>`, `ImmutableStack<T>` → created but *empty* (no items populated)
- The customization is required for all eight types.

**Dynamic dispatch confirmed working:**
- `ImmutableQueue.CreateRange(dynamic)` and `ImmutableStack.CreateRange(dynamic)` both work via dynamic dispatch — no cast or reflection workaround needed.
- All `ToImmutableXxx(dynamic)` extension methods also resolve correctly.

**PropertyInfo/FieldInfo fix confirmed critical:**
- AutoFixture sends `PropertyInfo` requests when populating object properties.
- Without the `PropertyInfo pi => pi.PropertyType` arm in `GetRequestType`, a class with `public ImmutableList<string> Tags { get; set; }` would receive an empty default — the property would NOT be populated.
- Test `Customize_PopulatesProperty_OnObjectWithImmutableListProperty` validates this path.

**Implementation details:**
- `public sealed class ImmutableCollectionCustomization` in `Cabazure.Test.Customizations` namespace.
- Private nested `ImmutableCollectionBuilder : ISpecimenBuilder` handles all eight immutable types.
- `ArgumentNullException.ThrowIfNull(fixture)` guards the `Customize` method.
- 10 `[Fact]` tests: null-fixture throws, one per collection type (8 total), plus property-population test.

**Build result:** 0 errors, 0 warnings, 71/71 tests green.

### FixtureFactory.Customizations Refactor

**FixtureCustomizationCollection (new public type):**
- Thread-safe ordered collection in `Cabazure.Test` namespace; internal constructor, only `FixtureFactory` instantiates it.
- Pre-seeded with `Cabazure.Test.Customizations.AutoNSubstituteCustomization` (not the raw AutoFixture.AutoNSubstitute one — important: uses ConfigureMembers+GenerateDelegates).
- Private fields named with plain camelCase per team convention: `customizations`, `syncLock`.
- `GetEnumerator()` returns a snapshot to prevent modification-during-enumeration issues.
- `Clear()` intentionally exposed (unlike old `SutFixtureCustomizations` which had no `Clear`) — test isolation is possible with a local `new FixtureCustomizationCollection()`.

**SutFixtureCustomizations deleted:**
- `src/Cabazure.Test/Customizations/SutFixtureCustomizations.cs` removed; `FixtureFactory.Customizations` is the single entry point.

**Test migration:**
- `SutFixtureCustomizationsTests.cs` → `FixtureCustomizationCollectionTests.cs` with expanded coverage (Remove, Remove<T>, Clear, snapshot enumeration, per-attribute integration tests).
- Uses static constructor (runs once per class, before any test) to register `GlobalCustomization`, avoiding module-initializer ordering issues in tests.

**copilot-instructions.md updated:**
- `SutFixtureCustomizations` section replaced with `FixtureFactory.Customizations` section.

**Build result:** 0 errors, 0 warnings, 78/78 tests green.

### Copilot Instructions Update

Updated `.github/copilot-instructions.md` to reflect post-Phase-8 library state:
- Added **README Sync Rule** section (after Squad History Commit Rule) — requires `README.md` to be updated in the same commit whenever public API, attributes, customizations, or breaking changes are introduced.
- Fixed **private field naming** in Code Style: changed `_camelCase` to `camelCase` with no underscore prefix, aligning with the team decision documented in `.squad/decisions.md`.
- Replaced **`SutFixture` / `AutoNSubstituteDataAttribute`** concept docs with the current public API: `FixtureFactory`, all four Data Attributes, the three Customizations, `SutFixtureCustomizations`, and `[CustomizeWith]`.
- Updated **Project Structure** tree: removed `Fixture/` directory (deleted in Phase 8), added `AssemblyInitializer.cs` and `FixtureFactory.cs` entries.
- Updated **commit scope table**: `fixture` scope description changed from "SutFixture and fixture types" to "FixtureFactory and fixture configuration".

### Default Customizations Expansion

**FixtureCustomizationCollection now seeds all three standard customizations:**
- Updated `FixtureCustomizationCollection` constructor to include `RecursionCustomization` and `ImmutableCollectionCustomization` alongside the existing `AutoNSubstituteCustomization`.
- Updated XML `<remarks>` block to list all three pre-seeded customizations by name.
- No test changes required — `FixtureCustomizationCollectionTests` uses relative count assertions (`countBefore + 2`) and the `Clear_RemovesAll` test checks post-clear count (0), both unaffected by seed count changing 1 → 3.
- Tests for `RecursionCustomization` and `ImmutableCollectionCustomization` all pass unchanged (they use explicit `FixtureFactory.Create(new XxxCustomization())` so default seeding is irrelevant).

**Build result:** 0 errors, 0 warnings, 78/78 tests green.

### DateOnlyTimeOnlyCustomization and JsonElementCustomization

**AutoFixture 4.18.1 on .NET 9 behaviour (verified):**
- `DateOnly`: **FAILS** — throws `ArgumentOutOfRangeException` because AutoFixture generates invalid year/month/day combos via the `(int, int, int)` constructor (e.g., month=13, day=32).
- `TimeOnly`: Technically does not throw, but generates near-zero tick values (ticks=61, 64, 17 etc.) — essentially always midnight, which is useless for tests requiring varied times.
- `JsonElement`: **FAILS** — AutoFixture cannot construct the `ref Utf8JsonReader` parameter required by `JsonElement` constructors.

**DateOnlyTimeOnlyCustomization implementation:**
- Created `src/Cabazure.Test/Customizations/DateOnlyTimeOnlyCustomization.cs` with a single `DateTimeOnlyBuilder : ISpecimenBuilder`.
- Derives both `DateOnly` and `TimeOnly` from a randomly generated `DateTime` via `context.Resolve(typeof(DateTime))`.
- Uses `DateOnly.FromDateTime(dateTime)` and `TimeOnly.FromDateTime(dateTime)` for conversion — ensures valid, well-distributed results.
- Pattern matches request type via `ParameterInfo`, `PropertyInfo`, `FieldInfo`, or `Type` — same approach as `ImmutableCollectionCustomization`.
- **Added to default seed** in `FixtureCustomizationCollection` alongside `AutoNSubstituteCustomization`, `RecursionCustomization`, and `ImmutableCollectionCustomization`.

**JsonElementCustomization implementation:**
- Created `src/Cabazure.Test/Customizations/JsonElementCustomization.cs` with a single `JsonElementBuilder : ISpecimenBuilder`.
- Creates a `JsonElement` by parsing a JSON object with a randomly generated key/value string pair: `{"key":"value"}`.
- **CRITICAL: `.Clone()` is required.** `JsonElement` backed by an un-cloned `JsonDocument` becomes invalid when the document is garbage-collected. `.Clone()` creates a document-independent copy that survives past the `JsonDocument`'s lifetime.
- Pattern matches request type via `ParameterInfo`, `PropertyInfo`, `FieldInfo`, or `Type` — consistent with other specimen builders.
- **NOT added to defaults** — opt-in only. `JsonElement` is a specialized type not everyone uses; adding it to defaults would pollute the fixture for projects that don't need it.

**Scope decision:**
- `DateOnlyTimeOnlyCustomization`: default — addresses a broad .NET 9+ API gap (AutoFixture can't handle .NET 6+ date/time types).
- `JsonElementCustomization`: opt-in — specialized use case (System.Text.Json serialization tests).

**FixtureCustomizationCollection update:**
- Constructor now seeds four customizations: `AutoNSubstituteCustomization`, `RecursionCustomization`, `ImmutableCollectionCustomization`, `DateOnlyTimeOnlyCustomization`.
- XML `<remarks>` updated to list all four by name.

**README.md update:**
- Added both customizations to the Features table.
- Added dedicated "## Customizations" section with full documentation for all four default customizations plus `JsonElementCustomization`.
- Documented default vs opt-in behavior and provided usage examples for `JsonElementCustomization`.

**Build result:** 0 errors, 0 warnings. Tests pending Zoe's completion.

### Session Integration — JsonElement & DateOnly/TimeOnly Customizations (2026-03-07)

**Cross-Team Update from Zoe:**
- Zoe completed comprehensive test coverage (13 new test methods) for both customizations
- All 91 tests passing (13 new + 78 existing)
- Test files created: `JsonElementCustomizationTests.cs` and `DateOnlyTimeOnlyCustomizationTests.cs`
- Key learning shared: FluentAssertions 7.0 lacks DateOnly/TimeOnly comparison operators; workarounds documented

**Decisions Merged into `.squad/decisions.md`:**
- Decision #10: DateOnly/TimeOnly and JsonElement Customization Defaults
- Decision #11: Test Coverage for JsonElement and DateOnly/TimeOnly Customizations

**Status:** Both customizations ready for production. Code and tests fully aligned, no gaps.

### Phase 9: TypeCustomization<T> (2026-03-07)

**TypeCustomization<T> implementation:**
- Created `src/Cabazure.Test/Customizations/TypeCustomization.cs` — a generic sealed class that wraps a factory function for creating instances of `T`.
- Constructor accepts `Func<IFixture, T> factory` — the factory receives the actual `IFixture` instance (captured during `Customize()`), enabling users to call `f.Create<T>()`, `f.Build<T>().With(...).Create()`, etc.
- Private nested `DelegateBuilder : ISpecimenBuilder` handles specimen creation; pattern matches request type via `ParameterInfo`, `PropertyInfo`, `FieldInfo`, or `Type` (same as other builders in this project).
- Returns `new NoSpecimen()` for non-matching types; returns `factory(fixture)!` for matching types (non-null assertion needed since T could be value type).
- Full XML documentation includes two `<example>` blocks: direct instantiation (`new TypeCustomization<JsonElement>(f => ...)`) and subclassing pattern (`public sealed class MyCustomization : TypeCustomization<MyType>`).

**FixtureCustomizationCollection extensions:**
- Added `Add<T>(Func<IFixture, T> factory)` overload — simplest way to register inline factory, wraps in `TypeCustomization<T>`.
- Added `Add(ISpecimenBuilder builder)` overload — power-user API for full `ISpecimenBuilder` control, wraps in private `SpecimenBuilderCustomizationWrapper : ICustomization`.
- Both overloads placed immediately after the existing `Add(ICustomization)` method, before `Remove` methods.
- `SpecimenBuilderCustomizationWrapper` is a private sealed nested class at the bottom of the file (after enumerator methods).
- Added `using AutoFixture.Kernel;` and ensured `using Cabazure.Test.Customizations;` present.

**Design decision captured:**
- The factory receives `IFixture` (not `ISpecimenContext`) — this gives users the familiar, high-level AutoFixture API instead of forcing them to use the low-level kernel API.
- This is critical for ergonomics: `f => DateOnly.FromDateTime(f.Create<DateTime>())` is far cleaner than context resolution.

**Build result:** 0 errors, 0 warnings (Release mode with TreatWarningsAsErrors on).

### Phase 10: Readability Refactoring & DRY Cleanup (2026-03-07)

**SpecimenRequestHelper extracted:**
- Created `src/Cabazure.Test/Customizations/SpecimenRequestHelper.cs` — public static helper with a single `GetRequestType(object request)` method.
- Eliminates 4 identical private `GetRequestType` copies from `TypeCustomization<T>.DelegateBuilder`, `DateTimeOnlyBuilder`, `ImmutableCollectionBuilder`, and `JsonElementBuilder`.
- Public visibility adds user-facing value: anyone implementing a custom `ISpecimenBuilder` can use it without reimplementing the pattern-match switch.
- Removed now-unused `using System.Reflection;` imports from `TypeCustomization.cs`, `DateOnlyTimeOnlyCustomization.cs`, and `ImmutableCollectionCustomization.cs` (reflection types accessed via the helper's namespace).

**TypeCustomization<T> unsealed:**
- Changed `public sealed class TypeCustomization<T>` to `public class TypeCustomization<T>` to allow subclassing.
- This enables Refactoring 3 (JsonElementCustomization subclass) and opens a user extensibility pattern documented in the XML docs' `<example>` block.

**JsonElementCustomization simplified:**
- Replaced standalone `ICustomization` + `ISpecimenBuilder` implementation with `public sealed class JsonElementCustomization : TypeCustomization<JsonElement>`.
- Constructor calls `base(f => ...)` — the entire creation logic becomes a single lambda, eliminating the private nested builder class entirely.
- `using System.Reflection;` and `using AutoFixture.Kernel;` removed; `using AutoFixture;` added for `IFixture` (used in the lambda parameter type).

**ApplyCustomizations helper extracted in FixtureFactory:**
- Added `private static void ApplyCustomizations(IFixture fixture, IEnumerable<ICustomization> customizations)` method.
- Replaced two identical `foreach (var customization in ...) fixture.Customize(customization);` loops in `Create(params ICustomization[])` and `Create(MethodInfo)` with calls to this helper.

**Build result:** 0 errors, 0 warnings, all tests green.




---

## Phase 10: Readability Refactoring & DRY Elimination (2026-03-07)

### SpecimenRequestHelper Public Static Helper
- Extracted public static class SpecimenRequestHelper with GetRequestType(object request) method
- Eliminated four identical private copies from inner builders
- Enables library users to implement custom ISpecimenBuilder without code duplication
- Removed unnecessary using directives from downstream files

### TypeCustomization<T> Unsealed
- Removed sealed modifier to enable subclassing
- Enables JsonElementCustomization simplification
- XML documentation already promised this pattern

### JsonElementCustomization Simplified
- Replaced ICustomization + private JsonElementBuilder : ISpecimenBuilder with direct inheritance
- Now: public sealed class JsonElementCustomization : TypeCustomization<JsonElement>
- Creation logic is a single constructor lambda
- Private nested class eliminated

### FixtureFactory.ApplyCustomizations Helper
- Added private static void ApplyCustomizations(IFixture fixture, IEnumerable<ICustomization> customizations)
- Removed loop duplication from Create(params ICustomization[]) and Create(MethodInfo)

Build: 0 errors, 0 warnings. Tests: 111/111 passing.
