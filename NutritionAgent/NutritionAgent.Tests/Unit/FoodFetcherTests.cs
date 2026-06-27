using System.Net;
using NutritionAgent.Infrastructure;
using NutritionAgent.Services;

namespace NutritionAgent.Tests.Unit;

public class FoodFetcherTests
{
    [Fact]
    public async Task GetProductAsync_ValidBarcode_ReturnsProduct()
    {
        var fetcher = TestFixtures.CreateFoodFetcher(configureProduct: handler =>
            handler.Register(
                "/api/v2/product/3017620422003",
                HttpStatusCode.OK,
                TestFixtures.Load("off-3017620422003.json")));

        var result = await fetcher.GetProductAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nutella", result.Value!.ProductName);
    }

    [Fact]
    public async Task GetProductAsync_UnknownBarcode_ReturnsNotFound()
    {
        var fetcher = TestFixtures.CreateFoodFetcher(configureProduct: handler =>
            handler.Register(
                "/api/v2/product/invalid",
                HttpStatusCode.OK,
                TestFixtures.Load("off-not-found.json")));

        var result = await fetcher.GetProductAsync("invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetProductAsync_OffServerError_ReturnsUpstreamFailure()
    {
        var fetcher = TestFixtures.CreateFoodFetcher(configureProduct: handler =>
            handler.Register(
                "/api/v2/product/500",
                HttpStatusCode.InternalServerError,
                "{}"));

        var result = await fetcher.GetProductAsync("500");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.UpstreamFailure, result.Error!.Kind);
    }

    [Fact]
    public void BuildSearchQuery_PrefersEnglishSlugTag()
    {
        var tags = new[]
        {
            "en:breakfasts",
            "en:spreads",
            "en:sweet-spreads",
            "en:confectionary-based-spreads",
            "en:Pâtes à tartiner"
        };

        var query = FoodFetcher.BuildSearchQuery(tags);

        Assert.Equal("confectionary based spreads", query);
    }

    [Fact]
    public void BuildSearchQuery_FallsBackToLastTag()
    {
        var tags = new[] { "fr:Produits à tartiner" };

        var query = FoodFetcher.BuildSearchQuery(tags);

        Assert.Equal("fr:Produits à tartiner", query);
    }

    [Fact]
    public void BuildSearchQuery_EmptyCategories_ReturnsNull()
    {
        var query = FoodFetcher.BuildSearchQuery([]);

        Assert.Null(query);
    }

    [Fact]
    public async Task SearchAlternativesAsync_LiveSearchShape_ReturnsBetterGrades()
    {
        var source = new Domain.Product(
            "3017620422003",
            "Nutella",
            "Ferrero",
            "e",
            ["en:confectionary-based-spreads"],
            null,
            new Domain.Nutriments(56.3m, 30.9m, 10.6m, 6.3m, null, 0.107m));

        var fetcher = TestFixtures.CreateFoodFetcher(configureSearch: handler =>
            handler.Register(
                "/search",
                HttpStatusCode.OK,
                TestFixtures.Load("off-search-live.json")));

        var result = await fetcher.SearchAlternativesAsync(source);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
        Assert.All(result.Value!, p =>
            Assert.True(FoodFetcher.IsStrictlyBetterGrade("e", p.NutriScoreGrade)));
    }
}
