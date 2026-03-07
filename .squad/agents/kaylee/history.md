# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the guts of the library: AutoFixture customizations, the `ISpecimenBuilder` that routes interface/abstract-class requests to NSubstitute. Key challenge: AutoFixture doesn't natively create substitutes for abstract/interface types; we bridge that via `AutoNSubstituteCustomization`.

### Completed Phases (2026-03-07, Phases 1-12)

**Library Architecture:**
- `AutoNSubstituteCustomization` — routes interface/abstract-class requests to NSubstitute via `ConfigureMembers=true` + `GenerateDelegates=true`
- `FixtureFactory.Create()` — static factory (replaces old SutFixture class); supports custom `ICustomization[]` parameter
- Four data attributes: `AutoNSubstituteData`, `InlineAutoNSubstituteData`, `MemberAutoNSubstituteData`, `ClassAutoNSubstituteData`

**Customizations Completed:**
- `RecursionCustomization` — replaces throwing with omit-on-recursion behavior
- `ImmutableCollectionCustomization` — handles all 8 immutable collection types (List, Array, Set, Queue, Stack, etc.)
- `DateOnlyTimeOnlyCustomization` — generates valid DateOnly/TimeOnly from random DateTime (fixes AutoFixture gap)
- `JsonElementCustomization` — creates cloned JsonElements (requires `.Clone()` for safety post-GC)
- `TypeCustomization<T>` — generic factory pattern; `Add<T>(Func<IFixture, T>)` convenience method
- `SpecimenRequestHelper` — extracted public static helper for pattern-matching request types
- `CancellationTokenCustomization` — returns `new CancellationToken(false)`; prevents AutoFixture dominant-value footgun (already-cancelled token); registered as 5th default

**Defaults Seeding:**
- `FixtureCustomizationCollection` seeds 5 defaults: AutoNSubstitute, Recursion, ImmutableCollection, DateOnlyTimeOnly, CancellationToken
- `JsonElementCustomization` remains opt-in (not in defaults)

**Refactoring Complete:**
- Removed `SutFixture` class, `SutFixtureCustomizations` static class
- Consolidated via `FixtureFactory` + `FixtureFactory.Customizations` (FixtureCustomizationCollection)
- Applied organization-wide field naming: private fields/statics use plain camelCase (no `_`/`s_` prefix)
- All tests migrated to `FixtureFactory` API; 122 tests passing (as of Phase 12)

**Data Attribute Pipeline (Phase 12):**
- `AutoNSubstituteDataHelper.MergeValues` injects live fixture instance when `parameter.ParameterType.IsAssignableFrom(typeof(Fixture))` — covers both `IFixture` and `Fixture`
- `[Frozen]` on `IFixture` parameters is a no-op; fixture is injected, not frozen into the specimen container
- Injection branch runs before `CreateValue`, ensuring fixture is always the actual live instance

## Learnings

### Phase 14-15: DisposalTracker Integration (2026-03-07)

**Task:** Register fixture-generated theory argument values with xUnit3's `DisposalTracker` so disposable objects are cleaned up after each test case.

**Implementation (Phase 15):**
- Added `disposalTracker.AddRange(values)` in all four `GetData()` methods:
  - `AutoNSubstituteDataAttribute.GetData()`
  - `InlineAutoNSubstituteDataAttribute.GetData()`
  - `ClassAutoNSubstituteDataAttribute.GetData()`
  - `MemberAutoNSubstituteDataAttribute.GetData()`
- Call placed immediately after `AutoNSubstituteDataHelper.MergeValues()` and before constructing `TheoryDataRow`
- Registration happens once per row (inside loop for multi-row attributes)

