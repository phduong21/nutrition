namespace NutritionAgent.Services;

public sealed record ProductAnalysis(
    Domain.Product Product,
    int NutritionScore,
    Domain.HealthBand HealthBand,
    Domain.NutritionInsights NutritionInsights);

public sealed record AlternativesResponse(
    string SourceBarcode,
    IReadOnlyList<Domain.AlternativeRecommendation> Alternatives);
