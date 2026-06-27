using System.Net;
using NutritionAgent.Domain;
using NutritionAgent.Infrastructure;
using NutritionAgent.Services;

namespace NutritionAgent.Tests.Unit;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductAnalysisAsync_ValidBarcode_ReturnsScoredProduct()
    {
        var client = TestFixtures.CreateOffClient(handler =>
        {
            handler.Register(
                "/api/v2/product/3017620422003",
                HttpStatusCode.OK,
                TestFixtures.Load("off-3017620422003.json"));
            handler.Register(
                "/api/v2/search",
                HttpStatusCode.OK,
                TestFixtures.Load("off-search-spreads.json"));
        });

        var service = new ProductService(new FoodFetcher(client), new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nutella", result.Value!.Product.ProductName);
        Assert.Equal(HealthBand.Poor, result.Value.HealthBand);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.NutritionInsights.Disclaimer));
    }

    [Fact]
    public async Task GetProductAnalysisAsync_UnknownBarcode_ReturnsNotFound()
    {
        var client = TestFixtures.CreateOffClient(handler =>
            handler.Register(
                "/api/v2/product/invalid",
                HttpStatusCode.OK,
                TestFixtures.Load("off-not-found.json")));

        var service = new ProductService(new FoodFetcher(client), new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}
