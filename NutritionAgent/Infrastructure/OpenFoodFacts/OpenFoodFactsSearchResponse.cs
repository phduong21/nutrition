using System.Text.Json.Serialization;

namespace NutritionAgent.Infrastructure.OpenFoodFacts;

public sealed class OpenFoodFactsSearchResponse
{
    [JsonPropertyName("products")]
    public List<OpenFoodFactsProduct>? Products { get; init; }
}
