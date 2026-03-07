using AutoFixture;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that provides a safe <see cref="CancellationToken"/> for use
/// in unit tests.
/// </summary>
/// <remarks>
/// <para>
/// By default, AutoFixture produces an already-cancelled <see cref="CancellationToken"/> because it
/// resolves the <c>bool</c> constructor parameter as <c>true</c>. This causes any system under test
/// that checks <see cref="CancellationToken.IsCancellationRequested"/> at entry to fail immediately —
/// a silent but severe form of test data poisoning.
/// </para>
/// <para>
/// This customization replaces that behaviour with <c>new CancellationToken(false)</c>, which is
/// equivalent to <see cref="CancellationToken.None"/>: the token is not cancelled and
/// <see cref="CancellationToken.CanBeCanceled"/> is <see langword="false"/>.
/// </para>
/// <para>
/// For tests that need to verify cancellation behaviour, create a <see cref="CancellationTokenSource"/>
/// directly in the test body and pass <c>cts.Token</c> to the system under test.
/// For runner-scoped cancellation (cancelled if the test run is aborted), use
/// <c>TestContext.Current.CancellationToken</c> from xUnit 3.
/// </para>
/// <para>
/// To opt out of this customization, remove it via
/// <c>FixtureFactory.Customizations.Remove&lt;CancellationTokenCustomization&gt;()</c>
/// from your <c>[ModuleInitializer]</c>.
/// </para>
/// </remarks>
public sealed class CancellationTokenCustomization : TypeCustomization<CancellationToken>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancellationTokenCustomization"/> class.
    /// </summary>
    public CancellationTokenCustomization()
        : base(_ => new CancellationToken(false))
    {
    }
}
