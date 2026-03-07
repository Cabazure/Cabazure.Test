# Cabazure.Test

> Ergonomic .NET unit testing — xUnit 3, NSubstitute, AutoFixture, and FluentAssertions in one package.

[![NuGet](https://img.shields.io/nuget/v/Cabazure.Test.svg)](https://www.nuget.org/packages/Cabazure.Test)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/Cabazure/Cabazure.Test/actions/workflows/build.yml/badge.svg)](https://github.com/Cabazure/Cabazure.Test/actions/workflows/build.yml)

---

## What is it?

**Cabazure.Test** is a spiritual successor to [Atc.Test](https://github.com/atc-net/atc-test), rebuilt from the ground up for xUnit 3. It bundles xUnit 3, NSubstitute, AutoFixture, and FluentAssertions into a single package so you can focus on writing tests instead of wiring up infrastructure.

The key differentiator is xUnit 3's `[ModuleInitializer]` pattern — assembly-level setup is handled automatically, with no static constructors or manual bootstrapping required. Interfaces and abstract classes are substituted by NSubstitute automatically, everywhere.

---

## Installation

```
dotnet add package Cabazure.Test
```

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
using Cabazure.Test.Attributes;
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

## Features

| Feature | Description |
|---|---|
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
| `DateOnlyTimeOnlyCustomization` | Enables reliable creation of `DateOnly` and `TimeOnly` values derived from a random `DateTime`. |
| `JsonElementCustomization` | Opt-in customization that enables creation of `System.Text.Json.JsonElement` instances. |
| Auto-substitution | Interfaces and abstract classes are automatically replaced with NSubstitute substitutes everywhere — no manual `Substitute.For<T>()` required. |

---

## Customizations

### `RecursionCustomization`

Replaces AutoFixture's default `ThrowingRecursionBehavior` with `OmitOnRecursionBehavior` so recursive object graphs don't throw. **Included by default.**

### `ImmutableCollectionCustomization`

Enables creation of `ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableHashSet<T>`, `ImmutableDictionary<,>`, `ImmutableQueue<T>`, `ImmutableStack<T>`, and `ImmutableSortedSet<T>`. Without this customization, AutoFixture throws `ObjectCreationException` for most immutable types. **Included by default.**

### `DateOnlyTimeOnlyCustomization`

Enables reliable creation of `DateOnly` and `TimeOnly` values. AutoFixture cannot construct `DateOnly` by default (it generates invalid year/month/day combinations), and while `TimeOnly` technically works, AutoFixture produces near-zero tick values making it useless for tests. This customization derives both types from a randomly generated `DateTime`.

**Included by default.** Remove it with `FixtureFactory.Customizations.Remove<DateOnlyTimeOnlyCustomization>()` if you need different behavior.

### `JsonElementCustomization`

Enables creation of `System.Text.Json.JsonElement` instances. AutoFixture cannot construct `JsonElement` by default because it requires a `ref Utf8JsonReader` parameter. This customization creates a `JsonElement` representing a JSON object with a randomly generated key/value pair.

**Not included by default** — opt in via:

```csharp
FixtureFactory.Customizations.Add(new JsonElementCustomization());
```

Or apply per-test:

```csharp
var fixture = FixtureFactory.Create(new JsonElementCustomization());
var element = fixture.Create<JsonElement>();
```

### Custom Type Registration

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

## Compatibility

- **.NET 9+** (`net9.0`)
- **FluentAssertions 7.x** is included. FA 7 contains breaking changes from 6.x. If you are migrating from Atc.Test or another package that used FA 6, review the [FluentAssertions 7 migration guide](https://fluentassertions.com/upgradingtov7) before upgrading.

---

## License

[MIT](LICENSE) — © Ricky Kaare Engelharth

---

## Contributing

Contributions are welcome — open an issue or pull request on [GitHub](https://github.com/Cabazure/Cabazure.Test/issues).