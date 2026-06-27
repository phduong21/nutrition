namespace NutritionAgent.Infrastructure;

public sealed class OpenFoodFactsOptions
{
    public const string SectionName = "OpenFoodFacts";

    public string BaseUrl { get; init; } = "https://world.openfoodfacts.org";

    public string? UserAgent { get; init; }
}
