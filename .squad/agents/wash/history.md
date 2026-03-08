# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is xUnit 3 integration. Key difference from xUnit 2: `DataAttribute` in v3 uses `IDataAttribute` and the new `TheoryData` pipeline. Module initializers replace static constructors for assembly-level setup. I own the `AutoNSubstituteDataAttribute` and all the CI/NuGet plumbing.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

### StringContentExtensionsTests (parallel work with Kaylee)

- Created `tests/Cabazure.Test.Tests/Assertions/StringContentExtensionsTests.cs` with 12 `[Fact]` tests covering all three new extension method groups: `BeSimilarTo`/`NotBeSimilarTo`, `BeXmlEquivalentTo`/`NotBeXmlEquivalentTo`, `BeJsonEquivalentTo`/`NotBeJsonEquivalentTo`.
- Build currently fails with 12 CS1061 errors (one per call-site) because Kaylee's `StringContentExtensions` implementation does not exist yet — this is expected for parallel work and will be resolved once both files are in place.
- Test patterns used: `[Fact]` only, `var act = () => subject.Should()...`, then `act.Should().NotThrow()` or `act.Should().Throw<Exception>()` with message assertions. Matches `JsonElementAssertionsTests` and `DateTimeOffsetExtensionsTests` conventions exactly.
- Whitespace normalization tests use `\n` and double-spaces as subject to exercise the `\s+` → single-space collapse behaviour; XML tests use a raw string literal for indented XML vs. a compact single-line string; JSON tests mirror the same pattern with JSON object literals.

### 2026-03-07: README Authoring
- `README.md` is wired as `PackageReadmeFile` in `Cabazure.Test.csproj` — it will appear on nuget.org automatically when the package is packed. The file must exist at the repo root for the pack step to succeed without warnings.
- FluentAssertions 7.x migration note was included in the README under Compatibility. FA 7 contains breaking changes from 6.x; users migrating from Atc.Test or similar FA-6-based packages should be directed to the official migration guide.

### 2026-03-07: GitHub Actions Pipelines
- Created 3-workflow pattern matching all sibling Cabazure repos:
  - `ci.yml`: triggers on push/PR to main → build + test + coverage badges (committed back to repo)
  - `release.yml`: triggers on `vX.Y.Z` tags (must be on main) → build + test + coverage + pack + NuGet publish
  - `release-preview.yml`: triggers on `vX.Y.Z-previewN` tags (no main guard) → build + pack + NuGet publish
- **`dotnet build` does NOT produce `.nupkg` files by default** — a separate `dotnet pack --no-build -c:Release -p:Version=${VERSION}` step is required before `dotnet nuget push`
- Solution file is `.slnx` format (new XML format) — `dotnet` CLI auto-discovers it, no need to specify explicitly in workflow commands
- Version flows from git tag → `VERSION` env var → `-p:Version=${VERSION}` on both build and pack steps
- `NUGET_KEY` secret must be configured in repo settings before first release

### 2026-03-07: Solution Scaffolding
- Created `Cabazure.Test.sln` with src/tests structure
- **xUnit 3 Package Discovery:** xUnit v3 is at version 3.2.2 on NuGet
  - Main package: `xunit.v3` (version 3.2.2) for test projects
  - Extensibility: `xunit.v3.extensibility.core` (version 3.2.2) for library projects creating test attributes
  - Assertions: `xunit.v3.assert` (version 3.2.2) for assertion helpers
  - Test runner: `xunit.runner.visualstudio` (version 3.1.5)
- **Package Versions Locked:**
  - AutoFixture: 4.18.1
  - AutoFixture.AutoNSubstitute: 4.18.1
  - NSubstitute: 5.3.0
  - FluentAssertions: 7.0.0
  - Microsoft.NET.Test.Sdk: 17.12.0
  - coverlet.collector: 6.0.4
- **Key Learning:** Library projects extending xUnit should use `xunit.v3.extensibility.core` + `xunit.v3.assert`, NOT the full `xunit.v3` package (which requires OutputType=Exe)
- Created `.editorconfig` with C# team standards (space indent, crlf, using directives outside namespace)
- Established directory structure: Attributes/, Customizations/, Fixture/
- NuGet package metadata configured in main .csproj for future publishing

