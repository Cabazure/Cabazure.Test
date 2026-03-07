# Squad Decisions

## Active Decisions

### 1. Private Field Naming Convention — camelCase, No Prefix

**Proposed by:** Kaylee (Core Dev)  
**Date:** 2026-03-07  
**Status:** Approved

Private instance fields and private static fields in all C# source files use plain **camelCase** with **no prefix** (no underscore `_`, no `s_`).

**Rationale:**
- Aligns with the Cabazure sibling repos (e.g., `Cabazure.Client`) for a consistent codebase style across the organisation.
- The `.editorconfig` naming rules (`private_fields_should_be_camelcase`, `private_static_fields_should_be_camelcase`) enforce this at editor/analyzer level.
- `_` prefix is a Visual Studio default but not a .NET Runtime or BCL convention; camelCase is the BCL-preferred style per the .NET design guidelines for private members.
- Avoids accidental shadowing confusion between parameter names and field names — reviewers should rely on `this.` qualification if disambiguation is ever needed (which is rare in our codebase).

**Special Case:**
- `lock` is a reserved C# keyword; the sync-lock object in `SutFixtureCustomizations` is therefore named `syncLock` (not `lock`). This is not an exception to the rule — it is the correct camelCase name when the word "lock" conflicts with the language.

**Affected Files (at time of decision):**
- `src/Cabazure.Test/Fixture/SutFixture.cs` — `_fixture` → `fixture`
- `src/Cabazure.Test/Customizations/SutFixtureCustomizations.cs` — `_customizations` → `customizations`, `_lock` → `syncLock`

---

### 2. SutFixture Core Implementation Design

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Approved

#### 2a. Two `Freeze<T>` Overloads

**Decision:** Provide both `Freeze<T>()` and `Freeze<T>(instance)`.

**Rationale:**
- `Freeze<T>()`: Delegates to AutoFixture's `Freeze<T>()` — creates and registers in one call
- `Freeze<T>(instance)`: Uses `Inject(instance)` to register a user-provided instance
- This mirrors the flexibility of raw AutoFixture while maintaining a clean API
- Common pattern: create a configured substitute, then freeze it for later injection

#### 2b. `Substitute<T>()` Does NOT Auto-Register

**Decision:** `Substitute<T>()` creates a substitute but does NOT call `Freeze` internally.

**Rationale:**
- Separation of concerns: creation vs registration
- Allows test-specific one-off mocks without polluting the fixture
- If auto-registration is desired, users can chain: `fixture.Freeze(fixture.Substitute<IFoo>())`
- More predictable behavior — explicit registration is clearer

#### 2c. Constructor Accepts `params ICustomization[]`

**Decision:** Allow users to pass custom AutoFixture customizations to the constructor.

**Rationale:**
- Extensibility: teams may have domain-specific specimen builders or conventions
- Defaults to `AutoNSubstituteCustomization`, but doesn't force it
- Example use case: custom `DateTime` generation, specific string formats, or domain value objects
- Follows AutoFixture's design philosophy of composable customizations

#### 2d. xUnit 3 Attribute Uses Reflection for Freezing

**Decision:** `AutoNSubstituteDataAttribute` uses reflection to call generic `Freeze<T>(instance)` method.

**Rationale:**
- Test method parameters are `ParameterInfo[]` with runtime `Type`, not compile-time generics
- Reflection via `MakeGenericMethod` is the only way to invoke `Freeze<T>` with a runtime type
- Performance impact is negligible (once per test method invocation)
- Edge case handling: find the overload with 1 generic parameter (not the parameterless one)

#### 2e. Left-to-Right Freeze Semantics

**Decision:** Parameters marked `[Frozen]` are frozen AFTER creation, so subsequent parameters in the SAME test method get the frozen instance.

**Rationale:**
- Intuitive for users: "freeze this, then use it later in the parameter list"
- Matches AutoFixture.Xunit2 behavior (de facto standard)
- Example: `Test([Frozen] IFoo foo, MyClass sut)` — `sut` constructor gets the same `foo`

#### Implementation Notes

