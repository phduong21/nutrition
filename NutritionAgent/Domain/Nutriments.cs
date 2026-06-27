namespace NutritionAgent.Domain;

/// <summary>
/// Per-100g nutrient values mapped from Open Food Facts <c>product.nutriments</c>.
/// Individual fields are nullable when OFF omits them (common for <see cref="Fiber100g"/>).
/// See docs/OFF-FIELD-MAPPING.md.
/// </summary>
public record Nutriments(
    decimal? Sugars100g,
    decimal? Fat100g,
    decimal? SaturatedFat100g,
    decimal? Proteins100g,
    decimal? Fiber100g,
    decimal? Salt100g);
