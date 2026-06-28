using System.Net;
using System.Text.Json;
using NutritionAgent.Domain;
using NutritionAgent.Infrastructure.OpenFoodFacts;
using NutritionAgent.Services;

namespace NutritionAgent.Infrastructure;

public sealed class FoodFetcher(HttpClient productClient, HttpClient searchClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<Product>> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<Product>.Failure(new ServiceError(ErrorKind.InvalidInput, "Barcode is required."));

        try
        {
            var response = await productClient.GetAsync($"api/v2/product/{barcode}.json", cancellationToken);

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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Result<Product>.Failure(new ServiceError(
                ErrorKind.UpstreamFailure,
                "Open Food Facts request failed."));
        }
    }

    public async Task<Result<IReadOnlyList<Product>>> SearchAlternativesAsync(
        Product source,
        CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(source.CategoriesTags);
        if (string.IsNullOrWhiteSpace(query))
            return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());

        var betterGrades = GetBetterGrades(source.NutriScoreGrade);
        if (betterGrades.Count == 0)
            return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());

        var url =
            $"search?q={Uri.EscapeDataString(query)}" +
            "&page_size=20&fields=code,product_name,nutriscore_grade,nutriments";

        List<OpenFoodFactsProduct> products = [];
        try
        {
            var response = await GetSearchWithRetryAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());

            var payload = await DeserializeAsync<OpenFoodFactsSearchALiciousResponse>(response, cancellationToken);
            products = payload?.Hits ?? [];
        }
        catch (Exception)
        {
            return Result<IReadOnlyList<Product>>.Success(Array.Empty<Product>());
        }

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

    internal static string? BuildSearchQuery(IReadOnlyList<string> categoriesTags)
    {
        if (categoriesTags.Count == 0)
            return null;

        for (var i = categoriesTags.Count - 1; i >= 0; i--)
        {
            var tag = categoriesTags[i];
            if (!tag.StartsWith("en:", StringComparison.OrdinalIgnoreCase))
                continue;

            var suffix = tag[3..];
            if (IsAsciiSlug(suffix))
                return suffix.Replace('-', ' ');
        }

        var last = categoriesTags[^1];
        return last.StartsWith("en:", StringComparison.OrdinalIgnoreCase)
            ? last[3..].Replace('-', ' ')
            : last;
    }

    private static bool IsAsciiSlug(string value) =>
        value.Length > 0 && value.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-');

    private async Task<HttpResponseMessage> GetSearchWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        var response = await searchClient.GetAsync(url, cancellationToken);
        if ((int)response.StatusCode == 503)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            response = await searchClient.GetAsync(url, cancellationToken);
        }

        return response;
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
