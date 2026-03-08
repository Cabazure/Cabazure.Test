# Cabazure.Test

> Ergonomic .NET unit testing — xUnit 3, NSubstitute, AutoFixture, and FluentAssertions in one package.

[![NuGet](https://img.shields.io/nuget/v/Cabazure.Test.svg)](https://www.nuget.org/packages/Cabazure.Test)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/Cabazure/Cabazure.Test/actions/workflows/build.yml/badge.svg)](https://github.com/Cabazure/Cabazure.Test/actions/workflows/build.yml)

---

## What is it?

**Cabazure.Test** is a spiritual successor to [Atc.Test](https://github.com/atc-net/atc-test), rebuilt from the ground up for xUnit 3. It bundles xUnit 3, NSubstitute, AutoFixture, and FluentAssertions into a single package so you can focus on writing tests instead of wiring up infrastructure.

Two things set this library apart. First, project-wide fixture customizations are registered explicitly in a `[ModuleInitializer]` method — there is no reflection-based auto-discovery scanning assemblies for special attributes. This makes startup deterministic, fast, and straightforward to reason about. Second, interfaces and abstract classes are automatically substituted by NSubstitute everywhere — no manual `Substitute.For<T>()` calls required.

---

## Installation

```
dotnet add package Cabazure.Test
```

The package includes all dependencies needed for testing:
- **xUnit 3** — test framework
- **AutoFixture & AutoFixture.Xunit3** — fixture generation and `[Frozen]` attribute
- **NSubstitute** — automatic mocking
- **FluentAssertions** — assertion library

No additional packages are required to get started.

---

## Quick Start

### Using `FixtureFactory` in a `[Fact]` test

`FixtureFactory.Create()` returns a fully configured `IFixture` with NSubstitute auto-substitution. Call `fixture.Freeze<T>()` to register a shared instance before creating your SUT.

```csharp
using AutoFixture;
using Cabazure.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class OrderServiceTests
{
    [Fact]
    public void ProcessOrder_CallsRepository_WithOrder()
    {
        // Arrange
        var fixture = FixtureFactory.Create();
        var repository = fixture.Freeze<IOrderRepository>(); // NSubstitute substitute, shared with SUT
        var sut = fixture.Create<OrderService>();             // OrderService(IOrderRepository) auto-wired
        var order = fixture.Create<Order>();

        // Act
        sut.ProcessOrder(order);

        // Assert
        repository.Received(1).Save(order);
    }
}
```

### Using `[Theory, AutoNSubstituteData]` with `[Frozen]`

For theory-driven tests, `[AutoNSubstituteData]` resolves all parameters from a fixture. Mark upstream dependencies with `[Frozen]` so they are registered before the SUT is constructed — the same instance flows through to the SUT's constructor.

```csharp
using AutoFixture.Xunit3;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class NotificationServiceTests
{
    [Theory, AutoNSubstituteData]
    public void SendWelcome_PublishesMessage_ToEmailSender(
        [Frozen] IEmailSender emailSender,  // frozen first — injected into sut
        NotificationService sut,            // created with emailSender already wired
        string recipientAddress)            // random test data from AutoFixture
    {
        // Act
        sut.SendWelcome(recipientAddress);

        // Assert
        emailSender.Received(1).Send(Arg.Is<Email>(e => e.To == recipientAddress));
    }
}
```

> **Tip:** Parameters are resolved left to right. Place `[Frozen]` parameters before any types that depend on them.

### Using `[InlineAutoNSubstituteData]`

Combine explicit inline values with auto-generated parameters. Inline values fill leading parameters; the rest are resolved from the fixture.

```csharp
using AutoFixture.Xunit3;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class DiscountServiceTests
{
    [Theory]
    [InlineAutoNSubstituteData("SAVE10", 0.10)]
    [InlineAutoNSubstituteData("SAVE20", 0.20)]
    public void Apply_CalculatesCorrectDiscount(
        string couponCode,
        double rate,
        [Frozen] IDiscountRepository repository,
        DiscountService sut)
    {
        repository.GetRate(couponCode).Returns(rate);
        sut.Apply(couponCode).Should().Be(rate);
    }
}
```

### Project-wide customizations via `FixtureFactory.Customizations`

Register a customization once for the whole test assembly using `[ModuleInitializer]`. It is applied to every fixture created by any data attribute.

```csharp
using System.Runtime.CompilerServices;

internal static class TestAssemblyInitializer
{
    [ModuleInitializer]
    public static void Initialize()
        => FixtureFactory.Customizations.Add(new MyDomainCustomization());
}
```

### Per-test customization via `[CustomizeWith]`

Apply a customization to a single test method or an entire test class without touching the global registry.

```csharp
using AutoFixture.Xunit3;
using Cabazure.Test.Customizations;
using FluentAssertions;
using NSubstitute;
using Xunit;

[CustomizeWith(typeof(MyProjectCustomization))]
public class PaymentServiceTests
{
    [Theory, AutoNSubstituteData]
    [CustomizeWith(typeof(MyTestCustomization))]
    public void Charge_InvokesGateway([Frozen] IPaymentGateway gateway, PaymentService sut)
    {
        sut.Charge(100m);
        gateway.Received(1).Process(100m);
    }
}
```

---

## Freezing Fixtures with `[Frozen]`

The `[Frozen]` attribute comes from the `AutoFixture.Xunit3` package, which is a transitive dependency of `Cabazure.Test`. When you mark a parameter with `[Frozen]` in a theory test, AutoFixture registers that instance in the fixture before resolving subsequent parameters, ensuring the same instance is injected everywhere it is needed.

Add the using directive:

```csharp
using AutoFixture.Xunit3;
```

### Example: Freezing a Dependency

```csharp
[Theory, AutoNSubstituteData]
public void Process_UsesRepository(
    [Frozen] IRepository repo,      // Frozen first — registered in the fixture
    Service service)                // Created with repo already wired in
{
    var item = new Item { Id = 1 };
    service.Process(item);
    repo.Received(1).Save(item);
}
```

### Advanced: Matching Behavior

For fine-grained control over which types match the frozen instance, use the `Matching` enum:

```csharp
[Theory, AutoNSubstituteData]
public void Handler_UsesInterfaces(
    [Frozen(Matching.ImplementedInterfaces)] IHandler handler,
    MyClass sut)
{
    sut.DoWork();
    handler.Received(1).Execute();
}
```

**Key points:**
- Frozen parameters must appear **before** any parameters that depend on them — parameters are resolved **left to right**.
- Only reference types are frozen; value types are ignored.
- Works with all data attributes: `[AutoNSubstituteData]`, `[InlineAutoNSubstituteData]`, `[MemberAutoNSubstituteData]`, `[ClassAutoNSubstituteData]`.

---

## Argument Matching with `FluentArg`

`FluentArg.Match<T>` bridges FluentAssertions into NSubstitute's argument matching pipeline. Use it when you want to verify a received call with rich FluentAssertions assertions on the argument, rather than simple equality.

```csharp
using Cabazure.Test;
using FluentAssertions;
using NSubstitute;

[Fact]
public void Submit_SendsCorrectRequest()
{
    var service = Substitute.For<IOrderService>();
    var sut = new OrderHandler(service);

    sut.Submit("Alice", 100);

    service.Received(1).Process(
        FluentArg.Match<OrderRequest>(r =>
        {
            r.CustomerName.Should().Be("Alice");
            r.Amount.Should().Be(100);
        }));
}
```

When the assertion fails, the FluentAssertions failure message is included in NSubstitute's `ReceivedCallsException`, so you get precise feedback on exactly which field didn't match.

---

## Features

| Feature | Description |
|---|---|
| `FluentArg.Match<T>` | NSubstitute argument matcher that uses FluentAssertions assertions. Assertion failure messages surface in `ReceivedCallsException`. |
| `FixtureFactory` | `Create()` and `Create(ICustomization[])` — returns a configured `IFixture` for use in `[Fact]` tests. |
| `[AutoNSubstituteData]` | xUnit 3 `DataAttribute` — all theory parameters are auto-generated from the fixture. |
| `[InlineAutoNSubstituteData]` | Inline values fill leading parameters; remaining parameters are auto-generated by the fixture. |
| `[MemberAutoNSubstituteData]` | Rows from a static member fill leading parameters; remaining parameters are auto-generated by the fixture. |
| `[ClassAutoNSubstituteData]` | Rows from an `IEnumerable<object[]>` class fill leading parameters; remaining are auto-generated. |
| `[Frozen]` | Freezes a parameter in the fixture so all later parameters that depend on the same type receive the same instance. |
| `FixtureFactory.Customizations` | Ordered collection of project-wide customizations, pre-seeded with `AutoNSubstituteCustomization`. Supports `Add`, `Remove`, `Remove<T>`, `Clear`, and enumeration. |
| `[CustomizeWith]` | Per-method or per-class attribute that applies an `ICustomization` on top of any project-wide registrations. |
| `RecursionCustomization` | Replaces `ThrowingRecursionBehavior` with `OmitOnRecursionBehavior` so recursive object graphs don't throw. |
| `ImmutableCollectionCustomization` | Enables fixture creation of `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableDictionary<,>`, and other immutable collections. |
| `CancellationTokenCustomization` | Provides non-cancelled `CancellationToken` parameters in theory tests (`new CancellationToken(false)`), fixing AutoFixture's default. |
| `DateOnlyTimeOnlyCustomization` | Enables reliable creation of `DateOnly` and `TimeOnly` values derived from a random `DateTime`. |
| `JsonElementCustomization` | Included-by-default customization that enables creation of `System.Text.Json.JsonElement` instances. Produces a random JSON string by default; configurable via constructor overloads. |
| `JsonSerializerOptionsCustomization` | Prevents `ArgumentOutOfRangeException` when AutoFixture tries to set `IndentCharacter` to a random char. Produces `new JsonSerializerOptions()` by default. |
| `InvokeProtected` / `InvokeProtectedAsync` | Extension methods for invoking protected instance methods via reflection — void, typed-return, and async variants. Useful for testing Template Method patterns and protected virtual hooks without subclassing. |
| `BeSimilarTo<T>` | Whitespace-normalized string comparison (collapses whitespace/newlines) |
| `BeXmlEquivalentTo<T>` | XML structural comparison ignoring formatting |
| `BeJsonEquivalentTo<T>` | JSON structural comparison ignoring formatting |
| Auto-substitution | Interfaces and abstract classes are automatically replaced with NSubstitute substitutes everywhere — no manual `Substitute.For<T>()` required. Un-setup method calls return NSubstitute defaults (`null`, `0`, `false`, completed `Task`). Use `.Returns()` to configure explicit return values. |

---

## Customizations

### `RecursionCustomization`

Replaces AutoFixture's default `ThrowingRecursionBehavior` with `OmitOnRecursionBehavior` so recursive object graphs don't throw. **Included by default.**

### `ImmutableCollectionCustomization`

Enables creation of `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableHashSet<T>`, `ImmutableDictionary<,>`, `ImmutableQueue<T>`, `ImmutableStack<T>`, and `ImmutableSortedSet<T>`. Without this customization, AutoFixture throws `ObjectCreationException` for most immutable types. **Included by default.**

### `CancellationTokenCustomization`

Provides properly initialized `CancellationToken` parameters in theory tests. AutoFixture's default behavior creates an already-cancelled token with `CanBeCanceled = false`, which is rarely useful for testing. This customization supplies `new CancellationToken(false)` instead — a token that is not cancelled but cannot be cancelled.

**Included by default.** You can:

- **Use runner-scoped cancellation** in test code: Access `TestContext.Current.CancellationToken` directly (xUnit 3's idiomatic approach). This token is cancelled if the test run is aborted.
- **Create per-test cancellation** for testing cancellation handling: Build a `CancellationTokenSource` in the test body:
  ```csharp
  var cts = new CancellationTokenSource();
  var token = cts.Token;
  // Use and control token as needed
  ```
- **Opt out** of the customization if you need different behavior:
  ```csharp
  [ModuleInitializer]
  public static void Initialize()
      => FixtureFactory.Customizations.Remove<CancellationTokenCustomization>();
  ```

### `DateOnlyTimeOnlyCustomization`

Enables reliable creation of `DateOnly` and `TimeOnly` values. AutoFixture cannot construct `DateOnly` by default (it generates invalid year/month/day combinations), and while `TimeOnly` technically works, AutoFixture produces near-zero tick values making it useless for tests. This customization derives both types from a randomly generated `DateTime`.

**Included by default.** Remove it with `FixtureFactory.Customizations.Remove<DateOnlyTimeOnlyCustomization>()` if you need different behavior.

### `JsonElementCustomization`

Enables creation of `System.Text.Json.JsonElement` instances. AutoFixture cannot construct `JsonElement` by default because it requires a `ref Utf8JsonReader` parameter. **Included by default** — produces a random JSON string using `Utf8JsonWriter` so it works regardless of whether `JsonSerializerIsReflectionEnabledByDefault` is set to `false`.

Three constructor overloads let you control the generated value:

```csharp
// Default — random JSON string (reflection-free)
new JsonElementCustomization()

// Raw JSON string factory — you return the JSON, parsing/cloning is automatic
new JsonElementCustomization(
    f => $"{{\"{f.Create<string>()}\": \"{f.Create<string>()}\"}}")

// Element factory — you produce the JsonElement directly with full control
new JsonElementCustomization(f =>
{
    using var buffer = new MemoryStream();
    using (var writer = new Utf8JsonWriter(buffer))
    {
        writer.WriteStartObject();
        writer.WriteString(f.Create<string>(), f.Create<string>());
        writer.WriteEndObject();
    }
    return JsonDocument.Parse(buffer.ToArray()).RootElement.Clone();
})
```

To override the default in your project, remove the built-in instance and add your own:

```csharp
[ModuleInitializer]
public static void Initialize()
{
    FixtureFactory.Customizations.Remove<JsonElementCustomization>();
    FixtureFactory.Customizations.Add(
        new JsonElementCustomization(f => f.Create<int>().ToString()));
}
```

### `JsonSerializerOptionsCustomization`

Prevents an `ArgumentOutOfRangeException` when AutoFixture tries to construct `JsonSerializerOptions`. The `IndentCharacter` property (added in .NET 8) only accepts `' '` (space) or `'\t'` (tab); AutoFixture generates a random `char` for it, which always fails with:

> `"Supported indentation characters are space and horizontal tab."`

**Included by default.** Produces `new JsonSerializerOptions()` with all framework defaults. Override for a single fixture with `fixture.Inject(...)`, or project-wide by removing and re-registering:

```csharp
[ModuleInitializer]
public static void Initialize()
{
    FixtureFactory.Customizations.Remove<JsonSerializerOptionsCustomization>();
    FixtureFactory.Customizations.Add(
        _ => new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
```

AutoFixture cannot construct some types by default — `JsonElement`, `DateOnly`, or your own domain types with special creation logic. Use `FixtureFactory.Customizations` to register custom factories and builders.

#### Inline delegate (simplest)

Provide a factory function that receives the fixture and returns a fully constructed instance:

```csharp
FixtureFactory.Customizations.Add<DateOnly>(
    f => DateOnly.FromDateTime(f.Create<DateTime>()));
```

The factory is invoked whenever AutoFixture needs to create a `DateOnly`, whether requested directly or as a constructor parameter.

#### Using `Build<T>()` for partial overrides

For types AutoFixture can already construct, use `Build<T>()` to fix specific properties without replacing the entire creation logic:

```csharp
FixtureFactory.Customizations.Add<Order>(
    f => f.Build<Order>()
        .With(o => o.Status, OrderStatus.Pending)
        .Create());
```

#### Reusable customization class

For customizations shared across many tests, subclass `TypeCustomization<T>`:

```csharp
public sealed class MoneyCustomization : TypeCustomization<Money>
{
    public MoneyCustomization()
        : base(f => new Money(f.Create<decimal>())) { }
}
```

Register it once in your test assembly initializer:

```csharp
[ModuleInitializer]
public static void Initialize()
{
    FixtureFactory.Customizations.Add(new MoneyCustomization());
}
```

#### Direct `ISpecimenBuilder` registration

For power users who need full control over specimen creation logic, implement `ISpecimenBuilder` and register it directly:

```csharp
FixtureFactory.Customizations.Add(new MyAdvancedSpecimenBuilder());
```

#### Overriding a built-in customization

To replace a built-in customization like `DateOnlyTimeOnlyCustomization` or `JsonElementCustomization`, remove the original first:

```csharp
// Remove the built-in first
FixtureFactory.Customizations.Remove<DateOnlyTimeOnlyCustomization>();

// Then add your replacement
FixtureFactory.Customizations.Add<DateOnly>(
    f => DateOnly.FromDateTime(f.Create<DateTime>().Date));
```

---

## Packages Included

| Package | Version |
|---|---|
| `xunit.v3` | 3.2.2 |
| `NSubstitute` | 5.3.0 |
| `AutoFixture` | 4.18.1 |
| `AutoFixture.AutoNSubstitute` | 4.18.1 |
| `FluentAssertions` | 7.0.0 |

All packages are exposed as transitive dependencies — you get full access to the xUnit, NSubstitute, AutoFixture, and FluentAssertions APIs without adding additional package references.

---

## Protected Methods

`ProtectedMethodExtensions` provides extension methods for invoking protected instance methods via reflection. This is useful when testing the [Template Method pattern](https://en.wikipedia.org/wiki/Template_method_pattern) or protected virtual hooks without creating a `TestableMyClass : MyClass` subclass per test.

```csharp
// Invoke a protected void method
sut.InvokeProtected("Reset");

// Invoke a protected method with a return value
var discount = sut.InvokeProtected<decimal>("CalculateDiscount", 100m);

// Invoke a protected async method (returns Task)
await sut.InvokeProtectedAsync("OnActivatedAsync", cancellationToken);

// Invoke a protected async method with a typed return value
var dto = await sut.InvokeProtectedAsync<OrderDto>("FetchOrderAsync", id, cancellationToken);
```

### Example

```csharp
using Cabazure.Test;
using FluentAssertions;
using Xunit;

public class OrderProcessorTests
{
    [Theory, AutoNSubstituteData]
    public void CalculateDiscount_Returns10Percent(OrderProcessor sut)
    {
        var result = sut.InvokeProtected<decimal>("CalculateDiscount", 100m);

        result.Should().Be(10m);
    }
}

public class OrderProcessor
{
    protected virtual decimal CalculateDiscount(decimal total) => total * 0.1m;
}
```

### Notes

- **Base class methods** — Methods are resolved with `FlattenHierarchy`, so protected methods defined on any base class in the hierarchy are found automatically.
- **Overload resolution** — Overloads are matched by argument count and type compatibility.
- **Missing methods** — Throws `MissingMethodException` with a descriptive message (including the expected parameter types) if no matching method is found.
- **Exception transparency** — Original exceptions are surfaced directly via `ExceptionDispatchInfo`, not wrapped in `TargetInvocationException`, keeping stack traces and assertion errors intact.

---

## FluentAssertions Extensions

**Cabazure.Test** extends FluentAssertions with domain-specific assertions for JSON and datetime operations. All extensions are available via `using Cabazure.Test;` — no additional using directives are required.

### JsonElement Comparison

`JsonElementAssertions` provides methods to compare `System.Text.Json.JsonElement` instances against other elements or raw JSON strings. Comparisons are performed by normalizing both sides through serialization, ensuring structure equivalence regardless of whitespace or key ordering in the source.

#### Comparing two JsonElements

```csharp
using Cabazure.Test;
using FluentAssertions;
using System.Text.Json;

var element1 = JsonDocument.Parse("""{"name":"Alice","age":30}""").RootElement;
var element2 = JsonDocument.Parse("""{"age":30,"name":"Alice"}""").RootElement;

element1.Should().BeEquivalentTo(element2);  // ✓ Passes — same content, different key order
```

#### Comparing a JsonElement against a JSON string

```csharp
using Cabazure.Test;
using FluentAssertions;
using System.Text.Json;

var element = JsonDocument.Parse("""{"status":"active"}""").RootElement;

element.Should().BeEquivalentTo("""{"status":"active"}""");  // ✓ Passes — direct comparison
```

#### Important notes

- **Array order is significant** — `[1, 2, 3]` and `[3, 2, 1]` are not equivalent.
- **Object key order is preserved** — Key order from the serialized JSON is maintained during comparison, but does not affect equivalence (only content matters).

### DateTimeOffset Precision

`DateTimeOffsetExtensions` provides `BeCloseTo` and `NotBeCloseTo` methods for asserting that two `DateTimeOffset` values are within a specified precision tolerance. A project-wide default precision can be configured once using `[ModuleInitializer]`.

#### Using default precision

By default, `CabazureAssertionOptions.DateTimeOffsetPrecision` is set to **1 second**. Assert two values without specifying a tolerance:

```csharp
using Cabazure.Test;
using FluentAssertions;

[Fact]
public void OrderTimestamp_IsRecent()
{
    var now = DateTimeOffset.UtcNow;
    var order = new Order { CreatedAt = now.AddMilliseconds(500) };

    order.CreatedAt.Should().BeCloseTo(now);  // ✓ Passes — within default 1 second
}
```

#### Using explicit precision

Provide a custom precision in milliseconds for a single assertion:

```csharp
using Cabazure.Test;
using FluentAssertions;

var time1 = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
var time2 = new DateTimeOffset(2026, 3, 10, 12, 0, 0, 100, TimeSpan.Zero);

time1.Should().BeCloseTo(time2, 200);  // ✓ Passes — within 200 milliseconds
time1.Should().NotBeCloseTo(time2, 50);  // ✓ Passes — difference is 100ms, beyond 50ms
```

#### Configuring project-wide precision

Set `CabazureAssertionOptions.DateTimeOffsetPrecision` once in a `[ModuleInitializer]` to change the default for all tests in the assembly:

```csharp
using System.Runtime.CompilerServices;
using Cabazure.Test;

internal static class TestAssemblyInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // All BeCloseTo/NotBeCloseTo calls now default to 100ms tolerance
        CabazureAssertionOptions.DateTimeOffsetPrecision = 100;
    }
}
```

### JsonElement Equivalency in DTOs

When a DTO contains a `JsonElement` property, `BeEquivalentTo` falls back to reference equality by default, causing false failures for semantically identical JSON. Register `JsonElementEquivalencyStep` to enable proper semantic comparison:

**Per-call:**
```csharp
result.Should().BeEquivalentTo(expected, opts => opts.UsingJsonElementComparison());
```

**Globally (in your module initializer):**
```csharp
[ModuleInitializer]
public static void Initialize()
{
    AssertionOptions.AssertEquivalencyUsing(opts => opts.UsingJsonElementComparison());
}
```

### Allowing Empty Objects in BeEquivalentTo

When testing serialization round-trips across many DTO types, some may have no public properties. FluentAssertions 7.x throws `InvalidOperationException: "No members were found for comparison..."` in this case.

Use `AllowingEmptyObjects()` to allow the assertion to pass for types with no public members:

```csharp
// Per-call
result.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());

// Global (in module initializer)
AssertionOptions.AssertEquivalencyUsing(opts => opts.AllowingEmptyObjects());
```

Types with members continue through FluentAssertions' normal equivalency pipeline unchanged.

---

## String Content Assertions

The `StringContentExtensions` class extends FluentAssertions' `StringAssertions` with three
format-ignorant comparison methods, each with a positive and negative form.

### Whitespace-Normalized Comparison

Compare strings ignoring formatting differences — multiple spaces, tabs, and newlines are
collapsed to a single space before comparison:

```csharp
using Cabazure.Test;
using FluentAssertions;

var subject = """
    Hello
    World
    """;

subject.Should().BeSimilarTo("Hello World");
```

### XML Content Comparison

Compare XML strings by structure and content, ignoring indentation and line endings:

```csharp
using Cabazure.Test;
using FluentAssertions;

var subject = """
    <root>
      <child value="42" />
    </root>
    """;

subject.Should().BeXmlEquivalentTo("<root><child value=\"42\" /></root>");
```

### JSON Content Comparison

Compare JSON strings by value, ignoring formatting:

```csharp
using Cabazure.Test;
using FluentAssertions;

var subject = """
    {
        "name": "Alice",
        "age": 30
    }
    """;

subject.Should().BeJsonEquivalentTo("""{"name":"Alice","age":30}""");
```

Each method has a `Not` counterpart (`NotBeSimilarTo`, `NotBeXmlEquivalentTo`, `NotBeJsonEquivalentTo`)
and supports the standard FluentAssertions `because`/`becauseArgs` parameters.

---

## Test Timeouts

xUnit 3 and the .NET BCL provide three complementary timeout patterns for different scenarios:

### Whole-Test Execution Limit

Use the `Timeout` parameter on `[Fact]` or `[Theory]` to set a maximum duration for the entire test:

```csharp
[Fact(Timeout = 5000)]  // Timeout in milliseconds
public void SlowOperation_CompletesInTime()
{
    var result = ExpensiveComputation();
    result.Should().Be(42);
}
```

If the test exceeds the timeout, xUnit 3 throws `TestTimeoutException`. This is useful for detecting hangs in synchronous code or ensuring performance thresholds.

### Per-Await Timeout

Use `Task.WaitAsync(TimeSpan)` (.NET 6+) to add a timeout to individual `await` expressions. This is ideal when a single async call might hang:

```csharp
using FluentAssertions;

[Fact]
public async Task ApiCall_ReturnsWithinTimeout()
{
    var cts = new CancellationTokenSource();
    var result = await SomeApiCall(cts.Token)
        .WaitAsync(TimeSpan.FromSeconds(2));  // Timeout on this specific call
    
    result.Should().NotBeNull();
}
```

### Waiting for NSubstitute Call Verification

Use `WaitForReceived` to poll until an asynchronous call is verified on a substitute:

```csharp
[Theory, AutoNSubstituteData]
public async Task Service_CallsRepository(IRepository repo, Service sut)
{
    await sut.ProcessAsync();
    
    // Waits up to 10 seconds for the call (configurable)
    await repo.WaitForReceived(r => r.Save(Arg.Any<Item>()));
}
```

The default timeout is `WaitForReceivedExtensions.DefaultTimeout` (10 seconds). Override it globally in a `[ModuleInitializer]`:

```csharp
using Cabazure.Test;

internal static class TestAssemblyInitializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        WaitForReceivedExtensions.DefaultTimeout = TimeSpan.FromSeconds(5);
    }
}
```

---

## Test Performance Tips

Real-world usage reveals a few patterns that can subtly hurt test performance or discoverability. Here are three tips for keeping your test suite fast and maintainable.

### 1. Prefer `[Frozen]` parameters over `FixtureFactory.Create()` in shared helpers

When a private helper method calls `FixtureFactory.Create()` directly, it bypasses any `[CustomizeWith]` customizations on the test class and creates a bare, uncustomized fixture. Instead, declare your dependencies as `[Frozen]` parameters on the theory method and pass them into the shared helper.

```csharp
// ❌ Anti-pattern: bypasses [CustomizeWith] and creates unnecessary fixtures
private static async Task VerifyProcessing<TRequest, TResponse>()
{
    var fixture = FixtureFactory.Create();
    var request = fixture.Create<TRequest>();
    var sut = new MyProcessor(fixture.Create<IDependency>());
    // ...
}

// ✅ Preferred: inject via [Theory] parameters, share logic via helper
[Theory, AutoNSubstituteData]
public async Task ProcessAsync_ShouldHandle(
    [Frozen] IDependency dep,
    MyProcessor sut,
    object request)
    => await VerifyProcessing(sut, dep, request);

private static async Task VerifyProcessing(MyProcessor sut, IDependency dep, object request) { ... }
```

### 2. Register domain-type customizations once via `[ModuleInitializer]`

If tests frequently create complex domain objects (DTOs with many properties, nested types), registering a project-level customization via `[ModuleInitializer]` means the customization is applied once at startup — eliminating per-fixture resolution overhead on every test. See the [Project-wide customizations](#project-wide-customizations-via-fixturefactorycustomizations) section for the registration pattern.

```csharp
internal static class TestAssemblyInitializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
        => FixtureFactory.Customizations.Add(new MyDomainCustomization());
}
```

Registered customizations are shared across every fixture created by any data attribute — no repeated setup, no per-test overhead.

### 3. Consolidate repetitive `[Fact]` methods with `[MemberAutoNSubstituteData]`

When a test class has many near-identical `[Fact]` methods that differ only in a type argument (e.g., verifying that a processor handles every request type), express them as a single `[Theory]` backed by a `TheoryData<Type>` member. This reduces test count in the runner output and makes the intent clearer.

```csharp
// ❌ Anti-pattern: 50 near-identical [Fact] methods
[Fact] public async Task CanHandle_AuthorizeRequest() => await VerifyCanHandle<AuthorizeRequest>();
[Fact] public async Task CanHandle_BootNotificationRequest() => await VerifyCanHandle<BootNotificationRequest>();
// ... 48 more

// ✅ Preferred: one [Theory] with typed member data
public static TheoryData<Type> RequestTypes =>
[
    typeof(AuthorizeRequest),
    typeof(BootNotificationRequest),
    // ...
];

[Theory, MemberAutoNSubstituteData(nameof(RequestTypes))]
public async Task CanHandle(Type requestType, MyProcessor sut) { ... }
```

---

## Compatibility

- **.NET 9+** (`net9.0`)
- **FluentAssertions 7.x** is included. FA 7 contains breaking changes from 6.x. If you are migrating from Atc.Test or another package that used FA 6, review the [FluentAssertions 7 migration guide](https://fluentassertions.com/upgradingtov7) before upgrading.

---

## License

[MIT](LICENSE) — © Ricky Kaare Engelharth

---

## Contributing

Contributions are welcome — open an issue or pull request on [GitHub](https://github.com/Cabazure/Cabazure.Test/issues).