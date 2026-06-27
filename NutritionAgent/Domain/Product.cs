namespace NutritionAgent.Domain;

/// <summary>
/// Domain product mapped from Open Food Facts v2 product JSON.
/// <see cref="Barcode"/> and <see cref="Nutriments"/> are always present after mapping;
/// display and scoring fields may be null or empty when OFF data is incomplete.
/// See docs/OFF-FIELD-MAPPING.md.
/// </summary>
public record Product(
    string Barcode,
    string ProductName,
    string Brands,
    string? NutriScoreGrade,
    IReadOnlyList<string> CategoriesTags,
    string? IngredientsText,
    Nutriments Nutriments);
