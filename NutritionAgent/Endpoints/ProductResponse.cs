using NutritionAgent.Domain;
using NutritionAgent.Services;

namespace NutritionAgent.Endpoints;

public sealed record ProductResponse(
    string ProductName,
    string Brands,
    Nutriments Nutriments,
    string? NutriscoreGrade,
    string? IngredientsText,
    int NutritionScore,
    string HealthBand,
    NutritionInsights NutritionInsights);

public sealed record AlternativesApiResponse(
    string SourceBarcode,
    IReadOnlyList<AlternativeRecommendation> Alternatives);
