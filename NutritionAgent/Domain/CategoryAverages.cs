namespace NutritionAgent.Domain;

public record CategoryAverages(
    decimal? Sugars100g,
    decimal? Fat100g,
    decimal? SaturatedFat100g,
    decimal? Proteins100g,
    decimal? Fiber100g,
    decimal? Salt100g);
