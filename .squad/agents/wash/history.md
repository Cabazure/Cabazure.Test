# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

My domain is xUnit 3 integration. Key difference from xUnit 2: `DataAttribute` in v3 uses `IDataAttribute` and the new `TheoryData` pipeline. Module initializers replace static constructors for assembly-level setup. I own the `AutoNSubstituteDataAttribute` and all the CI/NuGet plumbing.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).
