# Migrating from Atc.Test to Cabazure.Test

This guide covers the steps to replace `Atc.Test` (2.x) with `Cabazure.Test` in an existing xUnit 3 project. Cabazure.Test ships several features as built-in defaults that previously required per-project boilerplate in Atc.Test, and renames several extension methods to better reflect their purpose.

---

## Step 1 — Update the project reference

In each test `.csproj`, replace the `Atc.Test` package reference with `Cabazure.Test`:

```xml
<!-- Remove -->
<PackageReference Include="Atc.Test" Version="2.0.17" />

<!-- Add -->
<PackageReference Include="Cabazure.Test" Version="x.y.z" />
```

If the project uses a `<Using>` element for a global using, update it:

```xml
<!-- Remove -->
<Using Include="Atc.Test" />

<!-- Add -->
<Using Include="Cabazure.Test" />
```

---

## Step 2 — Delete built-in customization files

Atc.Test required each test project to ship its own `JsonElementCustomization.cs` decorated with `[AutoRegister]`:

```csharp
[AutoRegister]
public class JsonElementCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var jsonElement = JsonDocument
            .Parse($"\"{fixture.Create<string>()}\"")
            .RootElement;
        fixture.Inject(jsonElement);
    }
}
```

**Delete this file.** `JsonElementCustomization` is now a built-in default in `FixtureFactory` and is applied automatically to every fixture.

The same applies to any other customization that exists purely to fix AutoFixture's inability to construct a standard .NET type. Check whether Cabazure.Test already covers it before porting it — see the [Customizations section of the README](README.md#customizations).

---

## Step 3 — Replace `[AutoRegister]` with `[ModuleInitializer]`

Atc.Test's `[AutoRegister]` attribute on an `ICustomization` class caused it to be applied project-wide at startup. Cabazure.Test uses the standard `[ModuleInitializer]` pattern instead:

```csharp
// Atc.Test — old
[AutoRegister]
public class MyProjectCustomization : ICustomization
{
    public void Customize(IFixture fixture) { ... }
}
```

```csharp
// Cabazure.Test — new
internal static class TestInitializer
{
    [ModuleInitializer]
    public static void Initialize()
        => FixtureFactory.Customizations.Add(new MyProjectCustomization());
}
```

> If the project had multiple `[AutoRegister]` customizations, add them all in a single `[ModuleInitializer]` method.

---

## Step 4 — API renames

The following extension methods have been renamed:

| Atc.Test | Cabazure.Test | Notes |
|---|---|---|
| `substitute.WaitForCall(x => x.Method(...))` | `substitute.WaitForReceived(x => x.Method(...))` | |
| `substitute.WaitForCallForAnyArgs(x => x.Method(...))` | `substitute.WaitForReceivedWithAnyArgs(x => x.Method(...))` | |
| `substitute.ReceivedCallWithArgument<T>()` | `substitute.ReceivedArg<T>()` | Returns `T` from the most recent matching call for further assertions |
| `options.CompareJsonElementUsingJson()` | `options.UsingJsonElementComparison()` | FluentAssertions `BeEquivalentTo` option |
| `sub.InvokeProtectedMethod(name, args)` | `sub.InvokeProtected(name, args)` | Synchronous protected methods |
| `sub.InvokeProtectedMethod(name, args)` | `sub.InvokeProtectedAsync(name, args)` | Asynchronous protected methods — previously the same method name |

---

## Step 5 — Replace `AddTimeout()` with BCL `.WaitAsync()`

Atc.Test shipped a `Task.AddTimeout()` extension for per-await timeouts. Cabazure.Test does not include this helper — use the .NET 6+ BCL method directly:

```csharp
// Atc.Test — old
await someTask.AddTimeout();

// Cabazure.Test — new
await someTask.WaitAsync(TimeSpan.FromSeconds(5));
```

If the project used `AddTimeout()` extensively, define a shared constant to avoid repeating the timeout value:

```csharp
internal static class TestTimeouts
{
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(5);
}

// Usage
await someTask.WaitAsync(TestTimeouts.Default);
```