- `AutoNSubstituteCustomization` wraps the AutoFixture.AutoNSubstitute package's customization with `ConfigureMembers=true` and `GenerateDelegates=true` for maximum auto-mocking coverage
- `AssemblyInitializer` uses `[ModuleInitializer]` (C# 9+) to run before xUnit 3 test discovery
- All public APIs include XML doc comments

---

### 3. Phase 7 — User-Defined Fixture Customizations

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Approved

Every `SutFixture` created by the data attributes now uses `AutoNSubstituteDataHelper.CreateFixture(testMethod)` which builds the fixture in strict priority order:

#### 3a. Customization Layering Order (Critical)

1. **`AutoNSubstituteCustomization`** — always first; NSubstitute is the non-negotiable foundation.
2. **`SutFixtureCustomizations.All`** — project-wide registrations, registered once via `[ModuleInitializer]`.
3. **`[CustomizeWith]` on the test method** — method-level overrides.
4. **`[CustomizeWith]` on the declaring class** — class-level defaults; applied after method-level.

> **Note on ordering:** AutoFixture's customization pipeline is last-writer-wins for the same type. Placing class-level `[CustomizeWith]` attributes after method-level means class-level overrides method-level for the same type. This is intentional: class attributes declare the "house rules" that always apply. If a different ordering is ever desired, it is a breaking change requiring a new decision.

#### 3b. `SutFixtureCustomizations` Design

- **No `Clear()` / `Reset()`** — omitted intentionally. A global registry that can be cleared mid-run would produce non-deterministic tests. If isolation is needed, use `[CustomizeWith]` at the method level.
- **Thread-safe via `lock`** — `Add` and `All` both lock on a private object. `All` returns a snapshot (`[.._customizations]`) so callers cannot mutate the shared list.
- **`All` is `internal`** — consumers interact only through `Add`; the framework reads the list. This preserves the ability to change the internal representation.

#### 3c. `CustomizeWithAttribute` Design

- `AllowMultiple = true` — multiple customizations can be stacked on a single method or class; they are applied in declaration order.
- Validation happens at `Instantiate()` call time (test discovery / execution), not at attribute construction time (compile time). This is consistent with how xUnit handles data attributes.
- Validation produces `InvalidOperationException` with a diagnostic message that names the offending type, making misconfiguration easy to diagnose.

#### Consequences

- All four data attributes (`AutoNSubstituteData`, `InlineAutoNSubstituteData`, `MemberAutoNSubstituteData`, `ClassAutoNSubstituteData`) now participate in the customization stack automatically.
- Per-row fixture creation in `Member` and `Class` variants is preserved — each row gets its own fully-customized fixture, preventing cross-row state leakage.
- The `SutFixture(params ICustomization[])` constructor is the integration point; the parameterless constructor is now only used by code that wants the default (NSubstitute-only) configuration.

---

### 4. Project Structure and Build Configuration

**Date:** 2026-03-07  
**Author:** Wash (Integration Dev)  
**Status:** Approved

#### 4a. Solution Structure

- **Solution File:** `Cabazure.Test.slnx` at repo root
- **Main Library:** `src/Cabazure.Test/Cabazure.Test.csproj`
- **Test Project:** `tests/Cabazure.Test.Tests/Cabazure.Test.Tests.csproj`
- **Directory Organization:**
  - `src/Cabazure.Test/Attributes/` — xUnit data attributes
  - `src/Cabazure.Test/Customizations/` — AutoFixture customizations
  - `src/Cabazure.Test/Fixture/` — Fixture and builder types

#### 4b. xUnit 3 Package References (Library)

- `xunit.v3.extensibility.core` version 3.2.2
- `xunit.v3.assert` version 3.2.2

**NOT** `xunit.v3` — that's for test projects and requires `OutputType=Exe`.

#### 4c. Supporting Packages (Library)

- `AutoFixture` version 4.18.1
- `AutoFixture.AutoNSubstitute` version 4.18.1
- `NSubstitute` version 5.3.0
- `FluentAssertions` version 7.0.0

#### 4d. Test Project Packages

- `xunit.v3` version 3.2.2 (full test framework)
- `xunit.runner.visualstudio` version 3.1.5
- `Microsoft.NET.Test.Sdk` version 17.12.0
- `coverlet.collector` version 6.0.4
- Project reference to `Cabazure.Test`

#### 4e. Project Settings

- TargetFramework: `net9.0`
- LangVersion: `latest`
- Nullable: `enable`
- ImplicitUsings: `enable`

#### Rationale

1. **xUnit 3 Package Split:** The xUnit team split v3 into focused packages. Libraries extending xUnit should use `extensibility.core` to avoid the test runner overhead and OutputType requirements.
2. **Version Locking:** All package versions are locked explicitly to ensure reproducible builds and avoid surprise breaking changes.
3. **Directory Structure:** Separates concerns — Attributes (xUnit integration), Customizations (AutoFixture), Fixture (core test builders).

---

### 5. Release Pipeline Pattern

**Date:** 2026-03-07  
**Author:** Wash (Integration Dev)  
**Status:** Proposed

- **ci.yml:** push/PR to main → build + test + coverage badges
- **release.yml:** v*.*.* tag (must be on main) → build + test + pack + NuGet publish
- **release-preview.yml:** v*.*.*-previewN tag → build + pack + NuGet publish (no main guard)
- **Version Flow:** tag → VERSION env var → `-p:Version=${VERSION}` on build+pack
- **Prerequisite:** NUGET_KEY secret required in repo settings before first release

---

### 6. Phase 7 Test Design & Validation (Zoe)

**Date:** 2026-03-07  
**Author:** Zoe (QA Lead)  
**Status:** Approved

#### 6a. InternalsVisibleTo for Direct Testing

**Decision:** Created `src/Cabazure.Test/AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("Cabazure.Test.Tests")]`.

**Rationale:**
- `SutFixtureCustomizations.All` and `CustomizeWithAttribute.Instantiate()` are both `internal`
- Prior tests worked around this via integration (attribute behaviour) and reflection
- Direct testing is more precise — an indirect path can pass vacuously if internal implementation changes
- Standard practice for library test projects

#### 6b. `ProjectWideValue` Must Be `public`

**Decision:** `ProjectWideValue` (test domain value) is declared `public record`.

**Rationale:**
- Used as a `[Theory]` parameter in `SutFixtureCustomizationsTests.ProjectWideCustomization_IsApplied_WhenAutoNSubstituteDataUsed`
- xUnit requires theory parameters to have consistent visibility with the test method
- Lives in test assembly, so no public leakage concern

#### 6c. `CountTestCustomization` Is Private and Nested

**Decision:** `CountTestCustomization` is a `private sealed class` nested inside `SutFixtureCustomizationsTests`.

**Rationale:**
- Used to test "count before → add 2 → assert count + 2" pattern
- Prevents pollution of global registry with recognizable types
- Other tests cannot observe or reference it

#### Observations

- Static registry is append-only and never cleared — ordering risk if multiple assemblies register conflicting customizations for the same concrete type
- Current mitigation: use uniquely-scoped nested records inside test classes to prevent cross-test contamination
- Future recommendation: Consider documenting in XML doc that callers must scope registrations to private types

---

### 7. SutFixtureCustomizations → FixtureFactory.Customizations Refactor

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Approved

#### 7a. Consolidation Rationale

**Decision:** Merged `SutFixtureCustomizations` static class into `FixtureFactory.Customizations` (type: `FixtureCustomizationCollection`).

**Rationale:**
- `SutFixtureCustomizations` was a standalone global registry class with no connection to `FixtureFactory`
- Better discoverability: users find the customization API through `FixtureFactory.Customizations` rather than a separate class
- `SutFixtureCustomizations` was a holdover name from the removed `SutFixture` class; it no longer reflected the architecture
- Richer API: `FixtureCustomizationCollection` supports `Add`, `Remove(instance)`, `Remove<T>()`, `Clear`, `Count`, and implements `IEnumerable<ICustomization>`

#### 7b. Implementation Details

- `FixtureFactory.Customizations` is a static property returning `FixtureCustomizationCollection`
- Pre-seeded with `AutoNSubstituteCustomization` (non-negotiable foundation)
- Thread-safe via internal locking (same pattern as previous `SutFixtureCustomizations`)
- `All` property returns a snapshot to prevent external mutation of the shared list

#### Consequences

- **Breaking change for direct consumers:** Code that accessed `SutFixtureCustomizations.Add` must now use `FixtureFactory.Customizations.Add`.
- All internal and test references have been updated.
- The `[ModuleInitializer]` pattern for project-wide registrations remains unchanged; callers simply use the new API.

---

### 8. Phase 8 — FixtureFactory API Design

**Proposed by:** Kaylee (Core .NET Developer)  
**Date:** 2026-03-07  
**Status:** Proposed

**Note:** See Decision 7 for the consolidation that moved customization registry into `FixtureFactory.Customizations`.

#### 8a. Introduce `FixtureFactory` as the public entry point

**Decision:** Replace `SutFixture` with a `public static class FixtureFactory` in the root `Cabazure.Test` namespace.

**Rationale:**
- `IFixture` is a well-known, fully-featured API; wrapping it in `SutFixture` provided no additional functionality.
- Returning `IFixture` directly gives consumers the full AutoFixture surface without a proxy layer.
- A static factory is the idiomatic pattern for returning configured instances without exposing construction complexity.

#### 8b. Three overloads — two public, one internal

| Overload | Access | Purpose |
|---|---|---|
| `Create()` | public | No-arg convenience; delegates to `Create([])`. |
| `Create(params ICustomization[])` | public | Applies `AutoNSubstituteCustomization` first, then each supplied customization. |
| `Create(MethodInfo)` | internal | Full priority stack for theory data attributes. |

**Rationale:**
- The `MethodInfo` overload is an implementation detail of the attribute pipeline; making it public would expose an internal contract to consumers.
- The two public overloads are sufficient for direct `[Fact]` usage.

#### 8c. Eliminate reflection from `AutoNSubstituteDataHelper`

**Decision:** Use AutoFixture kernel APIs directly.

- `CreateValue` → `new SpecimenContext(fixture).Resolve(type)`
- `FreezeValue` → `fixture.Customizations.Insert(0, SpecimenBuilderNodeFactory.CreateTypedNode(type, new FixedBuilder(value)))`

**Rationale:**
- `MakeGenericMethod` was the only reason `SutFixture` existed as a concrete type in the helper.
- Kernel APIs are the canonical, non-reflective way to perform these operations; they are already what AutoFixture's own extension methods delegate to.
- Removes a runtime failure mode (reflection errors only surfaced at test execution time).

#### 8d. Delete `SutFixture` and the `Fixture/` subdirectory

**Decision:** `SutFixture.cs` is deleted; `AssemblyInitializer.cs` is moved to the project root (`Cabazure.Test` namespace) and the `Fixture/` directory is removed entirely.

**Rationale:**
- The `Fixture/` directory only existed to house `SutFixture`; `AssemblyInitializer` was placed there historically but belongs at the project root since it is a library-level concern.
- Removing the directory reduces navigational friction and eliminates a namespace that served no grouping purpose.

#### Consequences

- **Breaking change for consumers:** Any code that directly instantiates `SutFixture` must be updated to use `FixtureFactory.Create()` or `FixtureFactory.Create(customizations)` instead. The returned `IFixture` provides a superset of the functionality `SutFixture` exposed.
- `AutoNSubstituteDataAttribute` and its three variants are unchanged from a user perspective; the fixture they inject into theory parameters is still configured identically.
- `FixtureFactory.Customizations.Add` and `[CustomizeWith]` continue to work without modification.

---

### 9. Immutable Collection Support — ImmutableCollectionCustomization

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)  
**Status:** Approved

