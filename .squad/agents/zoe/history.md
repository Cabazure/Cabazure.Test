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

### Phase 17: FluentAssertions Extensions Tests (2026-03-07)

**Task:** Write comprehensive tests for two new FluentAssertions extension files being implemented by Kaylee.

**Files Created:**
- `tests/Cabazure.Test.Tests/Assertions/JsonElementAssertionsTests.cs` (10 tests)
- `tests/Cabazure.Test.Tests/Assertions/DateTimeOffsetExtensionsTests.cs` (9 tests)

**Coverage:**

JsonElementAssertions (10 tests):
- Identity/equivalence checks with identical elements
- Difference detection with appropriate error messages
- Structure mismatch detection
- String overload with matching/different JSON
- Null value handling
- Empty object handling
- Array comparison with order sensitivity
- Array order difference detection
- Invalid JSON string throws JsonException

DateTimeOffsetExtensions (9 tests):
- Default precision (1s) pass/fail scenarios
- Custom int millisecond precision pass/fail scenarios
- NotBeCloseTo variants for both default and custom precision
- Custom default precision via CabazureAssertionOptions with proper cleanup (try/finally)

**Key Patterns:**
- Used `Action act = () => ...` with `.Should().Throw<Exception>()` for failure tests
- JsonElement test setup uses `JsonDocument.Parse().RootElement.Clone()` for proper value isolation
- Static property mutation test includes try/finally to prevent test pollution
- Tests follow existing project style: namespace `Cabazure.Test.Tests.Assertions`, minimal comments, AAA pattern

**Test Result:** ✅ 19/19 passing — Kaylee's implementation complete, verified. Build clean.

**Cross-team:** Kaylee implemented both extension modules; Wash added comprehensive README documentation.
**Decision logged:** `.squad/decisions.md` — Phase 17 FluentAssertions Extensions test patterns and verification.

### Phase 19: StringContentExtensions Tests (2026-03-07)

**Task:** Write tests for `StringContentExtensions` — FluentAssertions extension methods on `StringAssertions`.

**File created/augmented:** `tests/Cabazure.Test.Tests/Assertions/StringContentExtensionsTests.cs`

**Coverage (19 tests across 6 method pairs):**

BeSimilarTo (5):
- `BeSimilarTo_WithIdenticalContent_Passes`
- `BeSimilarTo_WithSameContentDifferentWhitespace_Passes` (pre-existing, "\n" + extra spaces)
- `BeSimilarTo_WithLeadingAndTrailingWhitespace_Passes`
- `BeSimilarTo_WithDifferentLineEndings_Passes`
- `BeSimilarTo_WithDifferentContent_ThrowsWithMessage` — checks both values in message

NotBeSimilarTo (2):
- `NotBeSimilarTo_WithDifferentContent_Passes`
- `NotBeSimilarTo_WithSameContentDifferentWhitespace_Throws`

BeXmlEquivalentTo (4):
- `BeXmlEquivalentTo_WithIdenticalXml_Passes`
- `BeXmlEquivalentTo_WithSameXmlDifferentFormatting_Passes`
- `BeXmlEquivalentTo_WithDifferentXmlContent_ThrowsWithMessage`
- `BeXmlEquivalentTo_WithInvalidXml_ThrowsXmlException` — propagation of `XmlException`

NotBeXmlEquivalentTo (2):
- `NotBeXmlEquivalentTo_WithDifferentXmlContent_Passes`
- `NotBeXmlEquivalentTo_WithSameXmlDifferentFormatting_Throws`

BeJsonEquivalentTo (4):
- `BeJsonEquivalentTo_WithIdenticalJson_Passes`
- `BeJsonEquivalentTo_WithSameJsonDifferentFormatting_Passes`
- `BeJsonEquivalentTo_WithDifferentJsonContent_ThrowsWithMessage`
- `BeJsonEquivalentTo_WithInvalidJson_ThrowsJsonException` — propagation of `JsonException`

NotBeJsonEquivalentTo (2):
- `NotBeJsonEquivalentTo_WithDifferentJsonContent_Passes`
- `NotBeJsonEquivalentTo_WithSameJsonDifferentFormatting_Throws`

