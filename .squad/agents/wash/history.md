# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is xUnit 3 integration. Key difference from xUnit 2: `DataAttribute` in v3 uses `IDataAttribute` and the new `TheoryData` pipeline. Module initializers replace static constructors for assembly-level setup. I own the `AutoNSubstituteDataAttribute` and all the CI/NuGet plumbing.

**Early Phase Summary (2026-03-07 to 2026-03-08):**
- Created test scaffolding: StringContentExtensionsTests, solution, CI/NuGet pipelines (3 workflow pattern)
- Documented five README sections: Protected Methods, FluentAssertions Extensions, [Frozen] Fixtures, String Content Assertions
- Updated using statements across 15+ test files after namespace consolidation
- Documented two equivalency steps (JsonElement, EmptyObject) with per-call and global registration patterns
- Committed: 8 documentation commits spanning Phases 16–25

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### Phase 16: FrozenAttribute Documentation
- Enhanced Installation section with bundled dependency list
- Clarified AutoFixture.Xunit3 as source of [Frozen]
- Added Freezing Fixtures section before Features table
- **Namespace Decision:** [Frozen] references AutoFixture.Xunit3 for NuGet discoverability

### Phase 17: FluentAssertions Extensions Documentation
- Added "## FluentAssertions Extensions" section to README
- Documented JsonElementAssertions.BeEquivalentTo() with per-call and global configuration examples
- Documented DateTimeOffsetExtensions with default precision (1 second) and custom overrides
- Introduced [ModuleInitializer] pattern for project-wide assertion configuration

### Phase 21: String Content Assertions Documentation
- Added three assertion methods to Features table: BeSimilarTo, BeXmlEquivalentTo, BeJsonEquivalentTo
- Created "## String Content Assertions" section with code examples for all three patterns
- Documented negative counterparts and standard FluentAssertions parameters

### Phase 22: Namespace Consolidation (part 2)
- Updated 15 test files: removed stale `using Cabazure.Test.Attributes;` statements
- Added `using Cabazure.Test.Customizations;` where FixtureCustomizationCollection is directly referenced
- Internal attribute files preserve `using Cabazure.Test.Attributes;` to access FixtureDataExtensions helper
- **Build Result:** ✅ GREEN

### Phase 23: JsonElementEquivalencyStep Documentation & Commit
- Added "### JsonElement Equivalency in DTOs" subsection to FluentAssertions Extensions
- Documented per-call usage: `opts.UsingJsonElementComparison()`
- Documented global registration via [ModuleInitializer] + AssertionOptions
- Committed: `feat(assertions): add JsonElementEquivalencyStep for BeEquivalentTo on DTOs`

### Phase 24: EmptyObjectEquivalencyStep Documentation & Commit
- Added "### Allowing Empty Objects in BeEquivalentTo" subsection
- Documented per-call usage: `opts.AllowingEmptyObjects()`
- Documented global registration pattern (mirrors Phase 23)
- Committed: `feat(assertions): add EmptyObjectEquivalencyStep for BeEquivalentTo on property-less types`
- **Pattern Learning:** Both Phases 23 & 24 follow consistent documentation structure: problem statement → per-call usage → global registration via [ModuleInitializer]

### Phase 25: Test Timeouts Documentation (2026-03-11)

### Phase 27: Dependency Upgrade Commits (2026-03-08)

**Task:** Create focused conventional commits for Kaylee's dependency upgrade work.

**Commits Created:**
- 8ee6553: uild(deps): upgrade test infrastructure packages — coverlet.collector & Microsoft.NET.Test.Sdk upgrades
- c31b69f: uild(deps): pin FluentAssertions to v7 (licensing) — version constraint [7.0.0, 8.0.0) in both .csproj files

**Result:** Clean conventional commit history. Single-concern separation enables clear bisect workflows. Licensing context preserved in commit message for future maintainers. All 217 tests passing.

**Pattern:** Infrastructure upgrades separated from licensing constraints for decision clarity and maintainability.

### Phase 38: Test Performance Tips Documentation (2026-03-11)

**Task:** Add "## Test Performance Tips" section to README.md based on real-world usage patterns from ocpp-core (3,025 tests).

**Section added:** After `## Test Timeouts`, before `## Compatibility` — three numbered tips:
1. Prefer `[Frozen]` parameters over `FixtureFactory.Create()` in shared helpers (avoids bypassing `[CustomizeWith]` and creating bare fixtures)
2. Register domain-type customizations once via `[ModuleInitializer]` (eliminates per-fixture resolution overhead; cross-references existing section)
3. Consolidate repetitive `[Fact]` methods with `[MemberAutoNSubstituteData]` + `TheoryData<Type>` (reduces test count, improves clarity)

**Pattern:** Doc sections use ❌/✅ code block pairs with a brief rationale sentence — consistent with FluentAssertions Extensions section style. Cross-references to existing sections preferred over duplicating examples.

**Cross-Update (Scribe, 2026-03-08T15:12:21Z):** Kaylee's decision merged to decisions.md. Code commits: fc2f65b, b41c235. Squad files logged. Phase 38 ready for merge.

### Phase 41: Stable Discovery Labels PR (2026-03-13)

**Task:** Create GitHub PR for `squad/stable-discovery-labels` targeting `main`.

**Work Completed:**
- Confirmed 4 commits on branch (stable-label strategy implementation + test coverage + squad history)
- Verified branch was already pushed to origin
- Created PR with comprehensive description covering:
  - Root cause: AutoFixture's non-deterministic `Guid.NewGuid()` causing display name mismatches between xUnit 3 discovery and execution phases
  - Solution: Set `TheoryDataRow.Label` to deterministic values per attribute type (empty string for `AutoNSubstituteData`, formatted values for `InlineAutoNSubstituteData`, row indices for member/class variants)
  - Test coverage: 9 new tests in `DiscoveryEnumerationLabelTests.cs` verifying label stability
  - Working as Wash (Integration & Tooling Developer)

**PR:** https://github.com/Cabazure/Cabazure.Test/pull/1

**Learning:** The "Not Run" bug illustrates the critical importance of deterministic display names in xUnit 3's discovery pipeline. The fix (stable labels) is minimal and surgical — one-liner assignments on each data row — making it a good model for targeted bug fixes in test infrastructure.

## Phase 26: Fix Discovery Enumeration — PR #1 (2026-03-20)

**Teammates:** Kaylee (Implementation), Zoe (Testing), Mal (Architecture Review)

**Task:** Create PR #1 for discovery enumeration fix.

**Work Done:**
- Created PR #1 at https://github.com/Cabazure/Cabazure.Test/pull/1
- Title: "Fix xUnit 3 test discovery enumeration: SupportsDiscoveryEnumeration() => false on all attributes"
- Branch: squad/stable-discovery-labels
- Commits:
  - Kaylee: Implementation (reverted labels, set SupportsDiscoveryEnumeration() => false)
  - Zoe: Tests (4 new SupportsDiscoveryEnumeration() contract tests)
  - Mal: Architecture decision (comprehensive analysis in decisions.md)

**PR Description:** Explains root cause (TestCaseUniqueID hash mismatch between discovery and execution), rationale for decision (test correctness over IDE discoverability), and analyzed alternatives.

**Status:** Ready for squad review before merge.

