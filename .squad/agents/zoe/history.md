# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain: test project `Cabazure.Test.Tests`. Unique challenge: testing a testing library using that library itself (dogfooding). Watch edge cases: sealed classes, value types, parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

**Completed Work Summary (Phases 1-18):**

Full test coverage for library features: FixtureFactory, 4 data attributes, 8 customizations, NSubstitute integration (argument matchers + async call waiting). 152+ tests passing. Infrastructure patterns: sealed-class composition, static collection restoration, FluentAssertions property checks, same-instance assertions with [Frozen] + Create<T>, dogfooding theory tests, JsonElement cloning, namespace collision workarounds.

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