---

## Step 6 — Replace `HasProperties()` with `AllowingEmptyObjects()`

Atc.Test provided a `HasProperties()` extension as a guard before calling `BeEquivalentTo` on types with no public properties (which FluentAssertions throws on without a guard):

```csharp
// Atc.Test — old
if (result.HasProperties())
    result.Should().BeEquivalentTo<object>(expected);
```

Cabazure.Test adds `AllowingEmptyObjects()` to FluentAssertions' equivalency options, removing the need for the conditional:

```csharp
// Cabazure.Test — new
result.Should().BeEquivalentTo<object>(expected, o => o.AllowingEmptyObjects());
```

To apply globally across all equivalency assertions in the project (recommended if many tests use it), register once in a `[ModuleInitializer]`:

```csharp
[ModuleInitializer]
public static void Initialize()
    => AssertionOptions.AssertEquivalencyUsing(o => o.AllowingEmptyObjects());
```

---

## General migration tips

- Prefer built-in Cabazure.Test helpers before writing bespoke specimen builders. Use `SpecimenRequestHelper.GetRequestType(request)` instead of duplicating request-matching helpers, and use `FixtureFactory.Customizations.Add<T>(...)` or `TypeCustomization<T>` when you only need a type-focused customization. Reach for a full `ISpecimenBuilder` only when you genuinely need lower-level specimen control.
- Use `FluentArg.Match<T>()` for single inline assertions where the verified argument is asserted immediately and not reused later. Keep `ReceivedArg<T>()` / `ReceivedArgs<T>()` when the captured value is reused, transformed, or asserted across a batch or multi-step flow.

One common cleanup is replacing an `Arg.Any<T>()` + later argument-extraction pattern with an inline matcher when the verification flow is equivalent:

```csharp
// Before
_ = substitute.Received(1).Handle(Arg.Any<MyRequest>());
var request = substitute.ReceivedArg<MyRequest>();
request.Id.Should().Be(expectedId);

// After
substitute.Received(1).Handle(
    FluentArg.Match<MyRequest>(request =>
        request.Id.Should().Be(expectedId)));
```

This is usually a good fit when the old Atc.Test pattern was `Arg.Any<T>()` followed by `ReceivedCallWithArgument<T>()`, or the Cabazure.Test equivalent `ReceivedArg<T>()`, and the extracted value was only used for that immediate assertion. If the test reuses the captured argument, transforms it first, or inspects a batch of received values, the post-call inspection style often stays clearer.

`FluentArg.Match<T>()` takes an assertion action, not a boolean predicate, so keep the checks inside FluentAssertions-style assertions.

---

## Quick-reference checklist

- [ ] Replace `<PackageReference Include="Atc.Test" .../>` with `<PackageReference Include="Cabazure.Test" .../>`
- [ ] Replace `<Using Include="Atc.Test" />` with `<Using Include="Cabazure.Test" />`
- [ ] Delete per-project `JsonElementCustomization.cs` files (now built-in)
- [ ] Convert `[AutoRegister]` customizations to `[ModuleInitializer]` + `FixtureFactory.Customizations.Add(...)`
- [ ] Rename `WaitForCall(...)` → `WaitForReceived(...)`
- [ ] Rename `WaitForCallForAnyArgs(...)` → `WaitForReceivedWithAnyArgs(...)`
- [ ] Rename `ReceivedCallWithArgument<T>()` → `ReceivedArg<T>()`
- [ ] Rename `CompareJsonElementUsingJson()` → `UsingJsonElementComparison()`
- [ ] Rename `InvokeProtectedMethod(...)` → `InvokeProtected(...)` (sync) or `InvokeProtectedAsync(...)` (async)
- [ ] Replace `task.AddTimeout()` → `task.WaitAsync(TimeSpan.FromSeconds(N))`
- [ ] Replace `if (obj.HasProperties()) { obj.Should().BeEquivalentTo(...); }` → `obj.Should().BeEquivalentTo(..., o => o.AllowingEmptyObjects())`
