# DateOnly/TimeOnly and JsonElement Customization Defaults

**Date:** 2026-03-07  
**Author:** Kaylee (Core .NET Developer)  
**Status:** Implemented

## Decision

`DateOnlyTimeOnlyCustomization` is added to the default seed in `FixtureCustomizationCollection`.  
`JsonElementCustomization` is opt-in only (not seeded by default).

## Rationale

**DateOnlyTimeOnlyCustomization → Default:**
- `DateOnly` and `TimeOnly` are part of the core .NET API surface (introduced .NET 6, now in .NET 9).
- AutoFixture **cannot** create `DateOnly` without a customization (throws `ArgumentOutOfRangeException`).
- `TimeOnly` technically works but produces useless values (ticks ≈ 0, always midnight).
- These types are common in modern .NET codebases (date-only fields, time-of-day properties).
- Adding this to defaults aligns with `RecursionCustomization` and `ImmutableCollectionCustomization` — all three fix AutoFixture gaps that affect broad categories of types.

**JsonElementCustomization → Opt-In:**
- `JsonElement` is a specialized type from `System.Text.Json` — not everyone uses it.
- Adding it to defaults would add `System.Text.Json` namespace references and processing to every fixture even if the project doesn't use `JsonElement`.
- Keeping it opt-in follows the principle of least surprise — users explicitly add it when they need it.

## Alternatives Considered

1. **Make both opt-in:** Rejected — `DateOnly`/`TimeOnly` are core .NET types, not optional like `JsonElement`.
2. **Make both default:** Rejected — pollutes fixtures for projects that don't use `JsonElement`.

## Implementation

- `FixtureCustomizationCollection` constructor now seeds four customizations: `AutoNSubstituteCustomization`, `RecursionCustomization`, `ImmutableCollectionCustomization`, `DateOnlyTimeOnlyCustomization`.
- `JsonElementCustomization` is documented in README with opt-in instructions.
- Users can remove `DateOnlyTimeOnlyCustomization` via `FixtureFactory.Customizations.Remove<DateOnlyTimeOnlyCustomization>()` if needed.
