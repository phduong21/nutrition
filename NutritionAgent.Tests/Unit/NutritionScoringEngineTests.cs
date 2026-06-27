using NutritionAgent.Domain;

namespace NutritionAgent.Tests.Unit;

public class NutritionScoringEngineTests
{
    private readonly NutritionScoringEngine _engine = new();

    [Fact]
    public void ScoringEngine_LowSugarHighFiber_ReturnsHigherScoreThanHighSugarLowFiber()
    {
        var healthy = new Nutriments(5m, 2m, 1m, 12m, 8m, 0.1m);
        var unhealthy = new Nutriments(55m, 15m, 8m, 4m, 0m, 0.5m);

        var healthyScore = _engine.CalculateScore(healthy);
        var unhealthyScore = _engine.CalculateScore(unhealthy);

        Assert.True(healthyScore > unhealthyScore);
    }

    [Fact]
    public void ScoringEngine_KnownFixture_ReturnsExpectedHealthBand()
    {
        var nutellaNutriments = new Nutriments(56.3m, 30.9m, 10.6m, 6.3m, null, 0.107m);

        var score = _engine.CalculateScore(nutellaNutriments);
        var band = _engine.ClassifyHealthBand(score);

        Assert.InRange(score, 0, 100);
        Assert.Equal(HealthBand.Poor, band);
    }

    [Fact]
    public void ScoringEngine_HighSugar_FlagsConcernInInsights()
    {
        var product = new Product(
            "123",
            "Sweet Snack",
            "Brand",
            "d",
            [],
            null,
            new Nutriments(30m, 10m, 6m, 4m, 1m, 0.5m));

        var insights = _engine.Score(product, categoryAverages: null);

        Assert.Contains("sugar", insights.Concerns, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScoringEngine_GoodProteinOrFiber_FlagsPositiveInInsights()
    {
        var product = new Product(
            "456",
            "Protein Bar",
            "Brand",
            "b",
            [],
            null,
            new Nutriments(8m, 5m, 2m, 12m, 7m, 0.3m));

        var insights = _engine.Score(product, categoryAverages: null);

        Assert.True(
            insights.Positives.Contains("protein", StringComparison.OrdinalIgnoreCase) ||
            insights.Positives.Contains("fiber", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NutritionInsights_AlwaysContainsDisclaimerField()
    {
        var product = new Product(
            "789",
            "Plain",
            "Brand",
            null,
            [],
            null,
            new Nutriments(null, null, null, null, null, null));

        var insights = _engine.Score(product, categoryAverages: null);

        Assert.False(string.IsNullOrWhiteSpace(insights.Disclaimer));
        Assert.Equal(ScoringThresholds.Disclaimer, insights.Disclaimer);
    }

    [Fact]
    public void ScoringEngine_WithCategoryAverages_IncludesComparisonInSummary()
    {
        var product = new Product(
            "3017620422003",
            "Nutella",
            "Ferrero",
            "e",
            ["en:spreads"],
            null,
            new Nutriments(56.3m, 30.9m, 10.6m, 6.3m, null, 0.107m));

        var averages = new CategoryAverages(
            Sugars100g: 25m,
            Fat100g: 15m,
            SaturatedFat100g: 5m,
            Proteins100g: 5m,
            Fiber100g: 3m,
            Salt100g: 0.5m);

        var insights = _engine.Score(product, averages);

        Assert.Contains("category", insights.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScoringEngine_WithoutCategoryAverages_StillReturnsInsights()
    {
        var product = new Product(
            "3017620422003",
            "Nutella",
            "Ferrero",
            "e",
            [],
            null,
            new Nutriments(56.3m, 30.9m, 10.6m, 6.3m, null, 0.107m));

        var insights = _engine.Score(product, categoryAverages: null);

        Assert.False(string.IsNullOrWhiteSpace(insights.Summary));
        Assert.False(string.IsNullOrWhiteSpace(insights.Concerns));
        Assert.False(string.IsNullOrWhiteSpace(insights.Positives));
        Assert.False(string.IsNullOrWhiteSpace(insights.Disclaimer));
    }
}