**Decision:** `ImmutableCollectionCustomization` is included in the library as a `public sealed class` that provides full support for all eight System.Collections.Immutable types.

**Supported Types:**
- `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableHashSet<T>`, `ImmutableDictionary<TKey, TValue>`
- `ImmutableSortedSet<T>`, `ImmutableSortedDictionary<TKey, TValue>`
- `ImmutableQueue<T>`, `ImmutableStack<T>`

**Rationale:**
- Without this customization, AutoFixture 4.18.1 throws `ObjectCreationException` for the first four types and creates empty collections for the last two.
- The customization handles both constructor parameters and object properties (via `PropertyInfo` and `FieldInfo` support).
- Essential for projects using immutable data structures.

**Consequences:**
- Users opt-in by passing `new ImmutableCollectionCustomization()` to `FixtureFactory.Create()` or via `[CustomizeWith]`.
- The customization is not applied by default; `AutoNSubstituteCustomization` remains the foundation.

---

### 12. All Three Standard Customizations Seeded by Default

**Author:** Kaylee (Core .NET Developer)  
**Date:** 2026-03-07  
**Status:** Approved

**Decision:** All three standard customizations are now seeded by default in `FixtureCustomizationCollection`:

1. `AutoNSubstituteCustomization` — NSubstitute integration (was already present)
2. `RecursionCustomization` — replaces `ThrowingRecursionBehavior` with `OmitOnRecursionBehavior`
3. `ImmutableCollectionCustomization` — full support for all eight `System.Collections.Immutable` types

