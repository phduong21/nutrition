using System.Text.Json.Serialization;

namespace NutritionAgent.Infrastructure.OpenFoodFacts;

public sealed class OpenFoodFactsSearchALiciousResponse
{
    [JsonPropertyName("hits")]
    public List<OpenFoodFactsProduct>? Hits { get; init; }
}
