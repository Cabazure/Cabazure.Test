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
