# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain: AutoFixture customizations, the `ISpecimenBuilder` routing interface/abstract-class requests to NSubstitute. Challenge: AutoFixture doesn't natively create substitutes; we bridge that via `AutoNSubstituteCustomization`.

**Completed Work Summary (Phases 1-18):**

Architecture: `FixtureFactory.Create()` factory + 4 data attributes + 8 customizations (AutoNSubstitute, Recursion, ImmutableCollection, DateOnlyTimeOnly, CancellationToken, TypeCustomization, SpecimenRequestHelper, JsonElement opt-in). Full NSubstitute integration: custom argument matchers (FluentArg + ReceivedCallExtensions) + async call waiting (WaitForReceivedExtensions). Key patterns: SpecimenContext zero-reflection, ExceptionDispatchInfo stack traces, ProtectedMethodExtensions for protected method invocation, IFixture injection, JsonElement cloning, ParameterInfo attribute resolution.

## Recent Work

### Phases 1-11: Foundation & Customizations (2026-03-07)

**Completed:**
- Architecture: FixtureFactory, 4 data attributes, 5 default customizations
- Customizations: AutoNSubstitute, Recursion, ImmutableCollection, DateOnlyTimeOnly, CancellationToken
- Specimen builders: TypeCustomization<T>, SpecimenRequestHelper
- Refactored SutFixture → FixtureFactory; migrated all tests
- Applied org-wide camelCase field naming convention
- Key learnings: SpecimenContext zero-reflection, CancellationToken(false) safety, PropertyInfo criticality, JsonElement cloning requirement

**Test Status:** 111+ passing

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

### Phases 14-16: DisposalTracker, FluentArg & ReceivedCallExtensions (2026-03-07)

**Phase 14 (Disposal):**
- Added `disposalTracker.AddRange(values)` in all four data attribute `GetData()` methods
- All fixture-generated `IDisposable`/`IAsyncDisposable` values now disposed deterministically after each test
- No API changes visible to consumers; build clean, no regressions

**Phase 16 (NSubstitute Integration):**
- `FluentArg.Matching<T>` — FluentAssertions-backed NSubstitute argument matcher
- `ReceivedCallExtensions` — `ReceivedArg<T>` (last arg) / `ReceivedArgs<T>` (all args) extensions
- Key NSubstitute internals: ArgumentMatcher.Enqueue<T>, IDescribeNonMatches interface, GenericToNonGenericMatcherProxyWithDescribe
- Skill extracted: `.squad/skills/nsubstitute-custom-matcher/SKILL.md`

### Phases 13-18: Refactoring, Async Waiting & Orchestration (2026-03-07)

**Phase 13 (Substitute Refactor):**
- Removed redundant SubstituteAttribute, fixed ParameterInfo passing
- Resolve(parameter) triggers AutoFixture's attribute resolution pipeline
- 127 tests passing

**Phase 17 (WaitForReceivedExtensions):**
- WaitForReceived<T>() / WaitForReceivedWithAnyArgs<T>() for async call waiting
- SignalingCallHandler + TaskCompletionSource, race-free pre-check, TestContext.Current.CancellationToken
- DefaultTimeout = TimeSpan.FromSeconds(10) (mutable)
- 152 tests passing

**Phase 18 (Orchestration):**
- Orchestration logs, session log, decision merge
- Phase 13 orchestration for WaitForReceivedExtensions

**Status:** ✅ Complete
- Decisions merged and deduplicated
- Agent histories updated with Phase 13 context
- Ready for final git commit

### Phase 19: ProtectedMethodExtensions (2026-03-07)

**Task:** Implement `ProtectedMethodExtensions` — reflection-based helpers for invoking protected instance methods in tests without exposing them as public.

**Deliverables:**
- `src/Cabazure.Test/ProtectedMethodExtensions.cs` — 4 public overloads + 3 private helpers

