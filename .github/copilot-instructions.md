# Cabazure.Test — Copilot Instructions

Cabazure.Test is an open-source .NET testing library that integrates **xUnit 3**, **NSubstitute**, **AutoFixture**, and **FluentAssertions** into a cohesive, ergonomic testing experience.

## Squad

This project uses Squad for AI team collaboration. Before working on any issue:

1. Read `.squad/team.md` for the team roster and member roles.
2. Read `.squad/routing.md` for work routing rules.
3. If the issue has a `squad:{member}` label, read `.squad/agents/{member}/charter.md` to understand their domain expertise and work in their voice.

## Tech Stack

| Concern | Package |
|---------|---------|
| Language | C# (.NET 9+, `<LangVersion>latest</LangVersion>`) |
| Test framework | xUnit 3 (`xunit` 3.x) |
| Mocking | NSubstitute |
| Test data | AutoFixture + AutoFixture.AutoNSubstitute |
| Assertions | FluentAssertions |
| Packaging | NuGet |

## Core Library Concepts

### `SutFixture`

The central fixture class. Wraps AutoFixture with `AutoNSubstituteCustomization` so that unregistered abstract/interface dependencies are automatically substituted. Key surface:

- `Create<T>()` — creates an instance with all unregistered dependencies auto-mocked.
- `Freeze<T>()` — registers a value (or substitute) to be reused for all subsequent `Create` calls.
- `Substitute<T>()` — explicitly creates an NSubstitute substitute for `T`.

### `AutoNSubstituteDataAttribute`

A custom xUnit 3 `DataAttribute` (`[AutoNSubstituteData]`) that uses `SutFixture` to generate and inject Theory test method arguments. Supports `[Frozen]` parameters.

### xUnit 3 Specifics

- Prefer `[ModuleInitializer]` (via `AssemblyInitializer`) over `IClassFixture` static constructors for global test setup.
- Never use `Console.Write` in tests — use `ITestOutputHelper`.
- For strongly-typed theory data sets, extend `TheoryData<T>`.

## Project Structure

```
src/
  Cabazure.Test/
    Attributes/          ← [AutoNSubstituteData] and supporting attributes
    Customizations/      ← AutoFixture customizations (AutoNSubstituteCustomization, etc.)
    Fixture/             ← SutFixture and related types
tests/
  Cabazure.Test.Tests/   ← Library's own tests — uses the library (dogfooding required)
```

## Code Style

- Use `var` when the type is obvious from the right-hand side.
- Prefer expression-bodied members for simple getters and single-line methods.
- Every public API member requires an XML doc comment (`<summary>`, `<typeparam>`, `<param>`, `<returns>`).
- Follow Microsoft's C# naming conventions (PascalCase types/members, camelCase locals, `_camelCase` private fields).
- Avoid abbreviations in public APIs.
- Target nullable reference types enabled (`<Nullable>enable</Nullable>`).

## Testing This Library

The library tests itself — `Cabazure.Test.Tests` uses `[AutoNSubstituteData]` from the library under development. This is both a quality signal and a dogfooding requirement. Do not add external mocking frameworks to the test project.

## Commit Messages

All commits must follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `ci`

**Scopes** (use the relevant one):
- `fixture` — SutFixture and fixture types
- `attributes` — DataAttribute and xUnit 3 integration
- `customizations` — AutoFixture customizations
- `tests` — test project changes
- `packaging` — NuGet / .csproj metadata
- `ci` — GitHub Actions workflows
- `squad` — squad team / agent files
- `github` — `.github/` config (not CI)
- *(omit scope for repo-wide changes)*

**Examples:**
```
feat(fixture): add SutFixture.Substitute<T>() for explicit substitution
fix(attributes): resolve frozen parameter ordering in AutoNSubstituteDataAttribute
test(fixture): add edge case coverage for sealed types
chore(packaging): configure NuGet metadata and license
```

Each commit should cover **one concern only** — no mixing features with test changes unless the test is inseparable from the feature (e.g., an internal test helper).

## Branch Naming

```
squad/{issue-number}-{kebab-case-slug}
```

Example: `squad/12-implement-sut-fixture`

## PR Guidelines

- Reference the issue: `Closes #{issue-number}`
- If the issue had a `squad:{member}` label: `Working as {member} ({role})`
- If flagged 🟡 needs-review: add a note in the PR description requesting squad review before merge.
- Keep PRs focused — one concern per PR.

## Capability Self-Check (for Coding Agent)

Before starting:

- **🟢 Good fit** — well-defined, follows existing patterns, bounded scope → proceed autonomously.
- **🟡 Needs review** — medium complexity, new API surface, or performance-sensitive → proceed, flag in PR.
- **🔴 Not suitable** — architecture decisions, API design, security concerns → comment on the issue and suggest reassignment to **Mal**.