**Rationale:**
Users of `FixtureFactory.Create()` with no arguments should get a fixture that works correctly for the most common test scenarios out of the box — including recursive object graphs and immutable collection properties. Previously, they had to opt-in to these customizations explicitly, which was a common source of confusion and boilerplate.

**Consequences:**
- `FixtureFactory.Create()` (no-arg) now produces a fixture with all three customizations applied.
- `FixtureCustomizationCollection` default `Count` is now 3 (was 1).
- Users who previously called `FixtureFactory.Customizations.Add(new RecursionCustomization())` or `FixtureFactory.Customizations.Add(new ImmutableCollectionCustomization())` globally may now have duplicates; they should remove those registrations.
- All existing tests pass unchanged (78/78 green).

---

### 10. Recursion Handling — RecursionCustomization

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)  
**Status:** Approved

**Decision:** `RecursionCustomization` is provided as a `public sealed class` following the same style as `AutoNSubstituteCustomization`.

**Implementation:**
- Removes AutoFixture's default `ThrowingRecursionBehavior`
- Installs `OmitOnRecursionBehavior` instead (leaves recursive properties as `null`)

**Rationale:**
- `OmitOnRecursionBehavior` is test-friendly — recursive properties default to `null` rather than throwing
- This is the recommended approach for handling recursive object graphs in tests
- Consistent pattern with other customization classes in this library

**Namespace Pitfall:**
- In the test project, `Fixture` is both a class (`AutoFixture.Fixture`) and a namespace remnant
- When constructing `new Fixture()` directly without going through `FixtureFactory`, use `new AutoFixture.Fixture()` to avoid CS0118 ambiguity

---

### 11. README Synchronization with FixtureFactory Phase 8 API

**Date:** 2026-03-07  
**Author:** Zoe (QA/Docs Lead)  
**Status:** Approved

**Decision:** README.md has been fully rewritten to reflect the library's current public API after Phase 8 (FixtureFactory refactor).

**Changes Made:**
- Replaced defunct `SutFixture` API with `FixtureFactory`
- Fixed broken CI badge (was `build.yml`, corrected to `ci.yml`)
- Added complete sections for all four theory data attributes
- Added sections for `RecursionCustomization` and `ImmutableCollectionCustomization`
- Added section for `FixtureFactory.Customizations` (formerly `SutFixtureCustomizations`) project-wide registration pattern
- Added section for `[CustomizeWith]` method/class-level customization
- Added section for `[Frozen]` parameter freezing with realistic multi-dependency example

**Rationale:**
- README drift is a real risk after refactors; must be treated as a deliverable alongside public API changes
- Previous README still described the now-deleted `SutFixture` class

**Governance Note:**
- README should be kept in sync whenever a public API surface changes
- QA/docs owner (Zoe) will flag README gaps in future task reviews

---

### 10. DateOnly/TimeOnly and JsonElement Customization Defaults

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)  
**Status:** Approved

#### Decision

`DateOnlyTimeOnlyCustomization` is added to the default seed in `FixtureCustomizationCollection`.  
`JsonElementCustomization` is opt-in only (not seeded by default).

#### Rationale

**DateOnlyTimeOnlyCustomization → Default:**
- `DateOnly` and `TimeOnly` are part of the core .NET API surface (introduced .NET 6, now in .NET 9)
- AutoFixture **cannot** create `DateOnly` without a customization (throws `ArgumentOutOfRangeException`)
- `TimeOnly` technically works but produces useless values (ticks ≈ 0, always midnight)
- These types are common in modern .NET codebases (date-only fields, time-of-day properties)
- Adding this to defaults aligns with `RecursionCustomization` and `ImmutableCollectionCustomization` — all three fix AutoFixture gaps that affect broad categories of types

**JsonElementCustomization → Opt-In:**
- `JsonElement` is a specialized type from `System.Text.Json` — not everyone uses it
- Adding it to defaults would add `System.Text.Json` namespace references and processing to every fixture even if the project doesn't use `JsonElement`
- Keeping it opt-in follows the principle of least surprise — users explicitly add it when they need it

