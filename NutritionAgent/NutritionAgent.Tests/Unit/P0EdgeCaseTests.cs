using System.Net;
using NutritionAgent.Domain;
using NutritionAgent.Infrastructure;
using NutritionAgent.Services;

namespace NutritionAgent.Tests.Unit;

/// <summary>
/// P0 scenarios from test-design-epic-product-endpoints-edge-cases.md (R-001, R-002, BC-02, NS-01, ALT-01).
/// </summary>
public class P0EdgeCaseTests
{
    private readonly NutritionScoringEngine _engine = new();

    [Fact]
    public void ScoringEngine_AllNullNutriments_ReturnsUnknownHealthBand()
    {
        var nutriments = new Nutriments(null, null, null, null, null, null);
        var score = _engine.CalculateScore(nutriments);
        var band = _engine.ClassifyHealthBand(nutriments, score);

        Assert.Equal(100, score);
        Assert.Equal(HealthBand.Unknown, band);
    }

    [Fact]
    public async Task GetProductAsync_WhitespaceBarcode_ReturnsInvalidInput()
    {
        var fetcher = TestFixtures.CreateFoodFetcher();

        var result = await fetcher.GetProductAsync("   ");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.InvalidInput, result.Error!.Kind);
    }

    [Fact]
    public async Task GetProductAsync_OffProductMissingCode_ReturnsNotFound()
    {
        var fetcher = TestFixtures.CreateFoodFetcher(configureProduct: handler =>
            handler.Register(
                "/api/v2/product/nocode",
                HttpStatusCode.OK,
                TestFixtures.Load("off-missing-code.json")));

        var result = await fetcher.GetProductAsync("nocode");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetProductAnalysisAsync_EmptyNutriments_ReturnsUnknownHealthBand()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/9999999999999",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-empty-nutriments.json"))),
            new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("9999999999999");

        Assert.True(result.IsSuccess);
        Assert.Equal(HealthBand.Unknown, result.Value!.HealthBand);
    }

    [Fact]
    public async Task GetAlternativesAsync_SourceGradeA_Returns200EmptyArray()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/1111111111111",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-grade-a.json"))),
            new NutritionScoringEngine());

        var result = await service.GetAlternativesAsync("1111111111111");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Alternatives);
    }

    [Fact]
    public async Task GetProductAnalysisAsync_MissingNutriScoreGrade_Returns200WithNullGrade()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(configureProduct: handler =>
                handler.Register(
                    "/api/v2/product/9999999999999",
                    HttpStatusCode.OK,
                    TestFixtures.Load("off-empty-nutriments.json"))),
            new NutritionScoringEngine());

        var result = await service.GetProductAnalysisAsync("9999999999999");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Product.NutriScoreGrade);
    }
}
