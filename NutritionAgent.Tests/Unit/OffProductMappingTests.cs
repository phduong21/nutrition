using System.Text.Json;
using NutritionAgent.Infrastructure.OpenFoodFacts;

namespace NutritionAgent.Tests.Unit;

public class OffProductMappingTests
{
    [Fact]
    public void MapFromOffResponse_NutellaFixture_MapsAllNutrimentsIncludingNullFiber()
    {
        var response = LoadFixture("off-3017620422003.json");

        var product = OpenFoodFactsProductMapper.Map(response);

        Assert.Equal("3017620422003", product.Barcode);
        Assert.Equal("Nutella", product.ProductName);
        Assert.Equal("Ferrero, Nutella, Yum yum", product.Brands);
        Assert.Equal("e", product.NutriScoreGrade);
        Assert.Contains("en:breakfasts", product.CategoriesTags);
        Assert.NotNull(product.IngredientsText);
        Assert.Contains("NOISETTES", product.IngredientsText);

        Assert.Equal(56.3m, product.Nutriments.Sugars100g);
        Assert.Equal(30.9m, product.Nutriments.Fat100g);
        Assert.Equal(10.6m, product.Nutriments.SaturatedFat100g);
        Assert.Equal(6.3m, product.Nutriments.Proteins100g);
        Assert.Null(product.Nutriments.Fiber100g);
        Assert.Equal(0.107m, product.Nutriments.Salt100g);
    }

    [Fact]
    public void MapFromOffResponse_MarsFixture_MapsAllFieldsCorrectly()
    {
        var response = LoadFixture("off-5000159407236.json");

        var product = OpenFoodFactsProductMapper.Map(response);

        Assert.Equal("5000159407236", product.Barcode);
        Assert.Equal("Mars", product.ProductName);
        Assert.Equal("Mars, Mars Wrigley", product.Brands);
        Assert.Equal("e", product.NutriScoreGrade);
        Assert.Contains("en:snacks", product.CategoriesTags);

        Assert.Equal(61.6m, product.Nutriments.Sugars100g);
        Assert.Equal(16.8m, product.Nutriments.Fat100g);
        Assert.Equal(8.38m, product.Nutriments.SaturatedFat100g);
        Assert.Equal(4.04m, product.Nutriments.Proteins100g);
        Assert.Equal(1.24m, product.Nutriments.Fiber100g);
        Assert.Equal(0.42m, product.Nutriments.Salt100g);
    }

    [Fact]
    public void ProjectStructure_HasRequiredLayeredFolders()
    {
        var projectRoot = FindProjectRoot();

        Assert.True(Directory.Exists(Path.Combine(projectRoot, "NutritionAgent", "Domain")));
        Assert.True(Directory.Exists(Path.Combine(projectRoot, "NutritionAgent", "Infrastructure")));
        Assert.True(Directory.Exists(Path.Combine(projectRoot, "NutritionAgent", "Services")));
        Assert.True(Directory.Exists(Path.Combine(projectRoot, "NutritionAgent", "Endpoints")));
    }

    private static OpenFoodFactsProductResponse LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<OpenFoodFactsProductResponse>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize fixture {fileName}");
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nutrition.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }
}