#### Alternatives Considered

1. **Make both opt-in:** Rejected — `DateOnly`/`TimeOnly` are core .NET types, not optional like `JsonElement`
2. **Make both default:** Rejected — pollutes fixtures for projects that don't use `JsonElement`

#### Implementation

- `FixtureCustomizationCollection` constructor now seeds four customizations: `AutoNSubstituteCustomization`, `RecursionCustomization`, `ImmutableCollectionCustomization`, `DateOnlyTimeOnlyCustomization`
- `JsonElementCustomization` is documented in README with opt-in instructions
- Users can remove `DateOnlyTimeOnlyCustomization` via `FixtureFactory.Customizations.Remove<DateOnlyTimeOnlyCustomization>()` if needed

---

### 11. Test Coverage for JsonElement and DateOnly/TimeOnly Customizations

**Proposed by:** Zoe (QA & Testing Lead)  
**Date:** 2026-03-07  
**Status:** Approved

#### Decision

Test coverage for type-specific AutoFixture customizations should verify:

1. **Null guard** — `Customize(null!)` throws `ArgumentNullException`
2. **Type-specific behavior** — Created value meets type semantics (e.g., `JsonElement.ValueKind == Object`, `DateOnly` is not `MinValue`)
3. **Non-trivial randomness** — Generated values are meaningfully populated (e.g., JsonElement has properties, TimeOnly has non-zero ticks)

---

## Phase 10: Readability Refactoring & DRY Elimination

### 12. SpecimenRequestHelper Extracted as Public API

**Proposed by:** Kaylee (Core Developer)  
**Date:** 2026-03-07  
**Status:** Approved

#### Decision

Extract the repeated `GetRequestType(object request)` switch logic from four private copies into a **single public static class `SpecimenRequestHelper`** in `Cabazure.Test.Customizations`:

```csharp
public static class SpecimenRequestHelper
{
    public static Type? GetRequestType(object request) => request switch
    {
        ParameterInfo pi => pi.ParameterType,
        PropertyInfo pr => pr.PropertyType,
        FieldInfo fi => fi.FieldType,
        Type t => t,
        _ => null,
    };
}
```

#### Rationale

1. **DRY**: Eliminates copy-paste from `TypeCustomization<T>.DelegateBuilder`, `DateTimeOnlyBuilder`, `ImmutableCollectionBuilder`, and the old `JsonElementBuilder`
2. **Public API value**: Library users implementing custom `ISpecimenBuilder` can reuse the pattern without reimplementation
3. **Reflection module cleanup**: Removes `using System.Reflection;` from three downstream files; types now referenced only inside SpecimenRequestHelper

#### Related Changes

- **TypeCustomization<T> unsealed**: Enables JsonElementCustomization to subclass instead of wrapping
- **JsonElementCustomization simplified**: Now `public sealed class JsonElementCustomization : TypeCustomization<JsonElement>`, eliminating the private nested `JsonElementBuilder` class

### 13. TypeCustomization<T> Unsealed for Composition Patterns

**Proposed by:** Kaylee  
**Date:** 2026-03-07  
**Status:** Approved

#### Decision

Remove the `sealed` modifier from `TypeCustomization<T>` to allow direct subclassing.

#### Rationale

1. **Documentation integrity**: XML `<example>` block already documented the subclassing pattern (now realizable)
2. **JsonElementCustomization simplification**: Allows `JsonElementCustomization : TypeCustomization<JsonElement>` instead of wrapping with a nested builder
3. **Composition over inheritance**: Sealed constraint was not load-bearing; unsealing enables a cleaner, more direct design

### 14. JsonElementCustomization Simplified via TypeCustomization Inheritance

**Proposed by:** Kaylee  
**Date:** 2026-03-07  
**Status:** Approved

#### Decision

Replace `JsonElementCustomization : ICustomization` with private nested `JsonElementBuilder : ISpecimenBuilder` pattern with direct inheritance: `public sealed class JsonElementCustomization : TypeCustomization<JsonElement>`.

Creation logic becomes a single constructor lambda:
```csharp
public sealed class JsonElementCustomization : TypeCustomization<JsonElement>
{
    public JsonElementCustomization() : base(fixture => fixture.Create<JsonElement>()) { }
}
```

#### Rationale

1. **Code clarity**: Entire creation logic is now visible at a glance (single lambda vs nested class + method)
2. **DRY**: Eliminates the private `JsonElementBuilder` class entirely
3. **Consistency**: Uses the same factory pattern as other customizations
4. **Reduced scope**: Removes unnecessary `using System.Reflection;` and `using AutoFixture.Kernel;`

### 15. FixtureFactory.ApplyCustomizations Helper Extracted

**Proposed by:** Kaylee  
**Date:** 2026-03-07  
**Status:** Approved

#### Decision

Extract a private helper method `ApplyCustomizations(IFixture fixture, IEnumerable<ICustomization> customizations)` in `FixtureFactory` to consolidate two identical `foreach` loops:

```csharp
private static void ApplyCustomizations(IFixture fixture, IEnumerable<ICustomization> customizations)
{
    foreach (var customization in customizations)
        fixture.Customize(customization);
}
```

Replace both occurrences in `Create(params ICustomization[])` and `Create(MethodInfo)` with calls to this helper.

#### Rationale

