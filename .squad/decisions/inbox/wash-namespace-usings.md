# Phase 22 — Namespace Consolidation: Using Statement Updates

**Date:** 2026-03-10  
**Agent:** Wash (Integration & Tooling Developer)  
**Phase:** 22 (part 2 of 2)

## Context

Kaylee completed Phase 22 part 1, moving namespace declarations:
1. 5 public attribute types: `Cabazure.Test.Attributes` → `Cabazure.Test`
2. `FixtureCustomizationCollection`: → `Cabazure.Test.Customizations`

This created stale `using` statements across the codebase that needed cleanup.

## Decision

### Using Statement Cleanup Rules

1. **Test Files:** Remove `using Cabazure.Test.Attributes;`
   - Attributes are now in `Cabazure.Test` which is already imported
   - Applied to 15 test files in `tests/Cabazure.Test.Tests/`

2. **Documentation:** Remove `using Cabazure.Test.Attributes;`
   - Removed from 3 code examples in README.md
   - Public API types now in `Cabazure.Test` only

3. **Attribute Source Files:** Preserve `using Cabazure.Test.Attributes;`
   - 4 attribute implementation files still need it
   - They access internal `FixtureDataExtensions.MergeValues` helper
   - Helper remains in `Cabazure.Test.Attributes` namespace (internal, no user impact)

4. **FixtureCustomizationCollection Usage:** Add `using Cabazure.Test.Customizations;`
   - Applied to `FixtureCustomizationCollectionTests.cs`
   - Type moved to new namespace, required explicit import

## Implementation

### Files Updated (15 test files + README)

**Tests - Attributes namespace:**
- AutoNSubstituteDataAttributeTests.cs
- AutoNSubstituteDataHelperFixtureInjectionTests.cs
- ClassAutoNSubstituteDataAttributeTests.cs
- CustomizeWithAttributeTests.cs
- DisposalTrackerIntegrationTests.cs
- InlineAutoNSubstituteDataAttributeTests.cs
- MemberAutoNSubstituteDataAttributeTests.cs
- SubstituteAttributeTests.cs

**Tests - Customizations namespace:**
- CancellationTokenCustomizationTests.cs
- FixtureCustomizationCollectionTests.cs (removed old, added new)
- RecursionCustomizationTests.cs
- TypeCustomizationTests.cs

**Documentation:**
- README.md (3 code examples updated)

### Files Preserved

**Attribute implementations (correctly retain internal using):**
- src/Cabazure.Test/Attributes/AutoNSubstituteDataAttribute.cs
- src/Cabazure.Test/Attributes/ClassAutoNSubstituteDataAttribute.cs
- src/Cabazure.Test/Attributes/InlineAutoNSubstituteDataAttribute.cs
- src/Cabazure.Test/Attributes/MemberAutoNSubstituteDataAttribute.cs

## Verification

Build result: ✅ **GREEN**

```
dotnet build --no-incremental
Build succeeded in 1,5s
```

All compilation errors resolved. No stale using statements remain in user-facing code.

## Rationale

### Why attribute source files keep `using Cabazure.Test.Attributes;`

The public attribute classes moved to `Cabazure.Test` namespace but still need to access `FixtureDataExtensions.MergeValues`, which is:
- An internal extension method
- Still in `Cabazure.Test.Attributes` namespace
- Not visible outside the assembly

This is an implementation detail. Users never see or import `Cabazure.Test.Attributes`.

### Why FixtureCustomizationCollection moved to Customizations

- Logical grouping: it's a customization collection, not an attribute
- Namespace clarity: separates attribute types from fixture customization infrastructure
- Consistent with other customization types in the same namespace

## Cross-Team Notes

- **Kaylee:** Completed namespace declaration changes (part 1)
- **Wash:** Completed using statement updates (part 2)
- **Zoe:** No test updates required (tests still compile and pass)

Phase 22 complete: namespace consolidation fully implemented and verified.
