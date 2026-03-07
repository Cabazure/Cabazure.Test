# Cabazure.Test — Copilot Instructions

Cabazure.Test is an open-source .NET testing library that integrates **xUnit 3**, **NSubstitute**, **AutoFixture**, and **FluentAssertions** into a cohesive, ergonomic testing experience.

## Squad

This project uses Squad for AI team collaboration. Before working on any issue:

1. Read `.squad/team.md` for the team roster and member roles.
2. Read `.squad/routing.md` for work routing rules.
3. If the issue has a `squad:{member}` label, read `.squad/agents/{member}/charter.md` to understand their domain expertise and work in their voice.

### Squad History Commit Rule

**After every session, all `.squad/` changes must be committed to the repo.** This includes history files, decisions, orchestration logs, and session logs. The Scribe is responsible for staging and committing `.squad/` changes at the end of each agent batch. No agent session is complete until squad state is committed.

### README Sync Rule

**Whenever a new public API, customization, or feature is added or removed, `README.md` must be updated in the same commit.** The README is the primary documentation for library users. A feature that isn't in the README doesn't exist to consumers. This applies to:
- New or removed public types (`FixtureFactory`, `ImmutableCollectionCustomization`, etc.)
- New or removed attributes (`[CustomizeWith]`, data attributes)
- New or removed customizations
- Breaking changes to existing APIs

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

### `FixtureFactory`

The public entry point for creating configured AutoFixture `IFixture` instances. Use in `[Fact]` tests:

- `FixtureFactory.Create()` — returns an `IFixture` with `AutoNSubstituteCustomization` applied.
- `FixtureFactory.Create(params ICustomization[])` — same, with additional customizations.

The returned `IFixture` supports the full AutoFixture API: `fixture.Create<T>()`, `fixture.Freeze<T>()`, `fixture.Inject(instance)`, etc.

### Data Attributes

Four xUnit 3 `DataAttribute` implementations for `[Theory]` tests, all backed by `FixtureFactory`:

- `[AutoNSubstituteData]` — all parameters auto-generated from the fixture.
- `[InlineAutoNSubstituteData(value1, value2)]` — leading parameters provided inline, rest auto-generated.
- `[MemberAutoNSubstituteData(nameof(MyData))]` — leading parameters from a static member, rest auto-generated.
- `[ClassAutoNSubstituteData(typeof(MyDataClass))]` — leading parameters from an `IEnumerable<object[]>` class, rest auto-generated.

All support `[Frozen]` parameters — a frozen parameter is registered in the fixture before subsequent parameters are resolved.

**Important:** All custom data attributes **must** return `SupportsDiscoveryEnumeration = true` in their implementation. Live `CancellationToken` instances (from `CancellationTokenSource`) are not serializable in xUnit 3's test case discovery phase and would break test enumeration if discovery tried to serialize them. The standard data attributes handle this correctly.

### Customizations

- `AutoNSubstituteCustomization` — applied automatically by `FixtureFactory`; enables NSubstitute auto-substitution.
- `RecursionCustomization` — replaces `ThrowingRecursionBehavior` with `OmitOnRecursionBehavior`; use when your domain has self-referencing types.
- `ImmutableCollectionCustomization` — enables `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<TKey,TValue>`, `ImmutableHashSet<T>`, `ImmutableSortedSet<T>`, `ImmutableSortedDictionary<TKey,TValue>`, `ImmutableQueue<T>`, and `ImmutableStack<T>`.
- `CancellationTokenCustomization` — applied automatically by `FixtureFactory`; provides non-cancelled `CancellationToken` parameters (`new CancellationToken(false)`). Use `TestContext.Current.CancellationToken` for runner-scoped cancellation in test code; use `CancellationTokenSource` in test body for per-test cancellation scenarios.

### Additional Customizations

- `JsonElementCustomization` — enables AutoFixture to create random `JsonElement` values.
- `DateOnlyTimeOnlyCustomization` — enables AutoFixture to create `DateOnly` and `TimeOnly` values.

### Project-Wide Customizations (`FixtureFactory.Customizations`)

`FixtureFactory.Customizations` is an ordered `FixtureCustomizationCollection` pre-seeded with `AutoNSubstituteCustomization`. Register project-wide customizations using `[ModuleInitializer]`:

```csharp
internal static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
        => FixtureFactory.Customizations.Add(new MyProjectCustomization());
}
```

The collection supports `Add`, `Remove(instance)`, `Remove<T>()`, `Clear`, `Count`, and `IEnumerable<ICustomization>`.

### Per-Test Customization (`[CustomizeWith]`)

Apply a customization to a specific test method or class:

```csharp
[CustomizeWith(typeof(MyCustomization))]
[Theory, AutoNSubstituteData]
public void MyTest(MyService sut) { ... }
```

### FluentAssertions Extensions (`Assertions/`)

The library extends FluentAssertions with custom assertion methods. All live in `src/Cabazure.Test/Assertions/` and are in the `Cabazure.Test` namespace (no extra `using` needed beyond `using Cabazure.Test;`).

#### `JsonElementAssertions`

Assertions for `System.Text.Json.JsonElement`, accessed via `.Should()` on a `JsonElement`:

- `.BeEquivalentTo(JsonElement expected)` — compares serialized JSON representations (whitespace-normalized).
- `.NotBeEquivalentTo(JsonElement expected)` — inverse.

```csharp
jsonElement.Should().BeEquivalentTo(expectedElement);
```

#### `DateTimeOffsetExtensions` + `CabazureAssertionOptions`

