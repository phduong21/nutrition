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
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/3017620422003",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-3017620422003.json"))),
            new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nutella", result.Value!.Product.ProductName);
        Assert.Equal(HealthBand.Poor, result.Value.HealthBand);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.NutritionInsights.Disclaimer));
    }

    [Fact]
    public async Task GetProductAnalysisAsync_DoesNotCallOffSearch()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/3017620422003",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-3017620422003.json"))),
            new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(
            "category average",
            result.Value!.NutritionInsights.Summary,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductAnalysisAsync_UnknownBarcode_ReturnsNotFound()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/invalid",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-not-found.json"))),
            new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }
}
