# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is xUnit 3 integration. Key difference from xUnit 2: `DataAttribute` in v3 uses `IDataAttribute` and the new `TheoryData` pipeline. Module initializers replace static constructors for assembly-level setup. I own the `AutoNSubstituteDataAttribute` and all the CI/NuGet plumbing.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).

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

