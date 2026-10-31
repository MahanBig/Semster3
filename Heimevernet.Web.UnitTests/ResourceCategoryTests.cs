using Heimevernet.Web.Models;

namespace Heimevernet.Web.UnitTests;

public class ResourceCategoryTests
{
    [Theory]
    [InlineData("Mat", "mat")]
    [InlineData("Drikkevann", "vann")]
    [InlineData("Ly og varme", "ly")]
    [InlineData("Førstehjelpspost", "helse")]
    [InlineData("Utstyr", "verktoy")]
    [InlineData("Kjøretøy", "verktoy")]
    [InlineData("Strøm og lading", "strom")]
    [InlineData("  STRØMAGGREGAT ", "strom")]
    [InlineData("Type A", "info")]   // Unknown type falls back to Informasjon.
    [InlineData("", "info")]
    [InlineData(null, "info")]
    public void FromType_PicksCategoryFromKeywords(string? type, string expectedKey)
    {
        Assert.Equal(expectedKey, ResourceCategory.FromType(type).Key);
    }

    [Fact]
    public void IconSvg_IsDecorativeAndUsesTheCategoryPath()
    {
        var svg = ResourceCategory.Water.IconSvg(24);

        Assert.Contains("aria-hidden=\"true\"", svg);
        Assert.Contains("width=\"24\"", svg);
        Assert.Contains(ResourceCategory.Water.IconPath, svg);
    }
}
