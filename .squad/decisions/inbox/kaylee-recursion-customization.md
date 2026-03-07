# Decision: RecursionCustomization Design

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)  
**Status:** Proposed

## Decision

Added `RecursionCustomization` to `Cabazure.Test.Customizations` as a `public sealed class` that follows the same style as `AutoNSubstituteCustomization`.

## Rationale

- `public sealed` is the consistent pattern for all customization classes in this library.
- `ArgumentNullException.ThrowIfNull(fixture)` is used instead of null-conditional `?.` — silently ignoring a null fixture hides bugs and is inconsistent with `AutoNSubstituteCustomization`.
- The core AutoFixture pattern (remove `ThrowingRecursionBehavior`, add `OmitOnRecursionBehavior`) is the standard recommended approach for handling recursive object graphs.
- `OmitOnRecursionBehavior` leaves recursive properties as `null` rather than throwing, which is the most test-friendly default.

## Namespace Collision Pitfall

In the test project, `Fixture` is both a class (`AutoFixture.Fixture`) and a namespace remnant. When writing tests that construct `new Fixture()` directly (without going through `FixtureFactory`), use the fully qualified name `new AutoFixture.Fixture()` to avoid CS0118 ambiguity errors.
