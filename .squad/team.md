# Squad Team

> Cabazure.Test

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Mal | 🏗️ Lead & Architect | [charter](.squad/agents/mal/charter.md) | active |
| Kaylee | ⚙️ Core .NET Developer | [charter](.squad/agents/kaylee/charter.md) | active |
| Wash | 🛠️ Integration & Tooling Developer | [charter](.squad/agents/wash/charter.md) | active |
| Zoe | 🧪 QA & Testing Lead | [charter](.squad/agents/zoe/charter.md) | active |
| Scribe | 📋 Memory & Session Logger | [charter](.squad/agents/scribe/charter.md) | active |

## Coding Agent Capabilities

| Capability | Fit | Notes |
|-----------|-----|-------|
| Implementing AutoFixture customizations | 🟢 Good fit | Well-defined patterns, existing examples |
| Adding new `DataAttribute` variants | 🟢 Good fit | Clear xUnit 3 extensibility points |
| Writing tests using `[AutoNSubstituteData]` | 🟢 Good fit | Follows established patterns |
| Fixing bugs in fixture composition | 🟡 Needs review | May affect public API behaviour |
| Designing new public API surface | 🔴 Not suitable | Route to Mal for design review |
| Architectural changes to fixture pipeline | 🔴 Not suitable | Route to Mal + Kaylee |

## Project Context

- **Project:** Cabazure.Test — open-source .NET unit testing library
- **Owner:** Ricky Kaare Engelharth
- **Stack:** C# (.NET 9+), xUnit 3, NSubstitute, AutoFixture, FluentAssertions
- **Universe:** Firefly
- **Created:** 2026-03-07
