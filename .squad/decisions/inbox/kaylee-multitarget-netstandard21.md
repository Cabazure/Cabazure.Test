# Decision: netstandard2.1 Multi-targeting Strategy

**Author:** Kaylee  
**Date:** 2026-03-07  
**Status:** Accepted

## Context

The library was targeting `net9.0` only. To support a broader consumer ecosystem (including consumers using .NET Framework 4.8, Xamarin, or Unity via netstandard2.1), multi-targeting was added.

## Decision

Target `net10.0;netstandard2.1` in `Cabazure.Test.csproj`. Test project remains `net10.0` only.

## Consequences

### Compile-time guards required
- `DateOnly`/`TimeOnly` types (added in .NET 6): `#if NET6_0_OR_GREATER` wraps entire `DateOnlyTimeOnlyCustomization.cs` and the corresponding entry in `FixtureCustomizationCollection` constructor array.
- `ArgumentNullException.ThrowIfNull` (added in .NET 6): replaced with `if (x is null) throw new ArgumentNullException(nameof(x))` across all source files.
- `TaskCompletionSource` non-generic (added in .NET 5): replaced with `TaskCompletionSource<object?>`.
- `Task.WaitAsync` (added in .NET 6): polyfilled with `#if NET6_0_OR_GREATER` / `Task.WhenAny` fallback.

### Additional package dependencies for netstandard2.1
- `System.Text.Json` 9.0.0 — JSON APIs not built into netstandard2.1.
- `Microsoft.CSharp` 4.7.0 — Required for `dynamic` dispatch used in `ImmutableCollectionCustomization`.

### Behavioral difference on netstandard2.1
`DateOnlyTimeOnlyCustomization` is not included in the default `FixtureCustomizationCollection` when built for netstandard2.1. Consumers on those platforms who need `DateOnly`/`TimeOnly` support must target a .NET 6+ framework or provide their own customization.
