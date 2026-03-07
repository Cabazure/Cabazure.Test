# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is xUnit 3 integration. Key difference from xUnit 2: `DataAttribute` in v3 uses `IDataAttribute` and the new `TheoryData` pipeline. Module initializers replace static constructors for assembly-level setup. I own the `AutoNSubstituteDataAttribute` and all the CI/NuGet plumbing.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

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