1. **DRY**: Two identical loops → one helper
2. **Clarity**: Reduces visual noise; intent is explicit via method name
3. **Testability**: Easier to verify behavior in isolation

### 16. SpecimenRequestHelper Edge Cases and Recommendations

**Proposed by:** Zoe (QA Lead)  
**Date:** 2026-03-07  
**Status:** Documented (no action required for Phase 10 merge)

#### Observations

1. **Null input handling**: `GetRequestType(object request)` accepts non-nullable `object`, but unsafe code or reflection could pass `null!`. Current switch throws `NullReferenceException`. Recommendation: add null-guard or accept nullable input with null pattern arm.

2. **MemberInfo subtypes**: `MethodInfo`, `EventInfo`, `ConstructorInfo` (subclasses of `MemberInfo`) fall through to `_ => null`. Probably intentional; document if so.

3. **CS0649 warning**: Test's `TestSubject.SomeField` (never assigned) is harmless — accessed only via reflection. No action needed unless build treats warnings as errors.

4. **ParameterInfo source**: Tests source `ParameterInfo` from constructors, consistent with AutoFixture's real customization pipeline behavior.
4. **Integration with FixtureFactory** — Customization works via `FixtureFactory.Create(customization)` or `FixtureFactory.Create()` (if in defaults)
5. **Property-on-object scenario** — Fixture can create an object that has the customized type as a property

#### Rationale

**JsonElementCustomization specific:**
- `JsonElement` is a struct wrapper around `JsonDocument` — must verify the element is cloned and survives GC
- Used `GC.Collect()` + `GC.WaitForPendingFinalizers()` to test clone independence
- Verified `ValueKind` and property enumeration to ensure meaningful content

**DateOnly/TimeOnly specific:**
- FluentAssertions 7.0 does not provide comparison operators for `DateOnly`/`TimeOnly` types
- Used workarounds: `result.Year.Should().BeGreaterThan(1)` or `NotBe(MinValue)` patterns
- Multiple-value randomness check: create 3 values, assert at least one differs from `MinValue`

**General pattern:**
- Followed `ImmutableCollectionCustomizationTests.cs` structure for consistency
- Nested test classes (`HasJsonElementProperty`, `HasDateTimeOnlyProperties`) keep helper types scoped
- Clear test names describe the scenario completely (e.g., `Create_JsonElement_IsClonedAndStandalone`)

#### Impact

- **Coverage:** 13 new test methods across 2 files
- **Build:** Tests compile successfully (verified with `dotnet build`)
- **Execution:** All tests passing (91/91)

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

## kaylee-type-customization

# Decision: TypeCustomization<T> Factory Pattern

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Implemented

## Context

Phase 9 required implementing a generic customization class that allows users to register factory functions for specific types. The key design question: should the factory receive `IFixture` (high-level API) or `ISpecimenContext` (low-level kernel API)?

## Decision

**The factory receives `IFixture`, not `ISpecimenContext`.**

```csharp
public sealed class TypeCustomization<T> : ICustomization
{
    private readonly Func<IFixture, T> factory;
    
    public TypeCustomization(Func<IFixture, T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
    }
    
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Add(new DelegateBuilder(fixture, factory));
    }
    
    // DelegateBuilder stores the IFixture instance and passes it to the factory
}
```

## Rationale

1. **Ergonomics:** Users can write `f => DateOnly.FromDateTime(f.Create<DateTime>())` instead of wrestling with `context.Resolve(typeof(DateTime))` and casting.

2. **Consistency:** All existing examples in the codebase use `IFixture` methods like `Create<T>()`, `Build<T>()`, `Freeze<T>()` — this keeps the same mental model.

3. **Discoverability:** IDE autocomplete on `IFixture` exposes the full AutoFixture API; `ISpecimenContext` only shows `Resolve(object)` which is opaque to new users.

4. **Safety:** The `IFixture` instance is captured during `Customize()`, ensuring the factory always operates on the correct fixture with all customizations applied.

## Alternatives Considered

**Option A: Factory receives `ISpecimenContext`**  
- Pro: Matches AutoFixture kernel conventions  
- Con: Requires users to know `context.Resolve(typeof(T))` and cast results  
- Con: Loses access to fluent API like `Build<T>().With(...).Create()`  
- **Rejected:** Too low-level for typical user needs

**Option B: Factory receives both `IFixture` and `ISpecimenContext`**  
- Pro: Maximum flexibility  
- Con: Signature bloat; confuses users about which to use  
- **Rejected:** Over-engineered for 99% of use cases

## Impact

- **FixtureCustomizationCollection:** Two new overloads added:
  - `Add<T>(Func<IFixture, T> factory)` — inline factory registration
  - `Add(ISpecimenBuilder builder)` — power-user escape hatch for full control
  
- **Power users:** Can still implement `ISpecimenBuilder` directly and register via the new `Add(ISpecimenBuilder)` overload if they need `ISpecimenContext` access.

## Examples

### Inline factory (simple)
```csharp
FixtureFactory.Customizations.Add<DateOnly>(f => 
    DateOnly.FromDateTime(f.Create<DateTime>()));
```

### Subclassed customization (reusable)
```csharp
public sealed class JsonElementCustomization : TypeCustomization<JsonElement>
{
    public JsonElementCustomization()
        : base(f =>
        {
            var json = $"{{\"id\":\"{f.Create<Guid>()}\"}}";
            return JsonDocument.Parse(json).RootElement.Clone();
        })
    {
    }
}
```