### 2026-03-08: Protected Methods README Section
- `ProtectedMethodExtensions` uses `BindingFlags.FlattenHierarchy` to resolve protected methods across the full type hierarchy — this is the key detail that makes it useful for base-class template method testing.
- README "Protected Methods" section was already scaffolded as a stub (Overloads bulleted list + single sync example). Updated it to: lead with a 4-overload quick-reference code block (void/typed/async-void/async-typed), replace the stub example with `[Theory, AutoNSubstituteData]` style to match the library's own idioms, and rewrite Notes to explicitly name `MissingMethodException` and `ExceptionDispatchInfo`.
- Added `InvokeProtected` / `InvokeProtectedAsync` row to the Features table so the API is discoverable from the features overview.
- When documenting reflection-based helpers, always surface the binding flags used — `FlattenHierarchy` vs `DeclaredOnly` has a material impact on test utility that readers need to know.

### 2026-03-10: Phase 17 — FluentAssertions Extensions Documentation
- Added new "## FluentAssertions Extensions" section to README after "Protected Methods" and before "Compatibility"
- Documented `JsonElementAssertions.BeEquivalentTo()` overloads:
  - Comparison between two `JsonElement` instances (normalized via serialization)
  - Comparison of `JsonElement` against JSON string literals
  - Key notes: array order is significant; object key order is preserved but does not affect equivalence
- Documented `DateTimeOffsetExtensions` with `CabazureAssertionOptions`:
  - `BeCloseTo(DateTimeOffset)` using default precision (1 second)
  - `BeCloseTo(DateTimeOffset, int precisionMilliseconds)` with explicit precision
  - `NotBeCloseTo(DateTimeOffset)` and `NotBeCloseTo(DateTimeOffset, int)` variants
  - Configuration via `[ModuleInitializer]` to set project-wide default precision
- All examples show namespace is `Cabazure.Test` only — no extra using directives required
- Code examples follow README style: language-tagged fences, clear explanations before code blocks, matching existing section patterns

- Updated all `[Frozen]` code examples in README to include `using AutoFixture.Xunit3;` at the top, making the source of `[Frozen]` explicit to readers.
- Enhanced Installation section to list all bundled dependencies (xUnit 3, AutoFixture.Xunit3, NSubstitute, FluentAssertions) with explicit mention that they are transitive dependencies requiring no additional setup.
- Created a new dedicated "Freezing Fixtures with `[Frozen]`" section before the Features table that:
  - States clearly that `[Frozen]` is provided by `AutoFixture.Xunit3` package (transitive dependency of `Cabazure.Test`)
  - Shows the required using directive
  - Provides a complete working example
  - Documents advanced usage with `Matching.ImplementedInterfaces` enum
  - Lists key points: left-to-right resolution, reference types only, works with all data attributes
- **Namespace Decision:** `[Frozen]` now consistently references `AutoFixture.Xunit3`, not `Cabazure.Test.Attributes`. The custom `FrozenAttribute` in `Cabazure.Test.Attributes` is a legacy artifact and users should prefer the standard AutoFixture.Xunit3 version for NuGet discoverability and upstream compatibility.

### Phase 16 (FrozenAttribute Documentation - 2026-03-07T21:44:11Z)

Task: Update README to clarify FrozenAttribute namespace and document Matching enum support.

Implementation:
- Enhanced Installation section with bundled dependency list (xUnit 3, AutoFixture.Xunit3, NSubstitute, FluentAssertions)
- Clarified AutoFixture.Xunit3 as source of [Frozen]
- Added new Freezing Fixtures section before Features table
- Updated 3 code examples (NotificationServiceTests, DiscountServiceTests, PaymentServiceTests) with using AutoFixture.Xunit3

Documentation Impact:
- Users: Clear namespace authority; reduced confusion
- NuGet Discoverability: Examples align with AutoFixture.Xunit3 official docs
- Maintainability: Single source of truth reduces tracking burden
- Backward Compatibility: Custom FrozenAttribute still available but de-emphasized

Cross-team: Kaylee refactored source code (agent-51); Zoe migrated tests (agent-52).

Decision logged: .squad/decisions.md - Phase 16 decision with Wash documentation details

### Phase 17: FluentAssertions Extensions Documentation (2026-03-07T22:12:13Z)

Task: Document two new FluentAssertions extension features (JsonElementAssertions and DateTimeOffsetExtensions) in README with clear examples and configuration guidance.

