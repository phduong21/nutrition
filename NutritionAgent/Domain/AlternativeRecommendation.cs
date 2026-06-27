namespace NutritionAgent.Domain;

public record AlternativeRecommendation(
    string Barcode,
    string ProductName,
    string NutriScoreGrade,
    string Rationale);
