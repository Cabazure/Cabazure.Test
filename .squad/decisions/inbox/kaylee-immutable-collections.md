# Findings: ImmutableCollectionCustomization

**Date:** 2026-03-07  
**Author:** Kaylee (Core Dev)

## Was the customization actually needed?

**Yes — confirmed via ad-hoc test against AutoFixture 4.18.1.**

| Type | Without Customization |
|---|---|
| `ImmutableList<T>` | `ObjectCreationExceptionWithPath` (throws) |
| `ImmutableArray<T>` | `ObjectCreationExceptionWithPath` (throws) |
| `ImmutableHashSet<T>` | `ObjectCreationExceptionWithPath` (throws) |
| `ImmutableDictionary<TKey,TValue>` | `ObjectCreationExceptionWithPath` (throws) |
| `ImmutableSortedSet<T>` | (not tested directly, assumed same as HashSet) |
| `ImmutableSortedDictionary<TKey,TValue>` | (not tested directly, assumed same as Dictionary) |
| `ImmutableQueue<T>` | Created but **empty** (0 items) |
| `ImmutableStack<T>` | Created but **empty** (0 items) |

The customization is essential for the first four types (throws without it) and improves correctness for the last two (populated instead of empty).

## Was the PropertyInfo fix confirmed to matter?

**Yes — the fix is critical.** The `GetRequestType` switch in the reference implementation only handled `ParameterInfo` and `Type`. AutoFixture also sends `PropertyInfo` requests when resolving object properties through its reflection-based pipeline. Without the `PropertyInfo pi => pi.PropertyType` arm, a class like:

```csharp
public class MyModel
{
    public ImmutableList<string> Tags { get; set; } = ImmutableList<string>.Empty;
}
```

…would be created successfully but `Tags` would remain empty (`ImmutableList<string>.Empty`). The property-population test (`Customize_PopulatesProperty_OnObjectWithImmutableListProperty`) confirms this path is exercised and passing.

The `FieldInfo fi => fi.FieldType` arm was also added for completeness (public fields follow the same pattern).

## Did ImmutableQueue/Stack work with dynamic dispatch?

**Yes — both work perfectly.** `ImmutableQueue.CreateRange(dynamic)` and `ImmutableStack.CreateRange(dynamic)` resolve correctly at runtime without any cast or reflection workaround. The DLR handles the generic method dispatch cleanly. No special treatment needed compared to the other collection types.

## Summary

All eight immutable collection types are now supported. The `PropertyInfo`/`FieldInfo` fix is the most impactful correctness improvement over the reference implementation — without it, the customization would silently fail for any immutable collection stored as an object property rather than a constructor parameter.
