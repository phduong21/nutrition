using NutritionAgent.Domain;
using NutritionAgent.Infrastructure;

namespace NutritionAgent.Services;

public sealed class ProductService(FoodFetcher foodFetcher, NutritionScoringEngine scoringEngine)
{
    public async Task<Result<ProductAnalysis>> GetProductAnalysisAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var productResult = await foodFetcher.GetProductAsync(barcode, cancellationToken);
        if (!productResult.IsSuccess)
            return Result<ProductAnalysis>.Failure(productResult.Error!);

        var product = productResult.Value!;
        var categoryTag = product.CategoriesTags.FirstOrDefault();

        CategoryAverages? averages = null;
        if (!string.IsNullOrWhiteSpace(categoryTag))
        {
            var averagesResult = await foodFetcher.GetCategoryAveragesAsync(categoryTag, cancellationToken);
            if (averagesResult.IsSuccess)
                averages = averagesResult.Value;
        }

        return Result<ProductAnalysis>.Success(BuildAnalysis(product, averages));
    }

    public async Task<Result<AlternativesResponse>> GetAlternativesAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var productResult = await foodFetcher.GetProductAsync(barcode, cancellationToken);
        if (!productResult.IsSuccess)
            return Result<AlternativesResponse>.Failure(productResult.Error!);

        var source = productResult.Value!;
        var searchResult = await foodFetcher.SearchAlternativesAsync(source, cancellationToken);
        if (!searchResult.IsSuccess)
            return Result<AlternativesResponse>.Failure(searchResult.Error!);

        var ranked = scoringEngine.RankAlternatives(source, searchResult.Value!);

        return Result<AlternativesResponse>.Success(new AlternativesResponse(source.Barcode, ranked));
    }

    private ProductAnalysis BuildAnalysis(Product product, CategoryAverages? averages)
    {
        var score = scoringEngine.CalculateScore(product.Nutriments);
        var band = scoringEngine.ClassifyHealthBand(score);
        var insights = scoringEngine.Score(product, averages);

        return new ProductAnalysis(product, score, band, insights);
    }
}
