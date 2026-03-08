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