**Reflection approach:**
- `BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy` on `GetMethods()` to discover protected methods across the entire type hierarchy. `FlattenHierarchy` is technically for statics, but harmless here; `NonPublic | Instance` is what actually surfaces inherited protected members.
- Filter with `!m.IsPrivate` to exclude private base-class methods while keeping `protected`, `protected internal`, and `internal` methods.

**Overload disambiguation logic:**
1. Filter candidates by name + `!m.IsPrivate`.
2. Narrow by parameter count — covers the most common disambiguation case with zero extra work.
3. For remaining ties, check each parameter with `paramType.IsAssignableFrom(arg?.GetType() ?? typeof(object))`. Null args map to `typeof(object)`, which only matches `object`-typed parameters — intentionally strict per spec.
4. Still ambiguous after step 3 → `AmbiguousMatchException` listing all candidate signatures.
5. No candidates after step 2 or 3 → `MissingMethodException` with type name, method name, and resolved arg type list.

**ExceptionDispatchInfo pattern:**
```csharp
catch (TargetInvocationException ex) when (ex.InnerException is not null)
{
    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
    throw; // unreachable; satisfies the compiler
}
```
The `throw;` after `.Throw()` is unreachable at runtime but required so the compiler sees all code paths as returning/throwing. `ExceptionDispatchInfo.Throw()` re-throws the original exception with its original stack trace intact — tests see the real exception, not a `TargetInvocationException` wrapper.

**Code-reuse via delegation:**
- `InvokeProtected` (void) → calls `InvokeProtected<object>`, discards result.
- `InvokeProtectedAsync` (Task) → calls `InvokeProtectedAsync<object>`; `Task<object>` is compatible with `Task`.

**Async typed overload handles three cases:**
1. `result is Task<TResult>` — await directly (happy path for typed async).
2. `result is Task` — await base task and return `default!` (handles `Task`-returning methods called via the generic overload from `InvokeProtectedAsync`).
3. Otherwise — direct cast (handles synchronous methods accidentally called via async overload).

**Build:** Clean, 0 warnings, 0 errors.

### Phase 14: ProtectedMethodExtensions (2026-03-07T20:37:30Z)

**Task:** Implement ProtectedMethodExtensions — reflection-based helpers for invoking protected instance methods in tests without exposing them as public.

**Deliverable:**
- src/Cabazure.Test/ProtectedMethodExtensions.cs (312 lines)
  - 4 public overloads: InvokeProtected, InvokeProtected<TResult>, InvokeProtectedAsync, InvokeProtectedAsync<TResult>
  - 3 private helpers: reflection discovery, async result handling, exception unwrapping

**Design Decisions:**
1. Void and async non-typed overloads delegate to typed counterparts (single reflection path)
2. BindingFlags.NonPublic | Instance | FlattenHierarchy discovers protected methods across inheritance
3. Two-stage overload disambiguation: parameter count (O(n)) → type compatibility check
4. ExceptionDispatchInfo.Capture(inner).Throw() unwraps TargetInvocationException with stack trace preservation
5. Null arguments strict matching: rg?.GetType() ?? typeof(object) (only matches object params)
6. Ambiguous matches → AmbiguousMatchException with all candidate signatures
7. Missing methods → MissingMethodException with method name + arg types

**Build:** Clean ✅
**Test Integration:** Zoe's test suite validates all scenarios (10 tests, 162/162 passing)
**Cross-team:** Zoe created implementation as squad unblock; Kaylee baseline design locked by test API contract.

### Phase 16 (Take 2): Replace Custom FrozenAttribute with AutoFixture.Xunit3.FrozenAttribute (2026-03-07)

**Task:** Remove `Cabazure.Test.Attributes.FrozenAttribute` and wire up `AutoFixture.Xunit3.FrozenAttribute` instead.

