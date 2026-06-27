using System.Text.Json.Serialization;

namespace NutritionAgent.Infrastructure.OpenFoodFacts;

/// <summary>
/// Subset of Open Food Facts v2 GET /api/v2/product/{barcode}.json response.
/// </summary>
public sealed class OpenFoodFactsProductResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; init; }
}

public sealed class OpenFoodFactsProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; init; }

    [JsonPropertyName("brands")]
    public string? Brands { get; init; }

    [JsonPropertyName("nutriscore_grade")]
    public string? NutriScoreGrade { get; init; }

    [JsonPropertyName("categories_tags")]
    public List<string>? CategoriesTags { get; init; }

    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; init; }

    [JsonPropertyName("nutriments")]
    public OpenFoodFactsNutriments? Nutriments { get; init; }
}

public sealed class OpenFoodFactsNutriments
{
    [JsonPropertyName("sugars_100g")]
    public decimal? Sugars100g { get; init; }

    [JsonPropertyName("fat_100g")]
    public decimal? Fat100g { get; init; }

    [JsonPropertyName("saturated-fat_100g")]
    public decimal? SaturatedFat100g { get; init; }

    [JsonPropertyName("proteins_100g")]
    public decimal? Proteins100g { get; init; }

    [JsonPropertyName("fiber_100g")]
    public decimal? Fiber100g { get; init; }

    [JsonPropertyName("salt_100g")]
    public decimal? Salt100g { get; init; }
}