### Power-user escape hatch
```csharp
public sealed class MyAdvancedBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        // Full kernel control with ISpecimenContext
    }
}

FixtureFactory.Customizations.Add(new MyAdvancedBuilder());
```

## Verification

Build passed with 0 errors, 0 warnings in Release mode (TreatWarningsAsErrors=true).


## zoe-type-customization-tests

# Decision: TypeCustomization<T> Test Patterns

**Date:** 2026-03-07  
**Author:** Zoe (QA Lead)  
**Status:** Implemented

## Context

Created comprehensive test coverage for `TypeCustomization<T>` and the two new `FixtureCustomizationCollection.Add()` overloads introduced in Phase 9. Discovered several test patterns unique to this generic, sealed customization type.

## Decisions Made

### 1. Avoid Namespace Collision with `new AutoFixture.Fixture()`

**Problem:** Test namespace includes `Cabazure.Test.Tests.Fixture` which collides with `AutoFixture.Fixture` type.

**Solution:** Use fully qualified `new AutoFixture.Fixture()` in test code when creating bare fixtures for isolated tests.

**Rationale:** Clear, unambiguous, and avoids need for namespace aliases.

### 2. Test Wrapping Pattern Instead of Subclassing

**Problem:** `TypeCustomization<T>` is `sealed`, so subclassing tests are not possible.

**Solution:** Created `TypeCustomization_CanBeWrappedInCustomClass()` test demonstrating composition via `ICustomization` wrapper.

**Rationale:** Documents the intended reuse pattern — composition over inheritance aligns with the sealed design.

### 3. Fix Ambiguous `Add(null!)` Calls

**Problem:** With 3 overloads of `Add()` (`ICustomization`, `Func<IFixture, T>`, `ISpecimenBuilder`), `null!` parameter is ambiguous.

**Solution:** Cast to the intended overload: `Add((ICustomization)null!)` in null-guard tests.

**Impact:** Updated existing test in `FixtureCustomizationCollectionTests.cs` to fix compilation error.

### 4. Use Local Fixtures to Avoid Global State Pollution

**Pattern:** For tests of `TypeCustomization<T>` itself, create a bare `new AutoFixture.Fixture()` and call `sut.Customize(fixture)` directly.

**Rationale:** Avoids modifying `FixtureFactory.Customizations` global registry in tests, preventing cross-test side effects.

**Alternative:** For tests of the convenience method `FixtureCustomizationCollection.Add<T>(factory)`, create a local `new FixtureCustomizationCollection()` instance.

### 5. Verify Factory Receives Working IFixture

**Test:** `Factory_ReceivesIFixture_WithWorkingCreate()` — factory lambda calls `f.Create<string>()` and verifies result is non-null.

**Rationale:** Ensures the `IFixture` passed to the factory is fully functional and not a stub/mock.

### 6. Test Constructor Parameter Interception

**Test:** `Create_UsesFactory_ForConstructorParameter()` — verify that `TypeCustomization<int>` intercepts `int` constructor parameters.

**Rationale:** Demonstrates that factory-based customization applies to all specimen requests, not just direct `Create<T>()` calls.

## Coverage Summary

**15 tests total:**
- 6 core `TypeCustomization<T>` tests (constructor, customize, factory behavior)
- 1 wrapping pattern test (composition over inheritance)
- 4 `FixtureCustomizationCollection` overload tests (factory + specimen builder)
- 4 integration tests (Build<T>, constructor params, multiple calls, isolation)

## Related Files

- `tests/Cabazure.Test.Tests/Customizations/TypeCustomizationTests.cs`
- `tests/Cabazure.Test.Tests/Customizations/FixtureCustomizationCollectionTests.cs` (1 line fix for ambiguous null)

## Test Results

✅ **106 tests passing** (15 new + 91 existing)  
✅ **0 errors, 0 warnings**




# Decision: CancellationToken Customization Strategy

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Implemented

## Context

AutoFixture produces an already-cancelled `CancellationToken` by default because it resolves the `bool` constructor parameter as `true` (the dominant value for bool). This creates a silent test failure mode: any SUT that checks `cancellationToken.IsCancellationRequested` at entry will exit early, causing tests to pass while never executing the real logic.

## Decision

Created `CancellationTokenCustomization` that returns `new CancellationToken(false)` (equivalent to `CancellationToken.None`) and registered it as the fifth default customization in `FixtureCustomizationCollection`.

### Why `new CancellationToken(false)` instead of a live token?

A live token backed by `CancellationTokenSource` is **not serializable** in xUnit 3. Using one would:
- Break `SupportsDiscoveryEnumeration()` on data attributes
- Force xUnit to fall back to `XunitDelayEnumeratedTheoryTestCase`
- Lose individual test case pre-discovery in Test Explorer

`new CancellationToken(false)` is serializable and preserves discovery while providing a safe default.

## Alternatives Considered

1. **Live token from CancellationTokenSource**: Not serializable, breaks xUnit 3 discovery
2. **Leave AutoFixture default**: Creates silent test failures — unacceptable
3. **No default, force explicit creation**: Violates "pit of success" design principle

## For Tests That Need Cancellation Behavior

- **Test-scoped cancellation:** Create `CancellationTokenSource` directly in test body, pass `cts.Token` to SUT
- **Runner-scoped cancellation:** Use `TestContext.Current.CancellationToken` (xUnit 3) — cancelled if test run is aborted

