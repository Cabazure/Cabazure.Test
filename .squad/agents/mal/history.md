# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

Cabazure.Test integrates xUnit 3, NSubstitute, AutoFixture, and FluentAssertions. The two headline features are:
1. `SutFixture` — AutoFixture-backed fixture that auto-substitutes unregistered interfaces/abstract classes via NSubstitute.
2. `AutoNSubstituteDataAttribute` — xUnit 3 DataAttribute that provides Theory arguments through SutFixture.

xUnit 3 advantages we're leveraging: module initializers via `[ModuleInitializer]`, improved extensibility points over xUnit 2.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### Phase 9 — Custom Type Registration Documentation

**Date:** 2026-03-XX  
**Task:** Document Kaylee's new `TypeCustomization<T>` and `FixtureCustomizationCollection` overloads in README.

**What was added:**
- `TypeCustomization<T>` — public `ICustomization` wrapping a `Func<IFixture, T>` factory. Can be instantiated directly or subclassed for reusable customizations.
- Two new `Add()` overloads on `FixtureCustomizationCollection`:
  - `Add<T>(Func<IFixture, T> factory)` — simplest path for inline delegates
  - `Add(ISpecimenBuilder builder)` — power-user escape hatch for advanced builders

**Documentation approach:**
- Added a new **"Custom Type Registration"** subsection under the `Customizations` section in README.md
- Structured from simplest (inline delegate) to most advanced (direct `ISpecimenBuilder`)
- Included practical examples: `DateOnly`, `Order` with partial override, `Money` domain type, and builtin override pattern
- Positioned right after `JsonElementCustomization` to naturally extend the customization narrative
- Kept language concise and pragmatic — each example is self-explanatory
- Emphasized the "why" (types AutoFixture can't construct) before diving into "how"

**Key design decision honored:**
- The inline delegate pattern (`Add<T>(factory)`) is featured first as the mental entry point
- Reusable customization classes (subclass `TypeCustomization<T>`) come next as the natural evolution for shared logic
- Direct `ISpecimenBuilder` registration positioned last as the escape hatch, not the default path
- All examples align with existing README style and tone

**No changes made to code — documentation only.** Documentation merged with README updates post-Phase 9.