**Key Patterns:**
- File already existed with 12 tests; augmented with 7 additional to reach full spec coverage.
- Added `using System.Text.Json;` and `using System.Xml;` to existing usings.
- Exception propagation tests (`XmlException`, `JsonException`) check the unwrapped type directly — the implementation should not swallow parse errors.
- `[Fact]` throughout — no `[AutoNSubstituteData]` needed for pure string assertions.
- All failure-message tests assert both the actual and expected values appear in the exception message.


### Phase 23: JsonElementEquivalencyStep Tests (2026-03-07)

**Task:** Write comprehensive tests for JsonElementEquivalencyStep and JsonElementEquivalencyExtensions.

**File Created:** 	ests/Cabazure.Test.Tests/Assertions/JsonElementEquivalencyStepTests.cs (5 tests)

**Coverage:**

1. UsingJsonElementComparison_WithIdenticalDtoJsonElements_Passes — identical JSON in both DTOs passes
2. UsingJsonElementComparison_WithSemanticallyEqualButDifferentlyFormattedJson_Passes — whitespace-different JSON normalized and passes
3. UsingJsonElementComparison_WithDifferentJsonValues_Throws — genuinely different JSON throws
4. BeEquivalentTo_WithoutUsingJsonElementComparison_AndDifferentlyFormattedJson_Throws — regression/documentation: without the step, differently-formatted JSON fails structural comparison; documents WHY the feature exists
5. UsingJsonElementComparison_WhenJsonDiffers_FailureMessageContainsBothJsonStrings — failure message includes both the expected and actual JSON strings

**Skipped (per design decision):**
- Direct JsonElement.Should().BeEquivalentTo() test: JsonElement.Should() resolves to JsonElementAssertions, not FA's generic ObjectAssertions.BeEquivalentTo. Covered by JsonElementAssertionsTests.cs.
- Global AssertionOptions.AssertEquivalencyUsing registration test: FA 7.0.0 has no simple "reset to default"; global state isolation is risky. Exercised via user's module initializer in real usage.

**Key Patterns:**
- All tests use [Fact] — no auto-generated data needed for pure assertion logic
- Private TestDto nested class with string Name and JsonElement Data as test fixture
- Failure path tests use Action act = () => ... then ct.Should().Throw<Exception>()
- Message assertions use .Which.Message.Should().Contain(...) (consistent with JsonElementAssertionsTests.cs)
- No RootElement.Clone() needed since DTOs hold values by value type (JsonElement is a struct)

**Test Result:** ✅ 208/208 passing — 203 existing + 5 new. No regressions.

### Phase 24: EmptyObjectEquivalencyStep Tests (2026-03-07)

**Task:** Write comprehensive tests for EmptyObjectEquivalencyStep and AllowingEmptyObjects() extension method.

**File Created:** `tests/Cabazure.Test.Tests/Assertions/EmptyObjectEquivalencyStepTests.cs` (5 tests)

**Coverage:**

1. BeEquivalentTo_WithAllowingEmptyObjects_PassesForEmptyType — empty type (no properties/fields) passes comparison with AllowingEmptyObjects()
2. BeEquivalentTo_WithAllowingEmptyObjects_PassesForNonEmptyTypeWithMatchingValues — non-empty type with matching properties still works correctly (step doesn't break normal types)
3. BeEquivalentTo_WithAllowingEmptyObjects_ThrowsForNonEmptyTypeWithDifferentValues — non-empty type with differing values still fails as expected
4. BeEquivalentTo_WithoutAllowingEmptyObjects_ThrowsForEmptyType — regression test: without the step, empty type throws InvalidOperationException
5. BeEquivalentTo_WithAllowingEmptyObjects_HandlesNullExpectation — null expectation doesn't cause NullReferenceException (falls through to FA's null handling)

**Key Patterns:**
- All tests use [Fact] — no auto-generated data needed for equivalency step logic
- Private nested test DTO classes (EmptyDto with no members, DtoWithProperty with one property)
- Positive tests use `var act = () => ...` then `act.Should().NotThrow()`
- Negative tests use `Action act = () => ...` then `act.Should().Throw<Exception>()`
- Regression test documents WHY the feature exists (empty types throw InvalidOperationException in FA without this step)
- Pattern matches JsonElementEquivalencyStepTests.cs from Phase 23

