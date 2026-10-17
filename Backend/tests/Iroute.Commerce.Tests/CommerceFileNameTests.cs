using Iroute.Commerce.Application.Csv;

namespace Iroute.Commerce.Tests;

public class CommerceFileNameTests
{
    [Theory]
    [InlineData("commerce_16102026.csv", true)]
    [InlineData("COMMERCE_01012024.CSV", true)]
    [InlineData("commerce_31022024.csv", false)] // fecha inexistente
    [InlineData("commerce_2024.csv", false)]
    [InlineData("comercios_16102026.csv", false)]
    [InlineData("commerce_16102026.txt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ReturnsExpected(string? fileName, bool expected)
    {
        Assert.Equal(expected, CommerceFileName.IsValid(fileName));
    }

    [Fact]
    public void TryGetDate_ReturnsDateFromFileName()
    {
        Assert.True(CommerceFileName.TryGetDate(@"C:\cargas\commerce_16102026.csv", out var date));
        Assert.Equal(new DateOnly(2026, 10, 16), date);
    }
}
