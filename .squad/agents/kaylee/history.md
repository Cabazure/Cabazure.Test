# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the guts of the library: AutoFixture customizations, the `ISpecimenBuilder` that routes interface/abstract-class requests to NSubstitute. Key challenge: AutoFixture doesn't natively create substitutes for abstract/interface types; we bridge that via `AutoNSubstituteCustomization`.

**Completed Work Summary (Phases 1-13):**

Architecture: `FixtureFactory.Create()` static factory + 4 data attributes + 7 customizations (AutoNSubstitute, Recursion, ImmutableCollection, DateOnlyTimeOnly, JsonElement, TypeCustomization, CancellationToken, SpecimenRequestHelper). 5 registered as defaults. Refactored away `SutFixture`/`SutFixtureCustomizations` classes. Applied camelCase field naming. All tests migrated; 152+ tests passing. Data attribute pipeline: live fixture injection (Phase 12) + disposal tracking (Phase 15) + ParameterInfo refactor (Phase 13). NSubstitute integration: custom argument matchers (FluentArg + ReceivedCallExtensions, Phase 16) + async call waiting (WaitForReceivedExtensions, Phase 18). Key patterns: SpecimenContext for zero-reflection creation, CancellationToken(false) non-cancellation default, PropertyInfo requirement, JsonElement cloning, IFixture injection before CreateValue.

## Recent Work

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

### Phase 16: FluentArg and ReceivedCallExtensions (2026-03-07)

**Task:** Create `FluentArg.Matching<T>` (FluentAssertions-backed NSubstitute argument matcher) and `ReceivedCallExtensions` (`ReceivedArg<T>` / `ReceivedArgs<T>`).

**New files:**
- `src/Cabazure.Test/FluentArg.cs` — `FluentArg` static class + `FluentAssertionArgumentMatcher<T>` internal class
- `src/Cabazure.Test/ReceivedCallExtensions.cs` — extension methods on `object` (the substitute)

**Key NSubstitute internals confirmed:**
- `ArgumentMatcher.Enqueue<T>(IArgumentMatcher<T>)` is the correct public registration point (namespace `NSubstitute.Core.Arguments`)
- `IArgumentMatcher<T>.IsSatisfiedBy` signature is `bool IsSatisfiedBy(T? argument)` — nullable `T?`, not `T`
- Exceptions in `IsSatisfiedBy` are silently swallowed; `IDescribeNonMatches.DescribeFor` is the only channel for surfacing failure messages in `ReceivedCallsException`
- `ArgumentMatcher.Enqueue<T>` auto-wraps with `GenericToNonGenericMatcherProxyWithDescribe<T>` when matcher also implements `IDescribeNonMatches` — no extra registration needed
- `IDescribeNonMatches` is in namespace `NSubstitute.Core` (not `NSubstitute.Core.Arguments`)

**`ReceivedCallExtensions` API:**
- `ReceivedArg<T>(this object substitute)` — last call, first arg of type T; throws `InvalidOperationException` on no-calls or not-found
- `ReceivedArgs<T>(this object substitute)` — all args of type T across all calls; returns empty enumerable (never throws)
- Uses `substitute.ReceivedCalls()` (NSubstitute extension, `using NSubstitute;`) returning `IEnumerable<ICall>`

**Build:** Clean, 0 warnings, 0 errors.

**Skill extracted:** `.squad/skills/nsubstitute-custom-matcher/SKILL.md`
**Decisions logged:** `.squad/decisions/inbox/kaylee-fluentarg-impl.md`

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
### Phase 17: FluentArg & ReceivedCallExtensions Implementation (2026-03-07)

**Task:** Create FluentArg.Matching<T>() and ReceivedCallExtensions — inline NSubstitute argument matcher with FluentAssertions integration.

**Deliverables:**
- src/Cabazure.Test/FluentArg.cs — public sealed class with static Matching<T>() method and internal FluentAssertionArgumentMatcher<T> implementation
- src/Cabazure.Test/ReceivedCallExtensions.cs — public extension methods ReceivedArg<T>() and ReceivedArgs<T>() on object

**Key Design Decisions:**
1. Public API: FluentArg.Matching<T>(Action<T>) only; matcher class is internal sealed
2. IsSatisfiedBy uses T? nullable parameter; forwarding action is Action<T>; null-forgiving operator ! is safe for NSubstitute's call context
3. DescribeFor type-checks before re-running assertion to prevent InvalidCastException
4. ReceivedArg<T>() throws InvalidOperationException (consistent with LINQ First() semantics)
5. ReceivedArgs<T>() returns empty enumerable (never throws)

**Build Status:** Clean
**Commit:** 411c454 (feat: add FluentArg.Matching and ReceivedCallExtensions)

### Phase 18: WaitForReceivedExtensions Implementation (2026-03-07)

**Task:** Create async call waiting extensions for NSubstitute, enabling race-free verification of calls in concurrent/async test scenarios.

**Deliverables:**
- src/Cabazure.Test/WaitForReceivedExtensions.cs — WaitForReceived<T>() / WaitForReceivedWithAnyArgs<T>() extensions and internal SignalingCallHandler

