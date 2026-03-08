# Decision: Copy-on-Write Pattern for Thread-Safe Collections

**Date:** 2025-03-08  
**Author:** Kaylee  
**Status:** Proposed  
**Related:** Phase 38 (reflection-caching), Phase 39 (FixtureCustomizationCollection refactor)

## Context

Thread-safe collections that need lock-free reads have two common design patterns:

1. **Two-field with snapshot caching:** Mutable backing collection + lazily-built immutable snapshot (Phase 38 original design)
2. **Single volatile array with copy-on-write:** Volatile array reference that is replaced atomically on mutation (Phase 39 implementation)

## Decision

For collections in Cabazure.Test that need thread-safe, lock-free reads:

**Use copy-on-write with a single volatile array reference.**

### Rationale

1. **Simpler reasoning:** One field instead of two. The volatile field reference IS the snapshot.
2. **No DCL complexity:** Direct volatile reads are correct by construction — no double-checked locking, no inner re-check, no nullability handling.
3. **Clear write semantics:** All mutations acquire a lock, copy the current array, modify the copy, and atomically replace the reference.
4. **Same read performance:** Both approaches give lock-free reads after initialization. Copy-on-write eliminates the "first read builds snapshot" overhead.
5. **Acceptable write cost:** Mutations are rare (module initializers) and bounded (collection size ≤ 20 customizations). Array copies are negligible.

### Pattern Template

```csharp
private readonly object syncLock = new();
private volatile T[] _items;

// Constructor: initialize with default values
internal MyCollection()
{
    _items = [item1, item2, item3];
}

// Reads: direct volatile read, no lock
public int Count => _items.Length;
public IEnumerator<T> GetEnumerator() 
    => ((IEnumerable<T>)_items).GetEnumerator();

// Writes: lock + copy + replace
public void Add(T item)
{
    lock (syncLock)
        _items = [.._items, item];
}

public bool Remove(T item)
{
    lock (syncLock)
    {
        var current = _items;
        var idx = Array.IndexOf(current, item);
        if (idx < 0) return false;
        var next = new T[current.Length - 1];
        Array.Copy(current, 0, next, 0, idx);
        Array.Copy(current, idx + 1, next, idx, current.Length - idx - 1);
        _items = next;
        return true;
    }
}
```

### When Not to Use

- **High-frequency writes:** If mutations happen frequently (e.g., per-test), the copy overhead dominates. Use `ConcurrentDictionary` or `ImmutableArray<T>` with atomic swap instead.
- **Large collections:** If the array can grow to thousands of elements, copying becomes expensive. Consider chunked structures or immutable collections with structural sharing.

### Applied In

- `FixtureCustomizationCollection` (Phase 39) — 7 default customizations, mutations only at module init time. Perfect fit.

## Alternatives Considered

1. **Keep two-field design with correct DCL:** Adds complexity (inner re-check, nullability) for no benefit over copy-on-write.
2. **`ImmutableArray<T>` with Interlocked.CompareExchange:** Overkill — we already need a lock to prevent lost updates from concurrent mutations. Copy-on-write inside the lock is simpler.
3. **`ConcurrentBag<T>` or `ConcurrentQueue<T>`:** Don't preserve insertion order and don't support removals by value/type.

## Testing

All 238 existing tests pass with no behavioral changes (commit 1eb2eb5).

## Consequences

- **Positive:** Simpler code, easier to audit for correctness, no DCL bugs possible.
- **Positive:** Eliminates snapshot-building overhead on first read.
- **Neutral:** Mutation cost increases from O(1) list append to O(n) array copy — acceptable for low-frequency writes.
- **Negative:** None identified for this use case.

## Review Needed

If this pattern proves sound after production use, codify it in the charter as the standard approach for thread-safe collections with rare writes.