**Test Result:** ✅ 213/213 passing — 208 existing + 5 new. No regressions.

### Phase 25: TheoryData<T1, T2> Regression Tests (2026-03-07)

**Task:** Add regression tests for `TheoryData<T1, T2>` data source unpacking fix in both `ClassAutoNSubstituteDataAttribute` and `MemberAutoNSubstituteDataAttribute`.

**Background:** Bug fixed where data sources returning `TheoryData<T1, T2>` (strongly-typed xUnit 3 rows) would fail with ArgumentException: "Object of type 'TheoryDataRow`2[T1,T2]' cannot be converted to type 'T1'". Fix calls `ITheoryDataRow.GetData()` to unpack values before forwarding.

**Files Modified:**
1. `tests/Cabazure.Test.Tests/Attributes/ClassAutoNSubstituteDataAttributeTests.cs`:
   - Added `TypedTheoryData : TheoryData<string, int>` class yielding `("hello", 1)` and `("world", 2)`
   - Added test `ClassData_TheoryDataRows_AreUnpackedCorrectly` verifying unpacking with auto-generated substitute

2. `tests/Cabazure.Test.Tests/Attributes/MemberAutoNSubstituteDataAttributeTests.cs`:
   - Added static property `TypedRows : TheoryData<string, int>` yielding `("hello", 1)` and `("world", 2)`
   - Added test `MemberProperty_TheoryDataRows_AreUnpackedCorrectly` verifying unpacking with auto-generated substitute

**Key Patterns:**
- `TheoryData<T1, T2>` constructor-based initialization with `Add(T1, T2)` calls (ClassData) vs collection initializer syntax (MemberData)
- Both tests verify typed data columns (`string message`, `int count`) AND auto-generated substitute (`IMyInterface service`)
- Tests match existing style: `BeOneOf()` for data assertions, `NotBeNull()` for substitute assertions
- Regression coverage: ensures `ITheoryDataRow.GetData()` is called by attributes to unpack strongly-typed rows

**Test Result:** ✅ 217/217 passing — 213 existing + 2 new (ClassData + MemberData). No regressions.

### Phase 26: TheoryDataRow Unwrapping Fix (2026-03-08T08:06:15Z)

**Task:** Write comprehensive regression tests validating TheoryDataRow unwrapping fix for both `ClassAutoNSubstituteDataAttribute` and `MemberAutoNSubstituteDataAttribute`.

**Implementation:**

1. `tests/Cabazure.Test.Tests/Attributes/ClassAutoNSubstituteDataAttributeTests.cs`:
   - Added `TypedTheoryData : TheoryData<int>` data class yielding three test rows (1, 2, 3)
   - Added test `ClassData_WithTypedTheoryDataRows_UnpacksCorrectly` verifying individual rows are unpacked and passed separately to test methods
   - Validates row isolation: each row invokes test method independently with auto-generated substitute

2. `tests/Cabazure.Test.Tests/Attributes/MemberAutoNSubstituteDataAttributeTests.cs`:
   - Added static property `TypedRows : TheoryData<string>` yielding test rows ("alpha", "beta", "gamma")
   - Added test `MemberProperty_WithTypedTheoryDataRows_UnpacksCorrectly` verifying member-sourced rows also unpack correctly
   - Ensures consistency between class-sourced and member-sourced data paths

**Key Patterns:**
- `TheoryData<T>` constructor-based initialization with Add(T) calls
- Tests verify both typed data columns AND auto-generated substitutes alongside them
- Row isolation verified: each test invocation receives its specific row value
- Sealed-class composition pattern used for test data isolation
- Regression coverage: ensures `ITheoryDataRow.GetData()` is called by attributes

**Test Result:** ✅ 217/217 passing. No regressions.

**Cross-team:** Kaylee (Agent 74) implemented unwrapping logic in ClassAutoNSubstituteDataAttribute and MemberAutoNSubstituteDataAttribute.ToRows()