**Key NSubstitute internals confirmed:**
- Call specification capture: SetNextRoute → RecordCallSpecification → UseCallSpecInfo → Handle(spec=>spec, call=>CreateFrom)
- ICallSpecification.CreateCopyThatMatchesAnyArguments() — creates any-args variant after capture
- CallHandlerFactory signature: `delegate ICallHandler(ISubstituteState)`
- RegisterCustomCallHandlerFactory accumulates handlers (no unregister API); handlers remain for substitute lifetime
- ICallRouter.ReceivedCalls() returns IEnumerable<ICall> for pre-check (race-free when registered after handler)

**Design Decisions:**
1. DefaultTimeout = TimeSpan.FromSeconds(10) — static mutable field; users can set in [ModuleInitializer]
2. CancellationToken sourced from TestContext.Current.CancellationToken (xunit.v3.extensibility.core)
3. timeout parameter is nullable TimeSpan? — null means use DefaultTimeout; Timeout.InfiniteTimeSpan for infinite wait
4. Handler registration BEFORE pre-check ensures race-free detection (future calls + already-received)
5. TaskCompletionSource with RunContinuationsAsynchronously prevents synchronous continuation deadlocks
6. TrySetResult is idempotent — accumulated handlers become no-ops after first signal (acceptable for test lifetimes)
7. PendingSpecificationInfo.Handle branch: spec arm (created from Received call) + call arm (fallback to CreateFrom)
8. MatchArgs.AsSpecifiedInCall used in CreateFrom (preserves exact argument matching from expression)

**xUnit 3 Integration:**
- TestContext.Current never returns null — falls back to idle context with default CancellationToken
- No additional package reference needed (xunit.v3.extensibility.core already present)

**Build Status:** Clean, 0 warnings, 0 errors

### Phase 18: WaitForReceivedExtensions (2026-03-07T20:21:58Z)

**Task:** Implement WaitForReceivedExtensions — async call waiting for concurrent/async test scenarios.

**Deliverables:**
- src/Cabazure.Test/WaitForReceivedExtensions.cs (160 lines)
  - Public: WaitForReceived<T>() / WaitForReceivedWithAnyArgs<T>()
  - Internal: SignalingCallHandler with TaskCompletionSource
  - Static: DefaultTimeout = TimeSpan.FromSeconds(10) (mutable for test suites)

**Design Highlights:**
- CancellationToken sourced from TestContext.Current.CancellationToken (no parameter)
- Handler registration BEFORE pre-check ensures race-free detection
- ICallRouter.RegisterCustomCallHandlerFactory accumulates handlers (idempotent via TrySetResult)
- ICallSpecification.CreateCopyThatMatchesAnyArguments() for any-arg matching

**Build:** Clean. Tests: 152/152 passing (8 new by Zoe + 144 existing).
**Commit:** af98f11 — feat(concurrency): add WaitForReceived and WaitForReceivedWithAnyArgs

### Phase 13 Orchestration (2026-03-07T20:22:30Z)

**Task:** Scribe logging and decision merge for WaitForReceivedExtensions completion.

**Deliverables:**
- Orchestration logs: `.squad/orchestration-log/2026-03-07T20-22-30Z-kaylee.md` & `.squad/orchestration-log/2026-03-07T20-22-30Z-zoe.md`
- Session log: `.squad/log/2026-03-07T20-22-30Z-phase-13-waitforreceived.md`
- Decision merge: kaylee-waitforreceived.md → `.squad/decisions.md` (Decision #23)
- Cross-agent history updates: Appended to both Kaylee and Zoe history

### Phase 14: ProtectedMethodExtensions (2026-03-07T20:36:00Z)

**Task:** Implement `ProtectedMethodExtensions` — reflection-based test utility for invoking protected instance methods.

**Implementation:**
- File: `src/Cabazure.Test/ProtectedMethodExtensions.cs` (~180 lines)
- Four overloads: `InvokeProtected`, `InvokeProtected<TResult>`, `InvokeProtectedAsync`, `InvokeProtectedAsync<TResult>`
- Single code path per arity: void delegates to `<object>`, async-void delegates to async-typed
- ExceptionDispatchInfo unwrapping for clean exception stacks (no TargetInvocationException wrapper)
- Two-stage overload resolution: parameter count → type compatibility
- BindingFlags: NonPublic | Instance | FlattenHierarchy; filters `!IsPrivate` for inherited protected members

**Design Decisions (Decision #24-25):**
1. Void overload delegates to generic overload (single code path)
2. ExceptionDispatchInfo for stack trace preservation
3. Two-stage overload disambiguation (count → type compat)
4. BindingFlags strategy for inclusive member lookup

**Cross-team:** Zoe writing test coverage in parallel; wrote 162 tests, all passing (Zoe also created implementation as squad unblock, confirming the design).

**Build:** Clean. Tests: 162/162 passing.

### Phase 14 Orchestration (2026-03-07T20:36:00Z)

**Task:** Scribe logging and decision merge for ProtectedMethodExtensions completion.

**Deliverables:**
- Orchestration logs: `.squad/orchestration-log/2026-03-07T20-36-00Z-kaylee.md` & `.squad/orchestration-log/2026-03-07T20-36-00Z-zoe.md`
- Session log: `.squad/log/2026-03-07T20-36-00Z-phase14-protected-methods.md`
- Decision merge: kaylee-protected-method-design.md & zoe-protected-method-tests.md → `.squad/decisions.md` (Decisions #24-25)
- Cross-agent history updates: Appended to both Kaylee and Zoe history

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
