# Decision: Test Timeout Documentation (Phase 25)

**Date:** 2026-03-11  
**Agent:** Wash (Integration & Tooling Developer)  
**Issue/Reference:** Documentation task from Mal's timeout research findings

## Decision

Document three complementary test timeout patterns in README.md and .github/copilot-instructions.md. **No timeout helper was added to Cabazure.Test** — the xUnit 3 framework + .NET BCL provide complete coverage for all test timeout scenarios.

## Context

Mal researched test timeout patterns and found that xUnit 3 + BCL already provide comprehensive solutions. Rather than adding a new library helper, we document the three recommended patterns for test authors:

1. **Whole-test execution limit** — `[Fact(Timeout = 5000)]` or `[Theory(Timeout = 5000)]` — xUnit 3 native attribute
2. **Per-await timeout** — `await task.WaitAsync(TimeSpan.FromMilliseconds(500))` — .NET 6+ BCL API
3. **NSubstitute call verification timeout** — `substitute.WaitForReceived(x => x.Method(...))` — already in Cabazure.Test via `WaitForReceivedExtensions`

## Implementation

### README.md
- Added "## Test Timeouts" section after "String Content Assertions" and before "Compatibility"
- Documented all three patterns with code examples and use-case guidance
- Example configurations for per-test setup and module initializer overrides

### .github/copilot-instructions.md
- Added "### Test Timeouts" subsection after "ProtectedMethodExtensions" and before "xUnit 3 Specifics"
- Listed the three patterns with concise descriptions and best-use guidance
- Noted that no new library helper is required

## Rationale

- **Completeness:** The three patterns cover whole-test, per-call, and call-verification timeout scenarios — no gaps
- **No library coupling:** Timeout handling is better left to xUnit 3 and the BCL; adding a library helper would conflate testing infrastructure with Cabazure.Test's assertion/fixture responsibility
- **Discoverability:** Documenting patterns in README + copilot-instructions makes them immediately available to users and team members without requiring new code maintenance

## Impact

- **Users:** Clear, discoverable patterns for handling test timeouts across different scenarios
- **Maintenance:** No new code to maintain; patterns are stable (xUnit 3 native attributes, BCL APIs, existing WaitForReceived)
- **Scope:** Confirms that Cabazure.Test is focused on fixtures, assertions, and test data — not low-level framework concerns like timeouts

## Files Changed

- `README.md` — added "Test Timeouts" section (+60 lines)
- `.github/copilot-instructions.md` — added "Test Timeouts" subsection (+22 lines)
- `.squad/agents/wash/history.md` — logged this phase

**Commit:** `docs(assertions): document test timeout patterns`
