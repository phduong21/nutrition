using System.Net;
using System.Text.Json;
using NutritionAgent.Domain;
using NutritionAgent.Infrastructure.OpenFoodFacts;
using NutritionAgent.Services;

namespace NutritionAgent.Infrastructure;

public sealed class FoodFetcher(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<Product>> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<Product>.Failure(new ServiceError(ErrorKind.InvalidInput, "Barcode is required."));

        var response = await httpClient.GetAsync($"api/v2/product/{barcode}.json", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return Result<Product>.Failure(new ServiceError(ErrorKind.NotFound, $"Product '{barcode}' was not found."));

        if (!response.IsSuccessStatusCode)
            return Result<Product>.Failure(new ServiceError(
                ErrorKind.UpstreamFailure,
                $"Open Food Facts returned {(int)response.StatusCode}."));

        var payload = await DeserializeAsync<OpenFoodFactsProductResponse>(response, cancellationToken);

        if (payload is null || payload.Status != 1 || payload.Product is null)
            return Result<Product>.Failure(new ServiceError(ErrorKind.NotFound, $"Product '{barcode}' was not found."));

        return Result<Product>.Success(OpenFoodFactsProductMapper.Map(payload));
    }

    public async Task<Result<CategoryAverages>> GetCategoryAveragesAsync(
        string categoryTag,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryTag))
            return Result<CategoryAverages>.Failure(new ServiceError(ErrorKind.InvalidInput, "Category tag is required."));

        var url =
            $"api/v2/search?categories_tags={Uri.EscapeDataString(categoryTag)}" +
            "&page_size=20&fields=nutriments";

        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<CategoryAverages>.Failure(new ServiceError(
                ErrorKind.UpstreamFailure,
                $"Open Food Facts search returned {(int)response.StatusCode}."));

        var payload = await DeserializeAsync<OpenFoodFactsSearchResponse>(response, cancellationToken);
        var products = payload?.Products ?? [];

        if (products.Count == 0)
            return Result<CategoryAverages>.Failure(new ServiceError(
                ErrorKind.NotFound,
                $"No products found for category '{categoryTag}'."));

        return Result<CategoryAverages>.Success(CalculateAverages(products));
    }

    public async Task<Result<IReadOnlyList<Product>>> SearchAlternativesAsync(
        Product source,
        CancellationToken cancellationToken = default)
    {
        var category = source.CategoriesTags.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(category))
            return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());

        var betterGrades = GetBetterGrades(source.NutriScoreGrade);
        if (betterGrades.Count == 0)
            return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());

        var gradesQuery = string.Join(",", betterGrades);
        var url =
            $"api/v2/search?categories_tags={Uri.EscapeDataString(category)}" +
            $"&nutriscore_grade={gradesQuery}" +
            $"&page_size=10&fields=code,product_name,brands,nutriscore_grade,categories_tags,ingredients_text,nutriments";

        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<Product>>.Failure(new ServiceError(
                ErrorKind.UpstreamFailure,
                $"Open Food Facts search returned {(int)response.StatusCode}."));

        var payload = await DeserializeAsync<OpenFoodFactsSearchResponse>(response, cancellationToken);
        var products = payload?.Products ?? [];

        var alternatives = products
            .Where(p => p.Code is not null && p.Code != source.Barcode)
            .Select(p => OpenFoodFactsProductMapper.Map(new OpenFoodFactsProductResponse
            {
                Code = p.Code,
                Status = 1,
                Product = p
            }))
            .Where(p => IsStrictlyBetterGrade(source.NutriScoreGrade, p.NutriScoreGrade))
            .ToList();

        return Result<IReadOnlyList<Product>>.Success(alternatives);
    }

    private static CategoryAverages CalculateAverages(IEnumerable<OpenFoodFactsProduct> products)
    {
        var nutriments = products
            .Select(p => p.Nutriments)
            .Where(n => n is not null)
            .ToList();

        return new CategoryAverages(
            Average(nutriments.Select(n => n!.Sugars100g)),
            Average(nutriments.Select(n => n!.Fat100g)),
            Average(nutriments.Select(n => n!.SaturatedFat100g)),
            Average(nutriments.Select(n => n!.Proteins100g)),
            Average(nutriments.Select(n => n!.Fiber100g)),
            Average(nutriments.Select(n => n!.Salt100g)));
    }

    private static decimal? Average(IEnumerable<decimal?> values)
    {
        var numbers = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return numbers.Count == 0 ? null : numbers.Average();
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    internal static IReadOnlyList<string> GetBetterGrades(string? sourceGrade)
    {
        var rank = GradeRank(sourceGrade);
        if (rank is null or <= 1)
            return Array.Empty<string>();

        return new[] { "a", "b", "c", "d", "e" }
            .Where(g => GradeRank(g) < rank)
            .ToList();
    }

    internal static bool IsStrictlyBetterGrade(string? sourceGrade, string? candidateGrade)
    {
        var sourceRank = GradeRank(sourceGrade);
        var candidateRank = GradeRank(candidateGrade);
        return sourceRank is not null && candidateRank is not null && candidateRank < sourceRank;
    }

    private static int? GradeRank(string? grade) =>
        string.IsNullOrWhiteSpace(grade) ? null : grade.Trim().ToLowerInvariant()[0] switch
        {
            'a' => 1,
            'b' => 2,
            'c' => 3,
            'd' => 4,
            'e' => 5,
            _ => null
        };
}