Implementation:
- Added "## FluentAssertions Extensions" section after "Protected Methods" section
- JsonElement feature documentation:
  - Explains `BeEquivalentTo()` overloads with two code examples
  - Notes on array order sensitivity and object key ordering
  - Clear usage: element-to-element and element-to-string comparison
- DateTimeOffset feature documentation:
  - Explains `BeCloseTo()` and `NotBeCloseTo()` methods with three examples
  - Example 1: Default 1-second precision (no explicit tolerance)
  - Example 2: Explicit per-assertion millisecond precision
  - Example 3: Project-wide configuration via `[ModuleInitializer]`
  - Realistic business logic scenario (OrderTimestamp)
- All code examples use `using Cabazure.Test;` only (minimal imports)
- Consistent with existing README style: language tags, checkmark comments, proper heading hierarchy

Documentation Impact:
- Users: Clear discovery path for both extension features
- Examples: Runnable, idiomatic C# with proper patterns
- Maintenance: Documented contract for future API changes
- Discoverability: Placed between core features and compatibility notes

Cross-team: Kaylee implemented JsonElementAssertions and DateTimeOffsetExtensions; Zoe provided comprehensive test coverage (19 tests, all passing).

Decision logged: .squad/decisions.md - Phase 17 Decisions 1–3 (JsonElementAssertions, DateTimeOffsetExtensions, Documentation)

### Phase 21: String Content Assertions Documentation (2026-03-10T22:00:00Z)

Task: Update README.md to document three new string content assertion methods for format-ignorant comparison.

Implementation:
- Added three rows to the Features table documenting the new assertion methods:
  - `BeSimilarTo<T>`: Whitespace-normalized string comparison
  - `BeXmlEquivalentTo<T>`: XML structural comparison ignoring formatting
  - `BeJsonEquivalentTo<T>`: JSON structural comparison ignoring formatting
- Created new "## String Content Assertions" section after DateTimeOffset section, before Compatibility
- Documented three distinct comparison scenarios with code examples:
  - Whitespace normalization: collapses multiple spaces, tabs, and newlines to single space
  - XML comparison: ignores indentation and line endings, focuses on structure/content
  - JSON comparison: ignores formatting, compares by value
- All code examples use `using Cabazure.Test;` only (minimal imports)
- Documented negative counterparts (`NotBeSimilarTo`, `NotBeXmlEquivalentTo`, `NotBeJsonEquivalentTo`)
- Noted support for standard FluentAssertions `because`/`becauseArgs` parameters

Documentation Impact:
- Users: Clear discovery path for string content assertion features
- Examples: Runnable, idiomatic C# with proper patterns using raw string literals
- Maintenance: Documented contract for future API changes
- Discoverability: Seamlessly integrated between DateTimeOffset and Compatibility sections

### Phase 22: Namespace Consolidation (part 2 of 2) — Using Statement Updates (2026-03-10)

Task: Update all `using` statements across the codebase after Kaylee moved 5 public attribute types from `Cabazure.Test.Attributes` → `Cabazure.Test` and `FixtureCustomizationCollection` to `Cabazure.Test.Customizations`.

Implementation:
- Identified 15 test files with stale `using Cabazure.Test.Attributes;` statements
- Removed `using Cabazure.Test.Attributes;` from all test files (no longer needed since attribute types are now in `Cabazure.Test`)
- Removed `using Cabazure.Test.Attributes;` from 3 code examples in README.md
- Added `using Cabazure.Test.Customizations;` to `FixtureCustomizationCollectionTests.cs` (needed for direct `FixtureCustomizationCollection` usage)
- Preserved `using Cabazure.Test.Attributes;` in the 4 attribute source files themselves (AutoNSubstituteDataAttribute, ClassAutoNSubstituteDataAttribute, InlineAutoNSubstituteDataAttribute, MemberAutoNSubstituteDataAttribute) — they need it to access internal `FixtureDataExtensions.MergeValues` helper
- Verified that all files using `FixtureCustomizationCollection` have `using Cabazure.Test.Customizations;`

Build Result: ✅ GREEN — `dotnet build` succeeded with no errors

Key Insight:
- The 4 public attribute classes that now live in `Cabazure.Test` namespace still need `using Cabazure.Test.Attributes;` internally to access the internal `FixtureDataExtensions` helper that remained in the `Attributes` namespace
- User-facing code (tests, documentation examples) no longer needs `using Cabazure.Test.Attributes;` — all public types are in `Cabazure.Test`
- `FixtureCustomizationCollection` moved to `Cabazure.Test.Customizations` and required adding that using directive where directly referenced

