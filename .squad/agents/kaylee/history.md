# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is the guts of the library: `SutFixture`, AutoFixture customizations, the `ISpecimenBuilder` that routes interface/abstract-class requests to NSubstitute. Key challenge: AutoFixture doesn't natively create substitutes for abstract/interface types; we bridge that via `AutoNSubstituteCustomization`.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### 2026-03-07: Core Library Implementation

**AutoFixture + NSubstitute Integration:**
- `AutoNSubstituteCustomization` wraps `AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization` with `ConfigureMembers=true` and `GenerateDelegates=true` to ensure interfaces/abstract classes are automatically substituted
- The key insight: AutoFixture's specimen pipeline needs explicit customization to route abstract/interface requests to NSubstitute's `Substitute.For<T>()`
- Without customization, AutoFixture throws when it can't construct interfaces/abstract types

**SutFixture Design:**
- Wraps `IFixture` to provide a clean API: `Create<T>()`, `Freeze<T>()`, `Freeze<T>(instance)`, `Customize<T>()`, `Substitute<T>()`
- Two `Freeze` overloads: parameterless creates+registers, parameter-based registers existing instance
- `Substitute<T>()` creates a one-off substitute WITHOUT fixture registration (useful for test-specific mocks)
- Constructor accepts `params ICustomization[]` to allow extensibility beyond NSubstitute

**xUnit 3 DataAttribute Pattern:**
- xUnit 3 still supports `DataAttribute` base class with `GetData(MethodInfo)` returning `IEnumerable<object[]>`
- `AutoNSubstituteDataAttribute` uses reflection to:
  1. Instantiate `SutFixture` per test method invocation
  2. Resolve parameters left-to-right
  3. Freeze parameters marked with `[Frozen]` AFTER creation so subsequent params get the frozen instance
- Edge case: `Freeze<T>(instance)` requires finding the right overload via reflection (1-param, generic parameter type)

**Module Initializer:**
- `[ModuleInitializer]` runs before any test execution, perfect for global xUnit 3 config
- Currently empty (reserved for future use) but establishes the pattern
- Avoids static constructor ordering issues in test runners

### Phase 7: User-Defined Fixture Customizations

**SutFixtureCustomizations (global registry):**
- `static class SutFixtureCustomizations` in `Customizations/` namespace — thread-safe via `lock` on a private `object _lock`
- `Add(ICustomization)` is the public surface; `All` is `internal` returning a snapshot copy so callers can't mutate the list
- Users call `Add()` from `[ModuleInitializer]` for assembly-wide effect

**CustomizeWithAttribute:**
- `AllowMultiple = true` on `Method | Class` targets; `internal Instantiate()` validates type + parameterless ctor before calling `Activator.CreateInstance`
- Two validation checks produce `InvalidOperationException` with clear diagnostic messages: (1) does not implement `ICustomization`, (2) no public parameterless constructor

**CreateFixture layering order (critical):**
1. `AutoNSubstituteCustomization` — always first so NSubstitute is always the fallback
2. `SutFixtureCustomizations.All` — project-wide registrations
3. `[CustomizeWith]` on the test **method** — method-level overrides
4. `[CustomizeWith]` on the test **class** — class-level defaults (applied last; later = higher priority in AutoFixture)
- Note: class-level is applied AFTER method-level, which is intentional — class attributes act as defaults that can be overridden per-method if a later customization wins

**All 4 data attributes updated:**
- `AutoNSubstituteDataAttribute`, `InlineAutoNSubstituteDataAttribute`, `MemberAutoNSubstituteDataAttribute`, `ClassAutoNSubstituteDataAttribute` all replaced `new SutFixture()` with `AutoNSubstituteDataHelper.CreateFixture(testMethod)`
- Per-row fixture creation in `Member` and `Class` variants also updated — each row gets its own fully-customized fixture (no cross-row state leakage)
