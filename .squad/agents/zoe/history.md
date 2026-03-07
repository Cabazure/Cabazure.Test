# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the test project `Cabazure.Test.Tests`. The unique challenge: we're testing a testing library, and our tests must use that library themselves (dogfooding). Edge cases to watch: sealed classes, value types, types without parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

### Completed Test Coverage (2026-03-07, Phases 1-12)

**Test Files Completed:**
- `FixtureFactoryTests.cs` (15 tests) — migrated from SutFixtureTests, covers Create, Create(customizations), inheritance/interface/abstract type handling
- `AutoNSubstituteDataAttributeTests.cs` — four data attribute variants all verified
- `SutFixtureCustomizationsTests.cs` (8 tests) — global registry Add/All behavior, project-wide customization integration
- `CustomizeWithAttributeTests.cs` (8 tests) — method-level, class-level, multi-stacked attribute handling
- `RecursionCustomizationTests.cs` (5 tests) — null guard, behavior replacement (ThrowingRecursionBehavior → OmitOnRecursionBehavior)
- `ImmutableCollectionCustomizationTests.cs` (10 tests) — all 8 collection types plus property population via PropertyInfo
- `JsonElementCustomizationTests.cs` (6 tests) — ValueKind verification, property enumeration, clone/GC safety
- `DateOnlyTimeOnlyCustomizationTests.cs` (7 tests) — valid (non-default) date/time generation, property population
- `TypeCustomizationTests.cs` (15 tests) — generic factory pattern, wrapping (sealed class), overload testing, integration with Build<T>
- `FixtureCustomizationCollectionTests.cs` (updated) — null guards, overload dispatch for Add<T>(factory) and Add(builder), snapshot enumeration
- `SpecimenRequestHelperTests.cs` (5 tests) — pattern-matching all 5 branches (ParameterInfo, PropertyInfo, FieldInfo, Type, unknown)
- `CancellationTokenCustomizationTests.cs` (5 tests) — non-cancelled default, CanBeCanceled=false, equal tokens, opt-out via Remove<T>, integration with [AutoNSubstituteData]
- `AutoNSubstituteDataHelperFixtureInjectionTests.cs` (6 tests) — IFixture injection, same-instance, concrete Fixture, InlineData, [Frozen] edge case

**Test Results:**
- Phase 10 end: **111 tests passing**
- Phase 11 (CancellationToken): **116 tests passing**
- Phase 12 (Fixture injection): **122 tests passing**

**Key Testing Patterns Established:**
- **Namespace collision workaround:** Use `new AutoFixture.Fixture()` when ambient `Fixture` namespace exists
- **Sealed class testing:** Composition/wrapping pattern via `ICustomization` (cannot subclass)
- **Static collection mutations:** Protect with try/finally to restore state (prevent cross-test pollution)
- **FluentAssertions limits:** DateOnly/TimeOnly lack comparison operators; use `.Year` property or `.Not.Be(MinValue)` patterns
- **Dogfooding theory tests:** Use `[Theory, AutoNSubstituteData]` where applicable for library-under-test validation
- **Cloning requirement:** JsonElement must use `.Clone()` to survive beyond JsonDocument GC
- **Same-instance assertion:** Use `[Frozen]` + `fixture.Create<T>()` to prove injected fixture is the shared instance

## Learnings

### Phase 16: FluentArg & ReceivedCallExtensions Tests (2026-03-07)

**Task:** Write tests for `FluentArg` and `ReceivedCallExtensions` — Kaylee's new argument-matching utilities.

**Test Files Created:**
- `tests/Cabazure.Test.Tests/FluentArgTests.cs` (4 tests)
- `tests/Cabazure.Test.Tests/ReceivedCallExtensionsTests.cs` (8 tests)

**FluentArgTests Coverage:**
1. `Matching_PassingAssertion_ReceiveCheckSucceeds` — passing FA assertion doesn't throw
2. `Matching_FailingAssertion_ReceiveCheckThrowsWithFAMessage` — FA failure surfaces in `ReceivedCallsException.Message` (via `IDescribeNonMatches`)
3. `Matching_NullAssertion_ThrowsArgumentNullException` — null guard on assertion param
4. `Matching_WhenNoCallsReceived_ThrowsReceivedCallsException` — no-call baseline

**ReceivedCallExtensionsTests Coverage:**
1. `ReceivedArg_SingleCall_ReturnsArgFromLastCall`
2. `ReceivedArg_MultipleCalls_ReturnsArgFromLastCall` — last-call semantics
3. `ReceivedArg_NoCalls_ThrowsInvalidOperationException`
4. `ReceivedArg_ArgNotFoundInLastCall_ThrowsInvalidOperationException`
5. `ReceivedArgs_MultipleCalls_ReturnsAllArgsInOrder`
6. `ReceivedArgs_NoCalls_ReturnsEmptyEnumerable`
7. `ReceivedArgs_MixedArgTypes_ReturnsOnlyMatchingType` — string/int filtering from same call
8. `ReceivedArg_CombinedWithFluentAssertions_WorksEndToEnd` — integration