Cross-team: Kaylee completed Phase 22 part 1 (namespace declarations); this work completes part 2 (using statements).

### Phase 23: JsonElementEquivalencyStep Documentation & Commit (2026-03-10)

Task: Update README.md and copilot-instructions.md to document the new `JsonElementEquivalencyStep` / `UsingJsonElementComparison()` feature, then stage and commit all Phase 23 changes.

Implementation:
- Added new `### JsonElement Equivalency in DTOs` subsection to `## FluentAssertions Extensions` in README.md, between the DateTimeOffset section and the `## String Content Assertions` section
  - Explains the problem: `BeEquivalentTo` falls back to reference equality for `JsonElement` properties
  - Documents per-call usage: `opts.UsingJsonElementComparison()`
  - Documents global registration via `[ModuleInitializer]` + `AssertionOptions.AssertEquivalencyUsing`
- Added `JsonElementEquivalencyStep` / `UsingJsonElementComparison()` entry to the `### FluentAssertions Extensions` section in `.github/copilot-instructions.md`, between the new `#### JsonElementEquivalencyStep` heading and `#### StringContentExtensions`
- Staged all changes with `git add -A` and created commit `feat(assertions): add JsonElementEquivalencyStep for BeEquivalentTo on DTOs` (8 files, 330 insertions)

Files committed:
- `src/Cabazure.Test/Assertions/JsonElementEquivalencyStep.cs` (new)
- `src/Cabazure.Test/Assertions/JsonElementEquivalencyExtensions.cs` (new)
- `tests/Cabazure.Test.Tests/Assertions/JsonElementEquivalencyStepTests.cs` (new)
- `.squad/decisions/inbox/kaylee-p23-equivalency.md` (new)
- `README.md` (updated)
- `.github/copilot-instructions.md` (updated)
- `.squad/agents/kaylee/history.md`, `.squad/agents/zoe/history.md` (line-ending normalization)

Cross-team: Kaylee implemented the step and extension; Zoe provided 5 tests; Wash completed docs and commit.

### Phase 24: EmptyObjectEquivalencyStep Documentation & Commit (2026-03-10)

Task: Update README.md and copilot-instructions.md to document the new `EmptyObjectEquivalencyStep` / `AllowingEmptyObjects<TSelf>()` feature, then stage and commit all Phase 24 changes.

Implementation:
- Added new `### Allowing Empty Objects in BeEquivalentTo` subsection to `## FluentAssertions Extensions` in README.md, immediately after the "JsonElement Equivalency in DTOs" section
  - Explains the problem: `BeEquivalentTo` throws `InvalidOperationException` on types with no public properties
  - Documents per-call usage: `opts.AllowingEmptyObjects()`
  - Documents global registration via `[ModuleInitializer]` + `AssertionOptions.AssertEquivalencyUsing`
  - Notes that types with members continue through FluentAssertions' normal equivalency pipeline unchanged
- Added `EmptyObjectEquivalencyStep` / `AllowingEmptyObjects<TSelf>()` entry to the `### FluentAssertions Extensions` section in `.github/copilot-instructions.md`, between the `JsonElementEquivalencyStep` entry and `StringContentExtensions` heading
  - Describes `EmptyObjectEquivalencyStep` as an `IEquivalencyStep` that returns `AssertionCompleted` for empty types
  - Describes `AllowingEmptyObjects<TSelf>()` as the extension method that registers the step; documents both per-call and global usage patterns
- Staged changes with `git add README.md .github/copilot-instructions.md` and created commit `feat(assertions): add EmptyObjectEquivalencyStep for BeEquivalentTo on property-less types` (2 files, 21 insertions)

Key Pattern Learning:
- The documentation pattern established for `AllowingEmptyObjects()` mirrors `UsingJsonElementComparison()`: lead with the problem statement, then show per-call usage, then show global registration. This pattern is consistent and discoverable for users working with FluentAssertions equivalency configuration.
- Both extensions are designed to work seamlessly in module initializers via `AssertionOptions.AssertEquivalencyUsing`, keeping project-wide configuration centralized.
- Documenting both the step class and the extension method together in copilot-instructions makes the API surface clear to internal team members and future maintainers.

Cross-team: Kaylee implemented the step and extension; Zoe provided 5 tests; Wash completed docs and commit.
