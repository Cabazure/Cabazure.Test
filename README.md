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

### Using `SutFixture` in a `[Fact]` test

`SutFixture` creates instances with all unregistered dependencies automatically substituted via NSubstitute. Call `Freeze<T>()` to register a shared instance before creating your SUT.

```csharp
using Cabazure.Test.Fixture;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class OrderServiceTests
{
    [Fact]
    public void ProcessOrder_CallsRepository_WithOrder()
    {
        // Arrange
        var fixture = new SutFixture();
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

For theory-driven tests, `[AutoNSubstituteData]` resolves all parameters from a `SutFixture`. Mark upstream dependencies with `[Frozen]` so they are registered before the SUT is constructed — the same instance flows through to the SUT's constructor.

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

---

## Features

| Feature | Description |
|---|---|
| `SutFixture` | AutoFixture-backed fixture with NSubstitute auto-substitution. `Create<T>()`, `Freeze<T>()`, `Substitute<T>()`, `CreateMany<T>()`, and `Customize<T>()`. |
| `[AutoNSubstituteData]` | xUnit 3 `DataAttribute` that provides theory method arguments from a `SutFixture`. Works with any mix of value types, concrete classes, and interfaces. |
| `[Frozen]` | Parameter-level attribute that freezes a value in the fixture before subsequent parameters are resolved, ensuring consistent injection across the parameter list. |
| Auto-substitution | Interfaces and abstract classes are automatically replaced with NSubstitute substitutes everywhere — no manual `Substitute.For<T>()` required unless you want one that isn't registered. |

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
