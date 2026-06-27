using NutritionAgent.Domain;

namespace NutritionAgent.Infrastructure.OpenFoodFacts;

public static class OpenFoodFactsProductMapper
{
    public static Product Map(OpenFoodFactsProductResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(response.Product);

        var offProduct = response.Product;
        var barcode = offProduct.Code ?? response.Code
            ?? throw new InvalidOperationException("OFF response is missing product barcode.");

        var nutriments = offProduct.Nutriments ?? new OpenFoodFactsNutriments();

        return new Product(
            Barcode: barcode,
            ProductName: offProduct.ProductName ?? string.Empty,
            Brands: offProduct.Brands ?? string.Empty,
            NutriScoreGrade: offProduct.NutriScoreGrade,
            CategoriesTags: offProduct.CategoriesTags ?? [],
            IngredientsText: offProduct.IngredientsText,
            Nutriments: new Nutriments(
                Sugars100g: nutriments.Sugars100g,
                Fat100g: nutriments.Fat100g,
                SaturatedFat100g: nutriments.SaturatedFat100g,
                Proteins100g: nutriments.Proteins100g,
                Fiber100g: nutriments.Fiber100g,
                Salt100g: nutriments.Salt100g));
    }
}