**What changed:**
- `src/Cabazure.Test/Cabazure.Test.csproj` — added `AutoFixture.Xunit3 4.19.0` package reference
- `src/Cabazure.Test/Attributes/FrozenAttribute.cs` — deleted entirely
- `src/Cabazure.Test/Attributes/AutoNSubstituteDataHelper.cs`:
  - Added `using AutoFixture.Xunit3;`
  - `MergeValues` now reads `parameter.GetCustomAttribute<FrozenAttribute>()` (resolves to `AutoFixture.Xunit3.FrozenAttribute`)
  - Auto-generated frozen params: call `fixture.Customize(frozenAttr.GetCustomization(parameter))` **before** `CreateValue`. `FreezeOnMatchCustomization` creates the value and registers it as frozen in one step; `CreateValue` then returns the already-frozen instance.
  - Provided frozen params: unchanged — `FreezeValue(fixture, type, value)` path using `SpecimenBuilderNodeFactory.CreateTypedNode(type, new FixedBuilder(value))` is equivalent to `Matching.ExactType`.
- XML docs in all four `*AutoNSubstituteData*Attribute.cs` files updated: `<see cref="FrozenAttribute"/>` → `<see cref="AutoFixture.Xunit3.FrozenAttribute"/>`.

**Key insight:** `FreezeOnMatchCustomization` (used by `AutoFixture.Xunit3.FrozenAttribute.GetCustomization`) calls `new SpecimenContext(fixture).Resolve(Request)` internally to create the specimen, then inserts `FilteringSpecimenBuilder(FixedBuilder(value), Matcher)` at index 0. So after customization, the next `Resolve(parameter)` call returns the already-registered frozen value. The old pattern (create then freeze) still works for provided values where we already have the object in hand.

**Build:** Clean ✅

## Learnings

### Phase 18: StringContentExtensions (2026-03-08)

**Task:** Implement `src/Cabazure.Test/Assertions/StringContentExtensions.cs` — 6 extension methods on `StringAssertions` for format-ignorant string comparison.

**Implementation:**
- `BeSimilarTo` / `NotBeSimilarTo` — whitespace normalization via `NormalizeWhitespace(string? s)` using `Regex.Replace(s.Trim(), @"\s+", " ")`
- `BeXmlEquivalentTo` / `NotBeXmlEquivalentTo` — XML normalization via `XDocument.Parse(s).ToString(SaveOptions.DisableFormatting)`
- `BeJsonEquivalentTo` / `NotBeJsonEquivalentTo` — JSON normalization via `JsonSerializer.Serialize(JsonDocument.Parse(s).RootElement)`
- All extend `StringAssertions` (concrete FA class), return `AndConstraint<StringAssertions>`
- FA 7.x assertion pattern: `Execute.Assertion.BecauseOf(...).ForCondition(...).FailWith(...)`
- Invalid XML/JSON propagates naturally as `XmlException`/`JsonException` — no try/catch
- Private helpers `NormalizeWhitespace`, `NormalizeXml`, `NormalizeJson` all accept `string?` and return `string.Empty` for null
- `BeSimilarTo`/`NotBeSimilarTo` `expected` params typed as `string?`; XML/JSON variants typed as `string`
- Positive failure messages include both `{0}` (normalized expected) and `{1}` (normalized actual); negative messages include only `{0}` (normalized expected)
- Both BCL dependencies (`System.Xml.Linq`, `System.Text.Json`) already in .NET 9 — no new NuGet references

**Build:** Clean ✅

### AutoFixture.Xunit3.FrozenAttribute pattern (Phase 16)

`AutoFixture.Xunit3.FrozenAttribute.GetCustomization(parameter)` returns a `FreezeOnMatchCustomization`. When applied via `fixture.Customize(...)` **before** `CreateValue`, it both creates the specimen internally and registers it at index 0 as a `FilteringSpecimenBuilder(FixedBuilder(value), Matcher)`. The subsequent `CreateValue` call therefore returns the already-frozen instance from the cache — no double creation, no extra registration step needed. For provided values (inline/class/member data), skip `GetCustomization` and use the existing `FreezeValue` path (`SpecimenBuilderNodeFactory.CreateTypedNode`) so we freeze the exact object in hand rather than letting AutoFixture create a new one.

