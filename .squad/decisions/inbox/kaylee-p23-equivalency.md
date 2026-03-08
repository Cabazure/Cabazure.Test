# Phase 23: JsonElementEquivalencyStep Design Decisions

**Author:** Kaylee  
**Date:** 2026-03-08  
**Files created:**
- `src/Cabazure.Test/Assertions/JsonElementEquivalencyStep.cs`
- `src/Cabazure.Test/Assertions/JsonElementEquivalencyExtensions.cs`

---

## Decision 1: Exact FA 7.0.0 types confirmed via ILSpy decompilation

All types are in `FluentAssertions.Equivalency` namespace unless noted:

| Type | Kind | Notes |
|------|------|-------|
| `IEquivalencyStep` | interface | `Handle(Comparands, IEquivalencyValidationContext, IEquivalencyValidator) → EquivalencyResult` |
| `EquivalencyResult` | enum | Values: `ContinueWithNext`, `AssertionCompleted` |
| `Comparands` | class | `.Subject` and `.Expectation` are both `object?` |
| `IEquivalencyValidationContext` | interface | `.Reason` is `FluentAssertions.Execution.Reason` |
| `Reason` | class (in `FluentAssertions.Execution`) | `.FormattedMessage` (string), `.Arguments` (object[]) |
| `SelfReferenceEquivalencyAssertionOptions<TSelf>` | class | Has `Using(IEquivalencyStep) → TSelf` |
| `EquivalencyAssertionOptions<T>` | class | Extends `SelfReferenceEquivalencyAssertionOptions<EquivalencyAssertionOptions<T>>` |
| `EquivalencyAssertionOptions` | class | Extends `SelfReferenceEquivalencyAssertionOptions<EquivalencyAssertionOptions>` |

## Decision 2: `Handle()` implementation uses pattern matching on both comparands

```csharp
if (comparands.Subject is not JsonElement subject
    || comparands.Expectation is not JsonElement expectation)
{
    return EquivalencyResult.ContinueWithNext;
}
```

Both sides must be `JsonElement` for the step to fire. This is intentional — if the expectation is a `string`, the caller should use `JsonElementAssertions.BeEquivalentTo(string)` directly. Mixed-type comparisons fall through to the next step.

## Decision 3: Failure message uses `context.Reason` for `because` propagation

```csharp
Execute.Assertion
    .BecauseOf(context.Reason.FormattedMessage, context.Reason.Arguments)
    .ForCondition(subjectJson == expectedJson)
    .FailWith("Expected JSON to be equivalent to {0}{reason}, but found {1}.", expectedJson, subjectJson);
```

The `Reason` from `IEquivalencyValidationContext` carries whatever `because`/`becauseArgs` the caller passed to `BeEquivalentTo`. Using `.BecauseOf(context.Reason.FormattedMessage, context.Reason.Arguments)` threads that reason into the failure message — consistent with FA's own built-in steps (confirmed in decompiled source).

## Decision 4: Extension method is generic on `SelfReferenceEquivalencyAssertionOptions<TSelf>`

```csharp
public static TSelf UsingJsonElementComparison<TSelf>(
    this SelfReferenceEquivalencyAssertionOptions<TSelf> options)
    where TSelf : SelfReferenceEquivalencyAssertionOptions<TSelf>
    => options.Using(new JsonElementEquivalencyStep());
```

`BeEquivalentTo` lambdas receive `EquivalencyAssertionOptions<TExpectation>` (generic); `AssertEquivalencyUsing` lambdas receive `EquivalencyAssertionOptions` (non-generic). Both inherit from `SelfReferenceEquivalencyAssertionOptions<TSelf>`, so a single generic extension covers both registration patterns without overloads or casts. Return type is `TSelf` (matching `Using()`'s return type) so further chaining is preserved.

## Decision 5: Normalization via `JsonSerializer.Serialize()`

Same strategy as `JsonElementAssertions.BeEquivalentTo`. `JsonSerializer.Serialize()` normalizes whitespace and produces a canonical key-ordered representation, making structurally identical JSON compare as equal even if the original byte streams differed in formatting.
