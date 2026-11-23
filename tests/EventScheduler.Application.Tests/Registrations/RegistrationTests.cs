using EventScheduler.Application.Registrations;

namespace EventScheduler.Application.Tests.Registrations;

public sealed class RegistrationTests
{
    [Theory]
    [InlineData("ada@example.com", "ada@example.com")]
    [InlineData("Ada@Example.COM", "ada@example.com")]
    [InlineData("  ada@example.com\t", "ada@example.com")]
    [InlineData("  ADA@Example.com ", "ada@example.com")]
    public void NormalizeEmail_MixedCaseOrSurroundingWhitespace_ReturnsTrimmedLowerCase(string email, string expected)
    {
        // Arrange
        var input = email;

        // Act
        var normalized = Registration.NormalizeEmail(input);

        // Assert
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void NormalizeEmail_DifferentCasingOfSameAddress_ReturnsSameValue()
    {
        // Arrange
        const string Lower = "grace@example.com";
        const string Mixed = " Grace@EXAMPLE.com";

        // Act
        var normalizedLower = Registration.NormalizeEmail(Lower);
        var normalizedMixed = Registration.NormalizeEmail(Mixed);

        // Assert
        Assert.Equal(normalizedLower, normalizedMixed);
    }
}
