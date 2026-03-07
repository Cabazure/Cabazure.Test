# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the test project `Cabazure.Test.Tests`. The unique challenge: we're testing a testing library, and our tests must use that library themselves (dogfooding). Edge cases to watch: sealed classes, value types, types without parameterless constructors, multiple constructors, `[Frozen]` parameter interaction.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### 2026-03-07: Initial Test Coverage Created

**Task:** Write comprehensive tests for SutFixture and AutoNSubstituteDataAttribute

**Test Files Created:**
- `tests/Cabazure.Test.Tests/Fixture/SutFixtureTests.cs` (15 test methods)
  - Covers: Create, CreateMany, Freeze (both overloads), Substitute
  - Test scenarios: value types, interfaces, abstract classes, concrete classes with dependencies
  - Using plain `[Fact]` attributes (can't dogfood yet since we're testing the library itself)
  
- `tests/Cabazure.Test.Tests/Attributes/AutoNSubstituteDataAttributeTests.cs` (5 test methods)
  - Covers: various parameter types, `[Frozen]` attribute interaction, multiple parameters
  - Using `[Theory, AutoNSubstituteData]` for dogfooding

**Blockers Found:**
1. Implementation has xUnit v3 API issues - `DataAttribute` base class not found
2. Missing `using AutoFixture.Kernel;` for `ISpecimenBuilder`
3. Documented in `.squad/decisions/inbox/zoe-test-gaps.md` for Kaylee

**Dependencies Updated:**
- Added AutoFixture 4.18.1, AutoFixture.AutoNSubstitute 4.18.1
- Added FluentAssertions 7.0.0, NSubstitute 5.3.0
- Corrected xUnit to use `xunit.v3` package version 3.2.2 (not `xunit` 3.x which doesn't exist)

**Edge Cases for Future Coverage:**
- Sealed classes, generic types, multiple constructors
- Types without parameterless constructors, circular dependencies
- Record types, structs with constructors, collections

**Status:** Tests written and ready. Waiting for Kaylee to fix implementation compilation errors before tests can run.

