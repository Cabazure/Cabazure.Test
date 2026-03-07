# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain: test project `Cabazure.Test.Tests`. Unique challenge: testing a testing library using that library itself (dogfooding). Watch edge cases: sealed classes, value types, parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

**Completed Work Summary (Phases 1-18, plus Phase 16 FrozenAttribute migration):**

Full test coverage for library features: FixtureFactory, 4 data attributes, 8 customizations, NSubstitute integration (argument matchers + async call waiting). 165 tests passing. Infrastructure patterns: sealed-class composition, static collection restoration, FluentAssertions property checks, same-instance assertions with [Frozen] + Create<T>, dogfooding theory tests, JsonElement cloning, namespace collision workarounds.

## Recent Work

### Phases 1-13: Foundation & Customizations (2026-03-07)

**Test Coverage:** FixtureFactory (15), 4 data attributes, 5 customizations (15 each), SpecimenRequestHelper (5), CancellationToken (5), fixture injection (6), registry (5), stacking (8). Total: 122+ passing by Phase 12.

**Key Patterns:** Sealed-class composition, static restoration with try/finally, FluentAssertions limits (DateOnly/TimeOnly), same-instance assertions with [Frozen]+Create<T>, dogfooding theory tests, JsonElement cloning, namespace collision workarounds.

### Phase 12: Fixture Injection Tests (2026-03-07T17:33:43Z)

**Task:** Write tests for fixture instance injection.

**Coverage:** IFixture parameter injection, same-instance assertion, concrete Fixture type, InlineData mixing, [Frozen] handling. Result: 122/122 passing (6 new).

### Phase 13: Substitute Refactor & DisposalTracker (2026-03-07)

**Substitute Refactor:** Verified behavior after Kaylee's ParameterInfo refactor — all tests passing with new CreateValue(ParameterInfo) signature.

**DisposalTracker:** Created integration tests verifying disposal across all four attribute types (5 new tests). Pattern: TrackableDisposable for verification. Result: 127/127 passing.

### Phases 14-18: NSubstitute Advanced Features & Protected Methods (2026-03-07)

**Phase 14 (ProtectedMethodExtensions):**
- `InvokeProtected<TResult>` and async variants for protected method invocation  
- ExceptionDispatchInfo unwrapping, two-stage overload resolution, BindingFlags strategy
- Nested private fixture classes (ProtectedMethodBase, ProtectedMethodTarget, AsyncTarget, ThrowingTarget, OverloadedTarget)
- 162 tests passing; Zoe created implementation as squad unblock (Kaylee's design validated)

**Phase 16 (Argument Matchers):**
- FluentArg.Matching<T> — FluentAssertions-backed NSubstitute matcher
- ReceivedCallExtensions — ReceivedArg<T> (last) / ReceivedArgs<T> (all)
- Manual Substitute.For<T>() (avoid fixture interference with argument enqueue)
- TestRequest redeclared per file (isolation)
- 144 tests passing

**Phase 18 (WaitForReceivedExtensions):**
- WaitForReceived<T>() and WaitForReceivedWithAnyArgs<T>() for async call waiting
- SignalingCallHandler with TaskCompletionSource, race-free detection, xUnit 3 CancellationToken
- Fire-and-forget Task.Run() for delayed calls, try/finally DefaultTimeout mutation
- .CompleteWithinAsync(TimeSpan) and .ThrowAsync<TException>() patterns
- 152 tests passing

### Phase 16 (FrozenAttribute Migration — 2026-03-07)

**Task:** Migrate test files from custom `Cabazure.Test.Attributes.FrozenAttribute` to `AutoFixture.Xunit3.FrozenAttribute`.

**Files Updated (7 total):**
- `Attributes/AutoNSubstituteDataAttributeTests.cs`
- `Attributes/InlineAutoNSubstituteDataAttributeTests.cs`
- `Attributes/ClassAutoNSubstituteDataAttributeTests.cs`
- `Attributes/MemberAutoNSubstituteDataAttributeTests.cs`
- `Attributes/AutoNSubstituteDataHelperFixtureInjectionTests.cs`
- `Attributes/SubstituteAttributeTests.cs`
- `Customizations/TypeCustomizationTests.cs`

**Key Learnings:**
- Task listed 4 files but 7 files total use `[Frozen]` — always grep the full test tree for `\[Frozen\]`.
- The custom `FrozenAttribute` was still present in `Cabazure.Test.Attributes` when tests were updated, causing CS0104 ambiguity. Resolved with `using FrozenAttribute = AutoFixture.Xunit3.FrozenAttribute;` alias — takes precedence over namespace imports and survives until the custom attribute is deleted.
- Added explicit `<PackageReference Include="AutoFixture.Xunit3" Version="4.19.0" />` to csproj.
- Result: 165/165 tests passing.

## Learnings

### Phase 16 FrozenAttribute Test Update (Re-verified)

- All Phase 16 FrozenAttribute migration changes were already applied in a previous session. When asked to redo work, always check the current file state before modifying.
- `AutoFixture.Xunit3` 4.19.0 is already present in the test csproj; the type alias pattern (`using FrozenAttribute = AutoFixture.Xunit3.FrozenAttribute;`) is already in all 4 attribute test files (and 3 others).
- 165/165 tests confirmed passing on re-verification.

### Phase 16 (FrozenAttribute Migration - 2026-03-07T21:44:11Z)

Task: Migrate 7 test files from custom Cabazure.Test.Attributes.FrozenAttribute to AutoFixture.Xunit3.FrozenAttribute.

Implementation:
- Applied type alias pattern to 7 files
- Full grep verification of [Frozen] coverage completed
- AutoFixture.Xunit3 4.19.0 already present in Cabazure.Test.Tests.csproj

Pattern Decision: Type alias avoids CS0104 ambiguity during transition; self-documents intent; works both during and after cleanup.

Test Result: 165/165 passing; no regressions.

Cross-team: Kaylee refactored source code first (agent-51); Wash updated documentation (agent-53).

Decision logged: .squad/decisions.md - Phase 16 decision with implementation details
