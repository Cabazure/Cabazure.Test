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

### 10. Recursion Handling — RecursionCustomization

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)  
**Status:** Proposed

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
