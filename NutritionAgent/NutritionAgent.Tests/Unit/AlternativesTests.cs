using NutritionAgent.Domain;
using NutritionAgent.Services;

namespace NutritionAgent.Tests.Unit;

public class AlternativesTests
{
    private readonly NutritionScoringEngine _engine = new();

    [Fact]
    public void GetAlternatives_PoorProduct_ReturnsBetterNutriScore()
    {
        var source = new Product(
            "3017620422003",
            "Nutella",
            "Ferrero",
            "e",
            ["en:spreads"],
            null,
            new Nutriments(56.3m, 30.9m, 10.6m, 6.3m, null, 0.107m));

        var candidates = new List<Product>
        {
            new(
                "3333333333333",
                "Better Spread",
                "Brand C",
                "b",
                ["en:spreads"],
                null,
                new Nutriments(30m, 15m, 4m, 5m, 3m, 0.3m)),
            new(
                "4444444444444",
                "Best Spread",
                "Brand D",
                "a",
                ["en:spreads"],
                null,
                new Nutriments(10m, 8m, 2m, 7m, 5m, 0.2m))
        };

        var ranked = _engine.RankAlternatives(source, candidates);

        Assert.NotEmpty(ranked);
        Assert.All(ranked, alt => Assert.True(
            GradeValue(alt.NutriScoreGrade) < GradeValue(source.NutriScoreGrade!)));
        Assert.False(string.IsNullOrWhiteSpace(ranked[0].Rationale));
    }

    [Fact]
    public async Task ProductService_GetAlternativesAsync_ReturnsBetterAlternatives()
    {
        var service = new ProductService(
            TestFixtures.CreateFoodFetcher(
                configureProduct: handler =>
                {
                    handler.Register(
                        "/api/v2/product/3017620422003",
                        System.Net.HttpStatusCode.OK,
                        TestFixtures.Load("off-3017620422003.json"));
                },
                configureSearch: handler =>
                    handler.Register(
                        "/search",
                        System.Net.HttpStatusCode.OK,
                        TestFixtures.Load("off-search-alternatives.json"))),
            new NutritionScoringEngine());

        var result = await service.GetAlternativesAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Alternatives);
        Assert.All(result.Value.Alternatives, alt =>
            Assert.True(GradeValue(alt.NutriScoreGrade) < GradeValue("e")));
    }

    private static int GradeValue(string grade) =>
        grade.Trim().ToLowerInvariant()[0] switch
        {
            'a' => 1,
            'b' => 2,
            'c' => 3,
            'd' => 4,
            'e' => 5,
            _ => 99
        };
}
