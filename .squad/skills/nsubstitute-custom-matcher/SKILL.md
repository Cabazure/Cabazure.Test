# Skill: NSubstitute Custom Argument Matcher

**Captured by:** Kaylee  
**Date:** 2026-03-07  
**Source:** FluentArg implementation

---

## What This Skill Covers

How to create a custom argument matcher for NSubstitute 5.x that integrates cleanly with the `Received()` assertion API and surfaces rich failure messages in `ReceivedCallsException`.

---

## Key Types

| Type | Namespace | Purpose |
|------|-----------|---------|
| `IArgumentMatcher<T>` | `NSubstitute.Core.Arguments` | Generic matcher interface; implement `bool IsSatisfiedBy(T? argument)` |
| `IDescribeNonMatches` | `NSubstitute.Core` | Optional; implement `string DescribeFor(object? argument)` for failure messages |
| `ArgumentMatcher.Enqueue<T>` | `NSubstitute.Core.Arguments` | Public registration point; returns `ref T?` for use in call expressions |

---

## Pattern

```csharp
internal sealed class MyMatcher<T> : IArgumentMatcher<T>, IDescribeNonMatches
{
    public bool IsSatisfiedBy(T? argument)
    {
        // Return true if argument matches; false otherwise.
        // Exceptions are swallowed by NSubstitute (treated as non-match).
    }

    public string DescribeFor(object? argument)
    {
        // Called by NSubstitute to build the ReceivedCallsException message.
        // Return string.Empty if argument actually matches (shouldn't happen).
        // Return a human-readable reason if it doesn't.
    }
}

// Registration (inside a static factory):
public static ref T? Match<T>(/* ... */)
    => ref ArgumentMatcher.Enqueue<T>(new MyMatcher<T>(/* ... */));
```

---

## Gotchas

1. **Exceptions in `IsSatisfiedBy` are silently swallowed** — NSubstitute treats any exception as a non-match without any error output. Always implement `IDescribeNonMatches` to surface failure reasons.

2. **`ArgumentMatcher.Enqueue<T>` auto-detects `IDescribeNonMatches`** — when the matcher also implements it, NSubstitute automatically wraps it with `GenericToNonGenericMatcherProxyWithDescribe<T>`. No extra work needed.

3. **Nullability:** `IArgumentMatcher<T>.IsSatisfiedBy` has signature `bool IsSatisfiedBy(T? argument)` — the parameter is nullable. Use `!` suppression or a null guard when forwarding to non-nullable code.

4. **`ref T?` return value** — the `ref` return from `ArgumentMatcher.Enqueue<T>` is a NSubstitute implementation detail; it is safe to discard in normal usage but the `ref` must be declared in the factory method signature for the compiler to accept it.

---

## FluentAssertions Integration

The `FluentArg.Match<T>` pattern bridges FluentAssertions assertions into NSubstitute matchers:

```csharp
substitute.Received(1).Process(FluentArg.Match<Request>(r =>
{
    r.Name.Should().Be("Alice");
    r.Amount.Should().BeGreaterThan(0);
}));
```

- `assertion(argument)` throws `AssertionFailedException` (or similar) on failure → caught in `IsSatisfiedBy`, returns `false`
- `DescribeFor` re-runs the assertion and captures `ex.Message` → included verbatim in `ReceivedCallsException`
