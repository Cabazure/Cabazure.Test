# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

Cabazure.Test integrates xUnit 3, NSubstitute, AutoFixture, and FluentAssertions. The two headline features are:
1. `SutFixture` — AutoFixture-backed fixture that auto-substitutes unregistered interfaces/abstract classes via NSubstitute.
2. `AutoNSubstituteDataAttribute` — xUnit 3 DataAttribute that provides Theory arguments through SutFixture.

xUnit 3 advantages we're leveraging: module initializers via `[ModuleInitializer]`, improved extensibility points over xUnit 2.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### Phase 9 — Custom Type Registration Documentation

**Date:** 2026-03-XX  
**Task:** Document Kaylee's new `TypeCustomization<T>` and `FixtureCustomizationCollection` overloads in README.

**What was added:**
- `TypeCustomization<T>` — public `ICustomization` wrapping a `Func<IFixture, T>` factory. Can be instantiated directly or subclassed for reusable customizations.
- Two new `Add()` overloads on `FixtureCustomizationCollection`:
  - `Add<T>(Func<IFixture, T> factory)` — simplest path for inline delegates
  - `Add(ISpecimenBuilder builder)` — power-user escape hatch for advanced builders

**Documentation approach:**
- Added a new **"Custom Type Registration"** subsection under the `Customizations` section in README.md
- Structured from simplest (inline delegate) to most advanced (direct `ISpecimenBuilder`)
- Included practical examples: `DateOnly`, `Order` with partial override, `Money` domain type, and builtin override pattern
- Positioned right after `JsonElementCustomization` to naturally extend the customization narrative
- Kept language concise and pragmatic — each example is self-explanatory
- Emphasized the "why" (types AutoFixture can't construct) before diving into "how"

**Key design decision honored:**
- The inline delegate pattern (`Add<T>(factory)`) is featured first as the mental entry point
- Reusable customization classes (subclass `TypeCustomization<T>`) come next as the natural evolution for shared logic
- Direct `ISpecimenBuilder` registration positioned last as the escape hatch, not the default path
- All examples align with existing README style and tone

**No changes made to code — documentation only.** Documentation merged with README updates post-Phase 9.

### Phase 10 — CancellationToken Customization Documentation

**Date:** 2026-03-XX  
**Task:** Document Kaylee's new `CancellationTokenCustomization` and xUnit 3 cancellation patterns in README and copilot-instructions.

**What was documented:**

1. **README.md:**
   - Added `CancellationTokenCustomization` section under Customizations (between Immutable and DateOnlyTimeOnly)
   - Explains default behavior: `new CancellationToken(false)` — fixes AutoFixture's already-cancelled token issue
   - Three usage patterns documented:
     - Runner-scoped: Use `TestContext.Current.CancellationToken` directly (xUnit 3 idiomatic)
     - Per-test: Create `CancellationTokenSource` in test body
     - Opt-out: `FixtureFactory.Customizations.Remove<CancellationTokenCustomization>()`
   - Added feature table entry for `CancellationTokenCustomization`

2. **.github/copilot-instructions.md:**
   - Updated Commit Messages section to explicitly mention **focused conventional commits** (one concern per commit)
   - Added `CancellationTokenCustomization` to Customizations list with usage guidance
   - Added critical note in Data Attributes section: `SupportsDiscoveryEnumeration` must return `true` because live `CancellationToken` instances (from `CancellationTokenSource`) are not serializable during xUnit 3 test discovery

**Key design decisions honored:**
- CancellationToken handling is delegated to `CancellationTokenCustomization`, not to data attributes
- Test context tokens are the idiomatic xUnit 3 pattern for runner-scoped cancellation
- SupportsDiscoveryEnumeration requirement protects against serialization failures during discovery
- Documentation emphasizes practical patterns over framework implementation details

**No code changes — documentation and instruction updates only.**

### Phase 11 Summary (Completed 2026-03-07T17:26:47Z)

**Task:** Provide architectural leadership and documentation for CancellationToken customization completion

**Status:** ✅ Complete

**Documentation Updates:**
- `README.md`: CancellationTokenCustomization section finalized (positioned between ImmutableCollectionCustomization and DateOnlyTimeOnlyCustomization)
- `.github/copilot-instructions.md`: Extended with explicit "focused conventional commits" guidance and critical SupportsDiscoveryEnumeration constraint documentation for future custom data attribute implementation

**Cross-Team Orchestration:**
- Validated Kaylee's implementation against safety + discovery requirements
- Approved Zoe's 5 test cases for comprehensive coverage
- Merged all decision documents into `.squad/decisions.md` for team-wide reference

**Key Learning Documented:**
- AutoFixture's dominant-value heuristic creates silent failures for bool-parameter types where `true` = "already done/cancelled"
- Watch-list: Any API accepting bool that gates "normal" behavior (includes CancellationTokenSource-backed tokens, IDisposable disposed flags, etc.)
- Established pattern: default customizations fix framework gaps; opt-in customizations handle specialized use cases

**Status:** Library feature-complete with best-practice defaults. Pattern documented for future team members.

### xUnit3 and AutoFixture Disposal Behavior Investigation

**Date:** 2026-03-XX

**Key Findings:**

1. **xUnit3 has built-in disposal for theory arguments** via `Xunit.Sdk.DisposalTracker`:
   - Passed to `IDataAttribute.GetData(MethodInfo, DisposalTracker)`
   - Data attributes are expected to call `disposalTracker.AddRange(values)` for any `IDisposable`/`IAsyncDisposable` values
   - Disposed in LIFO order when test case completes via `XunitTestCase.DisposeAsync()`
   - Prefers `IAsyncDisposable` when both interfaces are implemented
   - Swallows individual exceptions and aggregates them

2. **AutoFixture.Xunit3 ignores the `disposalTracker` parameter** — specimens are not registered for disposal

3. **Our implementation also ignores it** — all four `AutoNSubstituteData*` attributes ignore the tracker

4. **Test class instances** are separately disposed by `TestRunner.DisposeTestClass()` if they implement `IDisposable`/`IAsyncDisposable`

5. **xUnit2 had no disposal mechanism** for theory data — this is a xUnit3 improvement

**Conclusion:** No `[Dispose]` attribute needed. Fix is to call `disposalTracker.AddRange(values)` in each attribute's `GetData()` method. One line per attribute.

---

### Phase 15: DisposalTracker Integration Completion (2026-03-07T18:29:07Z)

**Status:** ✅ Complete

**Execution:**
- **Kaylee (Agent-38):** Implemented `disposalTracker.AddRange(values)` in all four `GetData()` methods. Build clean, no regressions.
- **Zoe (Agent-39):** Created `DisposalTrackerIntegrationTests.cs` with 5 comprehensive tests. All 132 tests passing (127 existing + 5 new).

**Outcome:**
All fixture-generated `IDisposable`/`IAsyncDisposable` values now disposed deterministically after each test case via xUnit3's `DisposalTracker`. Substitutes and other managed resources are cleaned up immediately, not deferred to GC collection.

**Decisions Merged:**
- Decision 17: DisposalTracker Integration in GetData()
- Decision 18: DisposalTracker Integration Test Coverage

**Phase Rationale Validated:**
xUnit3's `DisposalTracker` is a first-class framework facility designed precisely for this use case. Using it aligns with xUnit3 conventions and eliminates a subtle resource management gap in the previous implementation.

### Phase 22: Namespace Consolidation (3/3) — Finalized (2026-03-07T19:42:15Z)

**Status:** ✅ Complete — All namespace moves committed

**Team Execution:**
- **Kaylee:** Moved 5 attribute types from `Cabazure.Test.Attributes` → `Cabazure.Test` (AutoNSubstituteDataAttribute family + CustomizeWithAttribute)
- **Wash:** Moved `FixtureCustomizationCollection` from `Cabazure.Test` → `Cabazure.Test.Customizations`, updated all usings in 20+ test files
- **Mal (this phase):** Updated `.github/copilot-instructions.md` to reflect new namespace surface; verified git staging; committed with focused conventional commit message

**Outcome:**
Public API surface simplified. Test authors now need only `using Cabazure.Test;` for all test-facing types:
- Attributes: `AutoNSubstituteDataAttribute`, `InlineAutoNSubstituteDataAttribute`, `MemberAutoNSubstituteDataAttribute`, `ClassAutoNSubstituteDataAttribute`, `CustomizeWithAttribute`
- Customizations: `FixtureCustomizationCollection` (now in dedicated Customizations namespace)

`Cabazure.Test.Attributes` namespace remains but contains only internal types (`FixtureDataExtensions`).

**Documentation Decisions:**
- Removed all references to `Cabazure.Test.Attributes` from copilot-instructions (it's an internal implementation detail now)
- Updated Project Structure section to clarify `Attributes/` contains internal support only
- Updated examples to use only `using Cabazure.Test;`
- Clarified that `FixtureCustomizationCollection` lives in `Cabazure.Test.Customizations`
- Added note that data attributes are "in the `Cabazure.Test` namespace"
- Updated Per-Test Customization section to clarify `[CustomizeWith]` is in `Cabazure.Test`

**Key Design Principle:**
Single entry-point namespace (`Cabazure.Test`) for all public test-facing types reduces friction and confusion. Specialized sub-namespaces (`Customizations`, `Assertions`) are purely organizational for the library's internal structure, not a cognitive burden on users.

**Commit Message:**
Followed Conventional Commits with BREAKING CHANGE footer; scope(s) = `attributes,customizations`; properly attributed to Copilot.