### Phase 16 (FrozenAttribute Replacement — 2026-03-07T21:44:11Z)

**Task:** Replace custom Cabazure.Test.Attributes.FrozenAttribute with AutoFixture.Xunit3.FrozenAttribute.

**Implementation:**
- Deleted src/Cabazure.Test/Attributes/FrozenAttribute.cs
- Updated AutoNSubstituteDataHelper.MergeValues to call rozenAttr.GetCustomization(parameter) before CreateValue
- Added AutoFixture.Xunit3 4.19.0 to src/Cabazure.Test/Cabazure.Test.csproj
- Build verified clean; no test modifications needed in source library (Zoe's responsibility)

**Why:** Removes custom attribute surface area; users now get Matching enum support (ExactType, DirectBaseType, ImplementedInterfaces) for free. AutoFixture.Xunit3 is battle-tested upstream standard.

**Key Behavioral Notes:**
- Auto-generated frozen params: FreezeOnMatchCustomization creates AND freezes before CreateValue returns
- Provided frozen params: FreezeValue path kept; equivalent to Matching.ExactType, no duplicate creation
- Value type guard unchanged

**Cross-team:** Zoe handled test migration (7 files, type alias pattern, 165 passing); Wash clarified README documentation (namespace, examples, Matching enum).

**Decision logged:** .squad/decisions.md — "Phase 16: Replace Custom FrozenAttribute with AutoFixture.Xunit3.FrozenAttribute"

### Phase 17: FluentAssertions Extensions (2026-03-07T23:11:00Z)

**Task:** Implement custom FluentAssertions extensions for JsonElement and DateTimeOffset.

**Implementation:**
- Created `src/Cabazure.Test/Assertions/` folder
- `JsonElementAssertions.cs`:
  - Custom assertions class for `JsonElement` struct
  - FA 7.0.0 pattern: `Execute.Assertion` (NOT AssertionChain — that's FA 8.x)
  - Two `BeEquivalentTo` overloads: `JsonElement` and `string` (parses then delegates)
  - Comparison via `JsonSerializer.Serialize` (normalizes whitespace, preserves key order)
  - Returns `AndConstraint<JsonElementAssertions>` for chaining
  - `Should()` extension method on `JsonElement`
- `DateTimeOffsetExtensions.cs`:
  - `CabazureAssertionOptions.DateTimeOffsetPrecision` static property (default: 1 second)
  - Four extension methods on `DateTimeOffsetAssertions<TAssertions>`:
    1. `BeCloseTo(nearbyTime)` — default precision
    2. `BeCloseTo(nearbyTime, int precisionMilliseconds)` — int ms
    3. `NotBeCloseTo(distantTime)` — default precision
    4. `NotBeCloseTo(distantTime, int precisionMilliseconds)` — int ms
  - All delegate to FA instance methods (`assertions.BeCloseTo(...)` / `assertions.NotBeCloseTo(...)`)
  - Skipped TimeSpan overloads — FA already has them as instance methods, extension would be shadowed (dead code)

**Key Design Decisions:**
- `JsonElement` is a struct → no base class inheritance (struct can't inherit from FA's `ReferenceTypeAssertions`)
- FA 7.0.0 assertions use `Execute.Assertion.BecauseOf(...).ForCondition(...).FailWith(...)` pattern
- DateTimeOffset extensions provide two new overload signatures: no-args (uses global default) and int-ms (convenient for inline literals)
- `AndConstraint<T>` is in `FluentAssertions` namespace; `Execute.Assertion` is in `FluentAssertions.Execution`

**Build:** Clean ✅

**Cross-team:** Zoe provided test coverage (19 tests, 19 passing); Wash added comprehensive README documentation with examples.
**Decision logged:** `.squad/decisions.md` — Phase 17 FluentAssertions Extensions (Decisions 1–3: JsonElementAssertions, DateTimeOffsetExtensions, Documentation)

### AutoNSubstituteDataHelper Refactor — Inline Trivial Helpers

**Task:** Remove three wrapper methods from `AutoNSubstituteDataHelper` and inline their logic at the call sites.

**What changed:**
- `CreateFixture` removed; all four attribute `GetData()` methods now call `FixtureFactory.Create(testMethod)` directly.
- `CreateValue` one-liner inlined into `MergeValues` as `new SpecimenContext(fixture).Resolve(parameter)`.
- `FreezeValue` body inlined into `MergeValues`; the redundant `IsValueType` guard inside `FreezeValue` was dropped because the call site already has `!parameter.ParameterType.IsValueType` as a condition.
- `using System.Reflection;` kept in `AutoNSubstituteDataHelper.cs` — `ParameterInfo` still requires it.
- 5 files changed, net −25 lines. Build clean, 184/184 tests passing.

**Key principle:** When a helper method is a pure passthrough or a one-liner with no reuse value, inline it — the call site already has all the context needed and the indirection only obscures intent.

### FixtureDataExtensions — MergeValues as IFixture Extension Method

**Task:** Convert `AutoNSubstituteDataHelper` static helper to an `IFixture` extension class `FixtureDataExtensions`.

**What changed:**
- Deleted `src/Cabazure.Test/Attributes/AutoNSubstituteDataHelper.cs`
- Created `src/Cabazure.Test/Attributes/FixtureDataExtensions.cs` — same logic, class renamed to `FixtureDataExtensions`, `MergeValues` gains `this IFixture fixture` as first parameter
- `AutoNSubstituteDataAttribute` and `InlineAutoNSubstituteDataAttribute`: removed `var parameters = ...` local, inlined `testMethod.GetParameters()` into the `fixture.MergeValues(...)` call
- `ClassAutoNSubstituteDataAttribute` and `MemberAutoNSubstituteDataAttribute`: `theoryParams` local retained (used across loop iterations); call site updated to `fixture.MergeValues(theoryParams, row)`
- 6 files changed. Build clean, 184/184 tests passing.

**Design note:** Extension method on `IFixture` reads more naturally at the call site (`fixture.MergeValues(...)`) and keeps the method discoverable via IDE autocomplete on fixture instances.

### Phase 22 Part 1: Namespace Consolidation (2026-03-08)

**Task:** Consolidate namespaces so `using Cabazure.Test;` is the only import needed for test authors.

**Implementation (Part 1):**
1. Changed namespace from `Cabazure.Test.Attributes` → `Cabazure.Test` in 5 public attribute files:
   - `AutoNSubstituteDataAttribute.cs`
   - `InlineAutoNSubstituteDataAttribute.cs`
   - `MemberAutoNSubstituteDataAttribute.cs`
   - `ClassAutoNSubstituteDataAttribute.cs`
   - `CustomizeWithAttribute.cs`
2. Added `using Cabazure.Test.Attributes;` to those 5 files to access internal `FixtureDataExtensions.MergeValues` extension method
3. Moved `FixtureCustomizationCollection`:
   - From: `src/Cabazure.Test/FixtureCustomizationCollection.cs` (namespace `Cabazure.Test`)
   - To: `src/Cabazure.Test/Customizations/FixtureCustomizationCollection.cs` (namespace `Cabazure.Test.Customizations`)
   - Updated `FixtureFactory.cs` using: `Cabazure.Test.Attributes` → `Cabazure.Test.Customizations`

**Why:** FixtureCustomizationCollection is accessed via `FixtureFactory.Customizations` (module initializer pattern), not directly in test methods. Placing it in the `Customizations` sub-namespace reduces root-level noise while keeping the public types (attributes) in `Cabazure.Test` where test authors expect them.

**Build:** Clean ✅ (src project verified; tests will need using updates in Part 2)

**Internal detail preserved:** `FixtureDataExtensions` remains `internal` in `Cabazure.Test.Attributes` namespace — no user-visible impact. The public attributes now live in `Cabazure.Test` and explicitly import `Cabazure.Test.Attributes` to access the helper.

### Phase 23: JsonElementEquivalencyStep (2026-03-08)

**Task:** Implement `JsonElementEquivalencyStep` (FA 7.0.0 `IEquivalencyStep`) + `UsingJsonElementComparison` extension to handle `JsonElement` properties in `BeEquivalentTo` calls.

**Key FA 7.0.0 types (confirmed by ILSpy decompilation):**
- `IEquivalencyStep` — in `FluentAssertions.Equivalency` namespace
- `Handle(Comparands, IEquivalencyValidationContext, IEquivalencyValidator)` — returns `EquivalencyResult`
- `EquivalencyResult` — enum with `ContinueWithNext` and `AssertionCompleted` values
- `Comparands` — class with `Subject` (`object`) and `Expectation` (`object`) properties
- `IEquivalencyValidationContext.Reason` — type `FluentAssertions.Execution.Reason` with `FormattedMessage` (string) and `Arguments` (object[]) properties
- `BeEquivalentTo` lambda uses `EquivalencyAssertionOptions<TExpectation>` (generic)
- `AssertEquivalencyUsing` lambda uses `EquivalencyAssertionOptions` (non-generic)
- Both inherit from `SelfReferenceEquivalencyAssertionOptions<TSelf>` which has `Using(IEquivalencyStep) → TSelf`

**Extension method design:** Generic on `TSelf` extending `SelfReferenceEquivalencyAssertionOptions<TSelf>` — single method works for both per-call (`BeEquivalentTo` lambda) and global (`AssertEquivalencyUsing` lambda) registration. Returns `TSelf` to allow further chaining.

**Step logic:** Pattern-match both `comparands.Subject` and `comparands.Expectation` to `JsonElement`; if either is not a `JsonElement`, return `ContinueWithNext`. Otherwise normalize both via `JsonSerializer.Serialize()`, assert string equality via `Execute.Assertion.BecauseOf(context.Reason.FormattedMessage, context.Reason.Arguments)`, return `AssertionCompleted`.

**Build:** Clean ✅

### Phase 24: EmptyObjectEquivalencyStep (2026-03-08)

**Task:** Implement EmptyObjectEquivalencyStep (FA 7.0.0 IEquivalencyStep) + AllowingEmptyObjects extension to allow BeEquivalentTo to succeed when comparing instances of types with no public properties or fields.

**Problem:** FluentAssertions 7.x throws InvalidOperationException ("No members were found for comparison…") from StructuralEqualityEquivalencyStep when the root-level object graph has zero public instance members. This is common when testing serialisation round-trips with marker/empty types.

**Solution:** Intercepts the comparison pipeline before the structural step. If the expectation type has zero public instance properties AND zero public instance fields, the instances are considered trivially equivalent and the assertion completes immediately. All other types pass through to FA's normal pipeline.

**Step logic:**
1. Get type from comparands.Expectation?.GetType()
2. If null, return ContinueWithNext
3. Check for public instance properties using GetProperties(BindingFlags.Public | BindingFlags.Instance)
4. Check for public instance fields using GetFields(BindingFlags.Public | BindingFlags.Instance)
5. If either has members, return ContinueWithNext
6. Otherwise return AssertionCompleted (trivially equivalent)

**Extension method pattern:** Follows JsonElementEquivalencyStep pattern — generic on TSelf extending SelfReferenceEquivalencyAssertionOptions<TSelf>. Works for both per-call (BeEquivalentTo lambda) and global (AssertEquivalencyUsing lambda) registration. Calls options.Using(new EmptyObjectEquivalencyStep()).

**Files created:**
- src/Cabazure.Test/Assertions/EmptyObjectEquivalencyStep.cs
- src/Cabazure.Test/Assertions/EmptyObjectEquivalencyExtensions.cs

**Build:** Not yet verified

**Cross-team:** Zoe created comprehensive test suite (5 tests); Wash updated README + committed feature.

**Status:** ✅ Complete — phase24 orchestration logged