**Test Results:** 144/144 passing (12 new + 132 existing). Zero regressions.

**Key Patterns & Notes:**
- **No `[AutoNSubstituteData]` for these tests:** They test NSubstitute argument-matcher infrastructure directly; using `Substitute.For<T>()` manually is clearer and avoids fixture interference with NSubstitute's argument enqueue pipeline.
- **`IDescribeNonMatches` verification:** Test 2 checks `ex.Message.ContainAny("Bob", "Alice", "Expected")` — NSubstitute calls `DescribeFor()` and includes its return in the exception message, surfacing the FA diff.
- **`TestRequest` redeclared per file:** Both test files declare their own `TestRequest` class in the local namespace to avoid cross-file coupling.
- **`ReceivedArgs<int>()` on value types:** Works correctly — NSubstitute stores boxed `object?[]` args; `arg is T typed` unboxes correctly for `int`, `bool`, etc.
- **Edge case found:** `ReceivedArgs` on a substitute with no calls returns an empty enumerable (not an exception) — verified by test 6. This asymmetry with `ReceivedArg` (which throws) is intentional and well-tested.

### Phase 15: DisposalTracker Integration Tests (2026-03-07)

**Task:** Create integration tests verifying `DisposalTracker.AddRange(values)` disposal in all four attribute types.

**Test Suite Created:**
- `DisposalTrackerIntegrationTests.cs` with 5 focused tests:
  1. `AutoNSubstituteData_SingleRow_ValueDisposedAfterTest`
  2. `InlineAutoNSubstituteData_ExplicitDisposable_Disposed`
  3. `ClassAutoNSubstituteData_AsyncDisposable_DisposedAsync`
  4. `ClassAutoNSubstituteData_MultipleRows_EachRowDisposablesTrackedAndDisposedIndependently`
  5. `FrozenParameter_RegisteredButNotDoubleDisposed`

**Edge Cases Discovered:**
- `ITheoryDataRow.GetData()` returns `object?[]` in nullable context → use `[0]!` null-forgiving operator
- Multi-row disposal: `AddRange()` called once per row inside enumeration loop; single `DisposeAsync()` call disposes all rows (LIFO)
- `TrackableDisposable` pattern preferred over NSubstitute mocks for disposal verification
- `MemberAutoNSubstituteDataAttribute` disposal structurally identical to `ClassAutoNSubstituteData` (omitted from test suite to avoid redundancy)

**Result:**
- 132/132 tests passing (127 existing + 5 new)
- No regressions
- Comprehensive disposal coverage across all four attribute types
- **CS0649 on reflection-only fields:** Harmless in test files; suppress with `#pragma` or `[SuppressMessage]` if needed
- **SpecimenRequestHelper pattern:** Static pure-method tests use plain `[Fact]`; no AutoFixture/NSubstitute needed

### Phase 12: Fixture Injection Tests (2026-03-07T17:33:43Z)

**Task:** Write tests for `AutoNSubstituteDataHelper` fixture instance injection.

**Test File Created:** `tests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataHelperFixtureInjectionTests.cs` (6 tests)

**Coverage:**
1. `Theory_IFixtureParameter_IsNotNull` — resolved value is not null
2. `Theory_IFixtureParameter_IsFixtureInstance` — resolved value is a concrete `AutoFixture.Fixture`
3. `Theory_IFixtureParameter_IsSameInstanceResolvingOtherParams` — injected fixture is same instance that resolved `[Frozen]` params
4. `Theory_ConcreteFixtureParameter_IsInjected` — concrete `Fixture` type also works
5. `InlineData_WithIFixtureParameter_InjectsFixture` — IFixture fills auto slot after explicit inline value
6. `Theory_FrozenIFixtureParameter_IsInjectedNormally` — `[Frozen]` on IFixture doesn't throw

**Test Results:** 122/122 passing (6 new + 116 existing)

**Key Pattern:** Test 3 is highest-value: proves injected fixture is the shared instance, not a new one. Uses `FixtureFactoryTests.IMyInterface` as the frozen type (no new infrastructure needed).

### Phase 13: Substitute Attribute Refactor Tests (2026-03-07T18:44:29Z)

**Task:** Verify Substitute attribute behavior after Kaylee's ParameterInfo refactor.

**Test File Updated:** `tests/Cabazure.Test.Tests/Attributes/SubstituteAttributeTests.cs`

