namespace NutritionAgent.Domain;

public record NutritionInsights(
    string Summary,
    string Concerns,
    string Positives,
    string Disclaimer);
