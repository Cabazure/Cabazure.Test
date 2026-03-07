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