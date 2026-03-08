using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Cabazure.Test;

/// <summary>
/// Contains FluentAssertions-style assertions for <see cref="JsonElement"/>.
/// </summary>
public sealed class JsonElementAssertions
{
    private readonly JsonElement subject;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementAssertions"/> class.
    /// </summary>
    /// <param name="subject">The <see cref="JsonElement"/> to assert against.</param>
    public JsonElementAssertions(JsonElement subject)
    {
        this.subject = subject;
    }

    /// <summary>
    /// Asserts that the <see cref="JsonElement"/> is equivalent to the expected element.
    /// Equivalence is determined by comparing the serialized JSON representations
    /// (whitespace-normalized).
    /// </summary>
    /// <param name="expected">The expected <see cref="JsonElement"/>.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public AndConstraint<JsonElementAssertions> BeEquivalentTo(
        JsonElement expected,
        string because = "",
        params object[] becauseArgs)
    {
        var subjectJson = JsonElementHelper.ToCompactString(subject);
        var expectedJson = JsonElementHelper.ToCompactString(expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(subjectJson == expectedJson)
            .FailWith("Expected JSON to be equivalent to {0}{reason}, but found {1}.", expectedJson, subjectJson);

        return new AndConstraint<JsonElementAssertions>(this);
    }

    /// <summary>
    /// Asserts that the <see cref="JsonElement"/> is equivalent to the expected JSON string.
    /// The string is parsed into a <see cref="JsonElement"/> and then compared.
    /// Equivalence is determined by comparing the serialized JSON representations
    /// (whitespace-normalized).
    /// </summary>
    /// <param name="expectedJson">The expected JSON string.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    /// <exception cref="JsonException">Thrown when <paramref name="expectedJson"/> is not valid JSON.</exception>
    public AndConstraint<JsonElementAssertions> BeEquivalentTo(
        string expectedJson,
        string because = "",
        params object[] becauseArgs)
    {
        var expected = JsonDocument.Parse(expectedJson).RootElement;
        return BeEquivalentTo(expected, because, becauseArgs);
    }
}

/// <summary>
/// Extension methods for creating <see cref="JsonElementAssertions"/> from a <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementAssertionsExtensions
{
    /// <summary>
    /// Returns a <see cref="JsonElementAssertions"/> object that can be used to assert
    /// the current <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="subject">The <see cref="JsonElement"/> to assert against.</param>
    /// <returns>A <see cref="JsonElementAssertions"/> instance.</returns>
    public static JsonElementAssertions Should(this JsonElement subject)
    {
        return new JsonElementAssertions(subject);
    }
}
