# Decision: Phase 22 Namespace Consolidation (Part 1)

**Date:** 2026-03-08  
**Status:** ✅ Implemented  
**Owner:** Kaylee (Core .NET Developer)

## Context

Test authors currently need two imports:
```csharp
using Cabazure.Test;
using Cabazure.Test.Attributes;
```

Goal: Make `using Cabazure.Test;` sufficient for all test authoring needs.

## Decision

### Part A: Public Attributes → Cabazure.Test

Changed namespace from `Cabazure.Test.Attributes` to `Cabazure.Test` in 5 public attribute types:
- `AutoNSubstituteDataAttribute`
- `InlineAutoNSubstituteDataAttribute`
- `MemberAutoNSubstituteDataAttribute`
- `ClassAutoNSubstituteDataAttribute`
- `CustomizeWithAttribute`

**Physical location unchanged:** Files remain in `src/Cabazure.Test/Attributes/` folder.

**Internal helper preserved:** `FixtureDataExtensions` stays in `Cabazure.Test.Attributes` namespace (internal, no user impact). The 5 public attributes explicitly import `using Cabazure.Test.Attributes;` to access the `MergeValues` extension method.

### Part B: FixtureCustomizationCollection → Cabazure.Test.Customizations

Moved `FixtureCustomizationCollection`:
- **From:** `src/Cabazure.Test/FixtureCustomizationCollection.cs` (namespace `Cabazure.Test`)
- **To:** `src/Cabazure.Test/Customizations/FixtureCustomizationCollection.cs` (namespace `Cabazure.Test.Customizations`)

**Rationale:** `FixtureCustomizationCollection` is accessed via `FixtureFactory.Customizations` from module initializers, not directly in test methods. Moving it to `Customizations` reduces root-level namespace clutter.

**Impact:** Updated `FixtureFactory.cs` using statement from `Cabazure.Test.Attributes` to `Cabazure.Test.Customizations`.

## Result

- **src project:** Builds cleanly ✅
- **Test projects:** Will require using statement updates in Part 2 (expected)
- **User API:** Cleaner — single import for test authoring

## Files Changed

1. `src/Cabazure.Test/Attributes/AutoNSubstituteDataAttribute.cs` — namespace + using
2. `src/Cabazure.Test/Attributes/InlineAutoNSubstituteDataAttribute.cs` — namespace + using
3. `src/Cabazure.Test/Attributes/MemberAutoNSubstituteDataAttribute.cs` — namespace + using
4. `src/Cabazure.Test/Attributes/ClassAutoNSubstituteDataAttribute.cs` — namespace + using
5. `src/Cabazure.Test/Attributes/CustomizeWithAttribute.cs` — namespace + using
6. `src/Cabazure.Test/Customizations/FixtureCustomizationCollection.cs` — moved + namespace
7. `src/Cabazure.Test/FixtureFactory.cs` — using statement updated
8. `src/Cabazure.Test/FixtureCustomizationCollection.cs` — deleted (moved)

## Follow-up

Part 2: Update test project using statements (owner TBD).