Overloads of `BeCloseTo`/`NotBeCloseTo` that use a configurable default precision instead of requiring an explicit `TimeSpan`:

- `.BeCloseTo(DateTimeOffset nearbyTime)` — uses `CabazureAssertionOptions.DateTimeOffsetPrecision` (default: 1 second).
- `.BeCloseTo(DateTimeOffset nearbyTime, int precisionMilliseconds)` — explicit millisecond precision.
- `.NotBeCloseTo(DateTimeOffset distantTime)` — inverse, default precision.
- `.NotBeCloseTo(DateTimeOffset distantTime, int precisionMilliseconds)` — inverse, explicit.

Configure the default precision in a `[ModuleInitializer]`:

```csharp
CabazureAssertionOptions.DateTimeOffsetPrecision = TimeSpan.FromMilliseconds(100);
```

#### `StringContentExtensions`

Format-ignorant string comparison on `StringAssertions` (call `.Should()` on any `string`):

| Method | Normalisation |
|--------|---------------|
| `.BeSimilarTo(string expected)` | Trim + collapse `\s+` → single space |
| `.NotBeSimilarTo(string expected)` | Inverse |
| `.BeXmlEquivalentTo(string expected)` | `XDocument.Parse` → `ToString(SaveOptions.DisableFormatting)` |
| `.NotBeXmlEquivalentTo(string expected)` | Inverse |
| `.BeJsonEquivalentTo(string expected)` | `JsonDocument.Parse` → `JsonSerializer.Serialize` |
| `.NotBeJsonEquivalentTo(string expected)` | Inverse |

`BeXmlEquivalentTo`/`BeJsonEquivalentTo` propagate `XmlException`/`JsonException` on invalid input — they do **not** swallow parse errors.

```csharp
responseBody.Should().BeJsonEquivalentTo("""{"name":"Alice","age":30}""");
xmlPayload.Should().BeXmlEquivalentTo("<root><item>1</item></root>");
"hello   world".Should().BeSimilarTo("hello world");
```

### NSubstitute Helpers

#### `FluentArg.Match<T>()`

Creates a NSubstitute argument matcher backed by a FluentAssertions assertion. Use in `Received()` call verifications:

```csharp
substitute.Received().Process(FluentArg.Match<Request>(r =>
    r.Name.Should().Be("Alice")));
```

The method is `Match<T>` (not `Matching`). Assertion failures surface in `ReceivedCallsException` via `IDescribeNonMatches`.

#### `WaitForReceivedExtensions`

Async helpers for verifying calls in concurrent/async code:

- `substitute.WaitForReceived(x => x.Method(args), timeout?)` — waits until the call is received.
- `substitute.WaitForReceivedWithAnyArgs(x => x.Method(default), timeout?)` — any-args variant.

Default timeout: `WaitForReceivedExtensions.DefaultTimeout` (10 seconds). Override in a `[ModuleInitializer]`.

#### `ReceivedCallExtensions`

Extensions on `ICall` for inspecting received calls (argument extraction, etc.).

#### `ProtectedMethodExtensions`

Extensions for invoking `protected` methods on substitutes via reflection, enabling assertion on protected virtual behaviour.

### xUnit 3 Specifics

- Prefer `[ModuleInitializer]` (via `AssemblyInitializer`) over `IClassFixture` static constructors for global test setup.
- Never use `Console.Write` in tests — use `ITestOutputHelper`.
- For strongly-typed theory data sets, extend `TheoryData<T>`.

## Project Structure

```
src/
  Cabazure.Test/
    Assertions/          ← FA extension methods (JsonElementAssertions, DateTimeOffsetExtensions, StringContentExtensions)
    Attributes/          ← [AutoNSubstituteData] family and supporting attributes
    Customizations/      ← AutoFixture customizations (AutoNSubstituteCustomization, etc.)
    AssemblyInfo.cs         ← [InternalsVisibleTo] declarations
    CabazureAssertionOptions.cs  ← Global precision options (lives in DateTimeOffsetExtensions.cs)
    FixtureCustomizationCollection.cs
    FixtureFactory.cs       ← Public fixture factory
    FluentArg.cs            ← NSubstitute × FluentAssertions argument matcher
    ProtectedMethodExtensions.cs
    ReceivedCallExtensions.cs
    WaitForReceivedExtensions.cs
tests/
  Cabazure.Test.Tests/   ← Library's own tests — uses the library (dogfooding required)
```

## Code Style

- Use `var` when the type is obvious from the right-hand side.
- Prefer expression-bodied members for simple getters and single-line methods.
- Every public API member requires an XML doc comment (`<summary>`, `<typeparam>`, `<param>`, `<returns>`).
- Follow Microsoft's C# naming conventions (PascalCase types/members, camelCase locals and private fields — no underscore prefix).
- Avoid abbreviations in public APIs.
- Target nullable reference types enabled (`<Nullable>enable</Nullable>`).

## Testing This Library

The library tests itself — `Cabazure.Test.Tests` uses `[AutoNSubstituteData]` from the library under development. This is both a quality signal and a dogfooding requirement. Do not add external mocking frameworks to the test project.

## Commit Messages

All commits must follow [Conventional Commits](https://www.conventionalcommits.org/), using **focused commits** — one concern per commit, no mixing unrelated changes:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `ci`

**Scopes** (use the relevant one):
- `assertions` — FluentAssertions extension methods (`Assertions/` folder)
- `fixture` — FixtureFactory and fixture configuration
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