**Outcome:**
- All fixture-generated `IDisposable`/`IAsyncDisposable` values now disposed deterministically after each test
- No API changes visible to consumers
- Build clean, no regressions
- `AutoNSubstituteDataHelper` itself was **not** changed — the tracker registration lives at the call site in each attribute.
- For multi-row attributes (`MemberAutoNSubstituteData`, `ClassAutoNSubstituteData`), `AddRange` is called once per row inside the loop.
- `DisposalTracker.AddRange` silently skips non-disposable values — no need to filter first.
- Replaced `/// <inheritdoc />` on each `GetData()` with explicit XML doc that documents the `disposalTracker` parameter and disposal behaviour.

**Key insight:** `DisposalTracker` is in `Xunit.Sdk` (already imported); it aggregates disposal exceptions rather than failing fast, so multiple disposables per row are all attempted.

**Cross-team:** Zoe writing disposal tests in parallel.

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### Key Patterns (Phases 1-11)

- **AutoFixture specimen pipeline:** `new SpecimenContext(fixture).Resolve(type)` is the zero-reflection way to create values; `SpecimenBuilderNodeFactory.CreateTypedNode` + `FixedBuilder` is the internal freeze mechanism
- **CancellationToken footgun:** AutoFixture dominant-value heuristic produces `true` for bool params → already-cancelled tokens. Fix: `new CancellationToken(false)` is serializable and preserves xUnit 3 per-test discovery. Live `CancellationTokenSource` tokens break discovery.
- **xUnit 3 `SupportsDiscoveryEnumeration`:** Custom data attributes must return `true`; non-serializable params (live tokens) force `XunitDelayEnumeratedTheoryTestCase`
- **PropertyInfo arm critical:** Specimen builders must handle `PropertyInfo` requests or property-typed fields won't be populated by AutoFixture
- **TypeCustomization<T> receives `IFixture` (not context):** Ergonomic API; factory lambda `f => f.Create<DateOnly>()` preferred over low-level context resolution
- **JsonElement must Clone():** `JsonElement` backed by un-cloned `JsonDocument` becomes invalid after GC

### Phase 12: IFixture/Fixture Parameter Injection (2026-03-07T17:33:43Z)

**Task:** Inject live fixture instance for theory parameters of type `IFixture` or `Fixture`.

**Implementation:**
- `AutoNSubstituteDataHelper.MergeValues` — new branch using `parameter.ParameterType.IsAssignableFrom(typeof(Fixture))`
- Covers both `IFixture` and `Fixture` in one check
- Matched parameter receives the live fixture directly; bypasses `CreateValue`
- `[Frozen]` on `IFixture` parameters: injected normally, frozen branch skipped
- XML doc on `MergeValues` updated

**Cross-team QA:** Zoe verified 6 new tests, all 122 passing.
**Decision logged:** `.squad/decisions.md` — `IFixture/Fixture Parameter Injection in Theory Methods`

### Phase 13: Substitute Attribute Refactor (2026-03-07T18:44:29Z)

**Task:** Remove duplicate SubstituteAttribute, fix ParameterInfo passing to enable AutoFixture's attribute pipeline.

**Implementation:**
- **Deleted** `src/Cabazure.Test/Attributes/SubstituteAttribute.cs` — redundant (AutoFixture.AutoNSubstitute.SubstituteAttribute is canonical)
- **Fixed** `AutoNSubstituteDataHelper.CreateValue` signature: `CreateValue(ParameterInfo)` instead of `CreateValue(Type)`
- **Changed** specimen creation: `SpecimenContext.Resolve(parameter)` instead of `Resolve(type)` to trigger attribute processing
- **Removed** isSubstitute check branch from `MergeValues` (no longer needed)
- **Removed** NSubstitute using from MergeValues

**Rationale:** ParameterInfo carries attribute metadata; Resolve(parameter) invokes AutoFixture's attribute resolution pipeline, naturally firing SubstituteAttribute without custom code.

**Cross-team QA:** Zoe verified updated tests, all 127 passing.
**Decision logged:** `.squad/decisions.md` — Substitute Attribute ParameterInfo refactor