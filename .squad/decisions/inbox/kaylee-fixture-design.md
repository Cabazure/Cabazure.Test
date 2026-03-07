# Design Decisions: SutFixture Core Implementation

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Implemented

## Context

Implementing the core `SutFixture` class and supporting infrastructure for Cabazure.Test, which combines AutoFixture and NSubstitute for automatic test dependency injection.

## Key Decisions

### 1. Two `Freeze<T>` Overloads

**Decision:** Provide both `Freeze<T>()` and `Freeze<T>(instance)`.

**Rationale:**
- `Freeze<T>()`: Delegates to AutoFixture's `Freeze<T>()` — creates and registers in one call
- `Freeze<T>(instance)`: Uses `Inject(instance)` to register a user-provided instance
- This mirrors the flexibility of raw AutoFixture while maintaining a clean API
- Common pattern: create a configured substitute, then freeze it for later injection

**Alternative Considered:** Single method with optional parameter — rejected because it complicates generic type inference and forces nullable reference handling.

### 2. `Substitute<T>()` Does NOT Auto-Register

**Decision:** `Substitute<T>()` creates a substitute but does NOT call `Freeze` internally.

**Rationale:**
- Separation of concerns: creation vs registration
- Allows test-specific one-off mocks without polluting the fixture
- If auto-registration is desired, users can chain: `fixture.Freeze(fixture.Substitute<IFoo>())`
- More predictable behavior — explicit registration is clearer

**Alternative Considered:** Auto-freeze every substitute — rejected because it prevents creating multiple distinct substitutes of the same type.

### 3. Constructor Accepts `params ICustomization[]`

**Decision:** Allow users to pass custom AutoFixture customizations to the constructor.

**Rationale:**
- Extensibility: teams may have domain-specific specimen builders or conventions
- Defaults to `AutoNSubstituteCustomization`, but doesn't force it
- Example use case: custom `DateTime` generation, specific string formats, or domain value objects
- Follows AutoFixture's design philosophy of composable customizations

**Alternative Considered:** Sealed with only NSubstitute — rejected because it limits advanced scenarios and goes against AutoFixture's extensibility model.

### 4. xUnit 3 Attribute Uses Reflection for Freezing

**Decision:** `AutoNSubstituteDataAttribute` uses reflection to call generic `Freeze<T>(instance)` method.

**Rationale:**
- Test method parameters are `ParameterInfo[]` with runtime `Type`, not compile-time generics
- Reflection via `MakeGenericMethod` is the only way to invoke `Freeze<T>` with a runtime type
- Performance impact is negligible (once per test method invocation)
- Edge case handling: find the overload with 1 generic parameter (not the parameterless one)

**Alternative Considered:** Non-generic `Inject(Type, object)` — rejected because it would deviate from AutoFixture's strongly-typed API and require wrapping the fixture differently.

### 5. Left-to-Right Freeze Semantics

**Decision:** Parameters marked `[Frozen]` are frozen AFTER creation, so subsequent parameters in the SAME test method get the frozen instance.

**Rationale:**
- Intuitive for users: "freeze this, then use it later in the parameter list"
- Matches AutoFixture.Xunit2 behavior (de facto standard)
- Example: `Test([Frozen] IFoo foo, MyClass sut)` — `sut` constructor gets the same `foo`

**Edge Case:** If two parameters have `[Frozen]` on the same type, the LAST one wins (overwrites). Documented as left-to-right processing.

## Implementation Notes

- `AutoNSubstituteCustomization` wraps the AutoFixture.AutoNSubstitute package's customization with `ConfigureMembers=true` and `GenerateDelegates=true` for maximum auto-mocking coverage
- `AssemblyInitializer` uses `[ModuleInitializer]` (C# 9+) to run before xUnit 3 test discovery — currently a no-op but reserves the pattern for future global config
- All public APIs include XML doc comments with `<summary>`, `<remarks>`, `<param>`, `<returns>`, and `<exception>` tags

## Open Questions

- **xUnit 3 availability:** The project specifies xUnit 3.2.0, which doesn't exist on NuGet yet (latest is 2.9.3). The code is written for xUnit 3's API surface. Blocked on xUnit 3 release.

## Related Files

- `src/Cabazure.Test/Fixture/SutFixture.cs`
- `src/Cabazure.Test/Customizations/AutoNSubstituteCustomization.cs`
- `src/Cabazure.Test/Attributes/AutoNSubstituteDataAttribute.cs`
- `src/Cabazure.Test/Attributes/FrozenAttribute.cs`
- `src/Cabazure.Test/Fixture/AssemblyInitializer.cs`