**Changes:**
- Updated to use canonical `using AutoFixture.AutoNSubstitute` (no custom SubstituteAttribute import)
- Tests now validate that AutoFixture.AutoNSubstitute.SubstituteAttribute fires naturally via ParameterInfo resolution

**Coverage:**
1. `Theory_SubstituteAttributeNull_CreatesParameterDefault` — null parameter passes through
2. `Theory_SubstituteAttributeInterface_CreatesSubstitute` — interface type substituted
3. `Theory_SubstituteAttributeAbstractClass_CreatesSubstitute` — abstract class substituted
4. `Theory_SubstituteAttributeConcreteNoInterface_CreatesSubstitute` — concrete type (non-substitutable) substituted
5. `Theory_SubstituteAttributeConcreteSubclass_CreatesSubstitute` — concrete subclass case

**Test Results:** 127/127 passing (5 tests validate refactor; 122 existing tests unaffected)

### Phase 14: DisposalTracker Integration Tests (2026-03-07)

**Task:** Write `DisposalTrackerIntegrationTests.cs` verifying that all four data attributes call `disposalTracker.AddRange(values)` in `GetData()` so xUnit3 disposes test arguments after each test.

**Test File Created:** `tests/Cabazure.Test.Tests/Attributes/DisposalTrackerIntegrationTests.cs` (5 tests)

**Coverage:**
1. `AutoNSubstituteData_IDisposableParameter_IsDisposedWhenTrackerDisposed` — fixture-generated `IDisposable` is disposed when tracker is disposed
2. `AutoNSubstituteData_IAsyncDisposableParameter_IsAsyncDisposedWhenTrackerDisposed` — fixture-generated `IAsyncDisposable` is async-disposed when tracker is disposed
3. `AutoNSubstituteData_NonDisposableParameter_TrackerDisposeCompletesWithoutException` — non-disposable values (e.g. `string`) are silently skipped; no exception
4. `InlineAutoNSubstituteData_AutoGeneratedIDisposableParameter_IsDisposedWhenTrackerDisposed` — auto-generated IDisposable in an inline-data method is tracked correctly
5. `ClassAutoNSubstituteData_MultipleRows_EachRowDisposablesTrackedAndDisposedIndependently` — two rows each get their own `TrackableDisposable` instance; both are disposed after `DisposeAsync()`

**Test Results:** 132/132 passing (5 new + 127 existing)

**Key Patterns Established:**

- **DisposalTracker test pattern:** Test at `GetData()` level — create a `DisposalTracker`, call `attr.GetData(methodInfo, tracker)`, extract values from the returned rows via `ITheoryDataRow.GetData()[i]!`, call `tracker.DisposeAsync()`, assert disposal state. This avoids the post-test-run timing problem.
- **TrackableDisposable/TrackableAsyncDisposable helpers:** Prefer a concrete helper class over NSubstitute mock verification for disposal testing. `IsDisposed` property is far simpler to assert than `Received()` on `IDisposable.Dispose()`.
- **Reflection host class:** A `DisposalTestMethodHost` class with stub methods (body `{ }`) provides `MethodInfo` to drive `GetData()` without any test-infrastructure overhead.
- **Kaylee's fix was already applied:** All 5 tests passed immediately because `disposalTracker.AddRange(values)` was already in all four attribute implementations. The tests retroactively confirm the fix is correct.
### Phase 16: FluentArg & ReceivedCallExtensions Test Coverage (2026-03-07)

**Task:** Write comprehensive tests for Kaylee's FluentArg and ReceivedCallExtensions utilities.

**Test Files Created:**
- 	ests/Cabazure.Test.Tests/FluentArgTests.cs — tests for FluentArg.Matching<T>() matcher behavior
- 	ests/Cabazure.Test.Tests/ReceivedCallExtensionsTests.cs — tests for ReceivedArg<T>() and ReceivedArgs<T>() extraction

**Test Coverage:**
- 144/144 tests passing
- FluentArg matcher exception handling, success/failure diagnostics, integration with NSubstitute
- ReceivedArg/ReceivedArgs extraction, edge cases (no calls, no matching arg)
- IDescribeNonMatches integration with ReceivedCallsException

**Design Notes:**
1. Tests use plain Substitute.For<T>() (not [AutoNSubstituteData]) to avoid call-stack ambiguity in ArgumentMatcher.Enqueue pipeline
2. TestRequest class declared locally in each file for isolation
3. Verified asymmetry: ReceivedArg<T>() throws InvalidOperationException; ReceivedArgs<T>() returns empty enumerable (mirrors LINQ First() vs Where())

**Commit:** 411c454 (feat: add FluentArg.Matching and ReceivedCallExtensions)