## Opt-Out Path

Remove via `FixtureFactory.Customizations.Remove<CancellationTokenCustomization>()` from a `[ModuleInitializer]`.

## Related Files

- `src/Cabazure.Test/Customizations/CancellationTokenCustomization.cs`
- `src/Cabazure.Test/FixtureCustomizationCollection.cs`

## Key Insight

This exposed a general AutoFixture footgun: types with bool constructor parameters are at risk when `true` represents "already done/cancelled/finished" states. The dominant-value heuristic creates invalid test data. Watch list: any type where a bool parameter gates behavior that should normally be active during tests.


# Decision: CancellationToken Customization Documentation

**Date:** 2026-03-XX  
**Architect:** Mal  
**Status:** Documented

## Context

Kaylee is implementing `CancellationTokenCustomization` as a default customization to fix AutoFixture's problematic default behavior of producing already-cancelled tokens. This required documentation updates to README.md and `.github/copilot-instructions.md` to explain the feature and establish patterns for future development.

## Decision

### 1. CancellationToken Handling via Customization

- `CancellationTokenCustomization` is included **by default** in `FixtureFactory`
- It provides `new CancellationToken(false)` — a non-cancelled, non-cancellable token
- This replaces AutoFixture's default already-cancelled token
- **The customization handles all CancellationToken creation; data attributes do not**.

### 2. Three Usage Patterns for Developers

**Runner-scoped cancellation** (xUnit 3 idiomatic):
```csharp
[Theory, AutoNSubstituteData]
public void MyTest(CancellationToken token)
{
    // For runner-scoped cancellation, use TestContext.Current.CancellationToken instead
    var runnerToken = TestContext.Current.CancellationToken;
}
```

**Per-test cancellation** (e.g., testing timeout/cancellation handling):
```csharp
[Theory, AutoNSubstituteData]
public void MyTest(CancellationToken token)
{
    var cts = new CancellationTokenSource();
    // Use cts.Token for controlled cancellation testing
    cts.Cancel();
}
```

**Opt-out**:
```csharp
[ModuleInitializer]
public static void Initialize()
    => FixtureFactory.Customizations.Remove<CancellationTokenCustomization>();
```

### 3. SupportsDiscoveryEnumeration = true Requirement

All custom data attributes must return `SupportsDiscoveryEnumeration = true` because:
- Live `CancellationToken` instances (from `CancellationTokenSource`) are **not serializable**
- xUnit 3 test discovery tries to serialize test case parameters
- Non-serializable tokens would break discovery and cause test enumeration to fail
- Our standard data attributes (`[AutoNSubstituteData]`, etc.) handle this correctly

**Implications for future data attribute implementations:**
- Always return `true` from `SupportsDiscoveryEnumeration`
- Never attempt to serialize live `CancellationToken` instances to test case discovery

### 4. Documentation in README

- Added dedicated `CancellationTokenCustomization` section under Customizations
- Positioned between `ImmutableCollectionCustomization` and `DateOnlyTimeOnlyCustomization`
- Explains problem (AutoFixture's default), solution, and all three usage patterns
- Updated Features table to include the customization

### 5. Documentation in copilot-instructions.md

- **Commit Messages:** Added explicit mention of "focused conventional commits" rule
- **Customizations:** Added `CancellationTokenCustomization` with usage guidance
- **Data Attributes:** Added critical note about `SupportsDiscoveryEnumeration = true` requirement and the reason (serialization of live tokens during discovery)

## Rationale

- **Customization-based approach** keeps CancellationToken handling orthogonal to data attribute logic
- **TestContext.Current.CancellationToken** aligns with xUnit 3's design for runner-scoped resources
- **SupportsDiscoveryEnumeration note** prevents future bugs from non-serializable tokens breaking discovery
- **Documentation-first** ensures developers understand idiomatic patterns before implementing variations
- **Clear examples** show practical patterns for the three common scenarios

## Consequence

Future Cabazure.Test developers will:
- Understand why CancellationToken parameters work correctly in theory tests
- Know how to use runner-scoped vs. per-test cancellation tokens
- Understand the discovery enumeration constraint when adding custom data attributes
- Follow focused conventional commits as a project pattern


---

## Decision: IFixture/Fixture Parameter Injection in Theory Methods

### 2026-03-07 18.32: IFixture/Fixture parameter injection
**By:** Ricky Kaare Engelharth  
**What:** Theory parameters of type IFixture or Fixture receive the live fixture instance from MergeValues rather than a fixture-created value. Check: parameter.ParameterType.IsAssignableFrom(typeof(Fixture)).  
**Why:** Allows test authors to access the fixture directly in theory parameters for advanced setup.

**Implementation:** Branch inserted in AutoNSubstituteDataHelper.MergeValues between the [Frozen] provided-value path and the general CreateValue path. IsAssignableFrom(typeof(Fixture)) handles both IFixture (interface) and Fixture (concrete class) in one check.

**Implications for future work:**
- Any theory parameter whose type satisfies IsAssignableFrom(typeof(Fixture)) will receive the fixture, not a generated specimen
- [Frozen] on an IFixture parameter is a no-op (fixture is injected, not frozen into the specimen container)
- Do not use this pattern for other AutoFixture types — only the live fixture root object is special-cased
