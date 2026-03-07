# Project Context

- **Owner:** Ricky Kaare Engelharth
- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Created:** 2026-03-07

## Core Context

Cabazure.Test integrates xUnit 3, NSubstitute, AutoFixture, and FluentAssertions. The two headline features are:
1. `SutFixture` — AutoFixture-backed fixture that auto-substitutes unregistered interfaces/abstract classes via NSubstitute.
2. `AutoNSubstituteDataAttribute` — xUnit 3 DataAttribute that provides Theory arguments through SutFixture.

xUnit 3 advantages we're leveraging: module initializers via `[ModuleInitializer]`, improved extensibility points over xUnit 2.

## Learnings

📌 Team initialized on 2026-03-07 — Firefly cast: Mal (Lead), Kaylee (Core Dev), Wash (Integration Dev), Zoe (QA), Scribe (Memory).
