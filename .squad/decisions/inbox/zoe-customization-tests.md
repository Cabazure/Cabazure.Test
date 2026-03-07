# Test Coverage for JsonElement and DateOnly/TimeOnly Customizations

**Proposed by:** Zoe (QA & Testing Lead)  
**Date:** 2026-03-07  
**Status:** For Review

## Decision

Test coverage for type-specific AutoFixture customizations should verify:

1. **Null guard** — `Customize(null!)` throws `ArgumentNullException`
2. **Type-specific behavior** — Created value meets type semantics (e.g., `JsonElement.ValueKind == Object`, `DateOnly` is not `MinValue`)
3. **Non-trivial randomness** — Generated values are meaningfully populated (e.g., JsonElement has properties, TimeOnly has non-zero ticks)
4. **Integration with FixtureFactory** — Customization works via `FixtureFactory.Create(customization)` or `FixtureFactory.Create()` (if in defaults)
5. **Property-on-object scenario** — Fixture can create an object that has the customized type as a property

## Rationale

**JsonElementCustomization specific:**
- `JsonElement` is a struct wrapper around `JsonDocument` — must verify the element is cloned and survives GC
- Used `GC.Collect()` + `GC.WaitForPendingFinalizers()` to test clone independence
- Verified `ValueKind` and property enumeration to ensure meaningful content

**DateOnly/TimeOnly specific:**
- FluentAssertions 7.0 does not provide comparison operators for `DateOnly`/`TimeOnly` types
- Used workarounds: `result.Year.Should().BeGreaterThan(1)` or `NotBe(MinValue)` patterns
- Multiple-value randomness check: create 3 values, assert at least one differs from `MinValue`

**General pattern:**
- Followed `ImmutableCollectionCustomizationTests.cs` structure for consistency
- Nested test classes (`HasJsonElementProperty`, `HasDateTimeOnlyProperties`) keep helper types scoped
- Clear test names describe the scenario completely (e.g., `Create_JsonElement_IsClonedAndStandalone`)

## Impact

- **Coverage:** 13 new test methods across 2 files
- **Build:** Tests compile successfully (verified with `dotnet build`)
- **Execution:** Deferred until Kaylee completes implementation

## Open Questions

None — coverage is complete for the specified requirements.
