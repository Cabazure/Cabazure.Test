using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for <see cref="StringAssertions"/> that provide format-ignorant
/// string comparison, including whitespace normalization, XML equivalence, and JSON equivalence.
/// </summary>
public static class StringContentExtensions
{
    /// <summary>
    /// Asserts that the string is similar to the expected string after normalizing whitespace.
    /// Normalization trims leading/trailing whitespace and collapses internal whitespace runs to a single space.
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The expected string to compare against after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<StringAssertions> BeSimilarTo(
        this StringAssertions assertions,
        string? expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeWhitespace(assertions.Subject);
        var normalizedExpected = NormalizeWhitespace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject == normalizedExpected)
            .FailWith(
                "Expected string to be similar to {0}{reason}, but found {1}.",
                normalizedExpected,
                normalizedSubject);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the string is not similar to the expected string after normalizing whitespace.
    /// Normalization trims leading/trailing whitespace and collapses internal whitespace runs to a single space.
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The string that the subject should not match after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<StringAssertions> NotBeSimilarTo(
        this StringAssertions assertions,
        string? expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeWhitespace(assertions.Subject);
        var normalizedExpected = NormalizeWhitespace(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject != normalizedExpected)
            .FailWith(
                "Expected string not to be similar to {0}{reason}, but they were equivalent after whitespace normalization.",
                normalizedExpected);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the string is XML-equivalent to the expected XML string.
    /// Equivalence is determined by parsing both strings as <see cref="XDocument"/> and
    /// comparing their serialized form with formatting disabled.
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The expected XML string to compare against after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown when <paramref name="expected"/> or the subject string is not valid XML.
    /// </exception>
    public static AndConstraint<StringAssertions> BeXmlEquivalentTo(
        this StringAssertions assertions,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeXml(assertions.Subject);
        var normalizedExpected = NormalizeXml(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject == normalizedExpected)
            .FailWith(
                "Expected XML to be equivalent to {0}{reason}, but found {1}.",
                normalizedExpected,
                normalizedSubject);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the string is not XML-equivalent to the expected XML string.
    /// Equivalence is determined by parsing both strings as <see cref="XDocument"/> and
    /// comparing their serialized form with formatting disabled.
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The XML string that the subject should not match after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    /// <exception cref="System.Xml.XmlException">
    /// Thrown when <paramref name="expected"/> or the subject string is not valid XML.
    /// </exception>
    public static AndConstraint<StringAssertions> NotBeXmlEquivalentTo(
        this StringAssertions assertions,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeXml(assertions.Subject);
        var normalizedExpected = NormalizeXml(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject != normalizedExpected)
            .FailWith(
                "Expected XML not to be equivalent to {0}{reason}, but they were equivalent after normalization.",
                normalizedExpected);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the string is JSON-equivalent to the expected JSON string.
    /// Equivalence is determined by parsing both strings as <see cref="JsonDocument"/> and
    /// comparing their serialized form (whitespace-normalized).
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The expected JSON string to compare against after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    /// <exception cref="JsonException">
    /// Thrown when <paramref name="expected"/> or the subject string is not valid JSON.
    /// </exception>
    public static AndConstraint<StringAssertions> BeJsonEquivalentTo(
        this StringAssertions assertions,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeJson(assertions.Subject);
        var normalizedExpected = NormalizeJson(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject == normalizedExpected)
            .FailWith(
                "Expected JSON to be equivalent to {0}{reason}, but found {1}.",
                normalizedExpected,
                normalizedSubject);

        return new AndConstraint<StringAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that the string is not JSON-equivalent to the expected JSON string.
    /// Equivalence is determined by parsing both strings as <see cref="JsonDocument"/> and
    /// comparing their serialized form (whitespace-normalized).
    /// </summary>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="expected">The JSON string that the subject should not match after normalization.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    /// <exception cref="JsonException">
    /// Thrown when <paramref name="expected"/> or the subject string is not valid JSON.
    /// </exception>
    public static AndConstraint<StringAssertions> NotBeJsonEquivalentTo(
        this StringAssertions assertions,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        var normalizedSubject = NormalizeJson(assertions.Subject);
        var normalizedExpected = NormalizeJson(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(normalizedSubject != normalizedExpected)
            .FailWith(
                "Expected JSON not to be equivalent to {0}{reason}, but they were equivalent after normalization.",
                normalizedExpected);

        return new AndConstraint<StringAssertions>(assertions);
    }

    private static string NormalizeWhitespace(string? s)
        => s is null ? string.Empty : Regex.Replace(s.Trim(), @"\s+", " ");

    private static string NormalizeXml(string? s)
        => s is null ? string.Empty : XDocument.Parse(s).ToString(SaveOptions.DisableFormatting);

    private static string NormalizeJson(string? s)
        => s is null ? string.Empty : JsonElementHelper.ToCompactString(JsonDocument.Parse(s).RootElement);
}
