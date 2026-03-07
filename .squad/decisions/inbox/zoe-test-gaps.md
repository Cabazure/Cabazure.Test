# Test Gaps Found by Zoe

**Date:** 2026-03-07
**Reporter:** Zoe (QA & Testing Lead)

## Implementation Issues Blocking Tests

The test files have been created and are ready, but the implementation has compilation errors that need to be fixed:

### 1. AutoNSubstituteDataAttribute.cs

**Error:** 
```
CS0246: The type or namespace name 'DataAttribute' could not be found
CS0641: Attribute 'AttributeUsage' is only valid on classes derived from System.Attribute
```

**Location:** `src\Cabazure.Test\Attributes\AutoNSubstituteDataAttribute.cs(32,52)`

**Issue:** In xUnit v3 (xunit.v3 3.2.2), the `DataAttribute` base class may be in a different namespace or have a different API than xUnit v2. The using statements currently have:
```csharp
using Xunit;
using Xunit.Sdk;
```

**Recommendation:** Check xUnit v3 documentation for the correct base class and namespace for theory data attributes.

### 2. SutFixture.cs

**Error:**
```
CS0246: The type or namespace name 'ISpecimenBuilder' could not be found
```

**Location:** `src\Cabazure.Test\Fixture\SutFixture.cs(111,41)`

**Issue:** `ISpecimenBuilder` is from AutoFixture but may need an explicit using statement:
```csharp
using AutoFixture.Kernel;
```

**Recommendation:** Add the missing using directive.

## Test Files Created

✅ **tests\Cabazure.Test.Tests\Fixture\SutFixtureTests.cs** (15 test methods)
- Coverage includes: Create, CreateMany, Freeze (both overloads), Substitute, value types, interfaces, abstract classes, concrete classes with dependencies

✅ **tests\Cabazure.Test.Tests\Attributes\AutoNSubstituteDataAttributeTests.cs** (5 test methods)
- Coverage includes: string arguments, int arguments, interface substitutes, frozen parameters, multiple parameters

## Next Steps

1. Kaylee should fix the compilation errors in the implementation
2. Once implementation compiles, Zoe should run tests and verify they pass
3. Any additional edge cases discovered during test runs should be added

## Edge Cases to Add Later

These aren't blocking, but should be added once basic tests pass:

- Sealed classes
- Types with multiple constructors
- Types without parameterless constructors
- Circular dependencies
- Generic types
- Collections
- Nested classes
- Record types
- Structs with constructors
