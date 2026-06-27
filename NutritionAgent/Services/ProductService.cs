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

        return Result<ProductAnalysis>.Success(BuildAnalysis(productResult.Value!));
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

        // Alternatives are best-effort: degrade gracefully when OFF search is unavailable.
        IReadOnlyList<AlternativeRecommendation> ranked = searchResult.IsSuccess
            ? scoringEngine.RankAlternatives(source, searchResult.Value!)
            : [];

        return Result<AlternativesResponse>.Success(new AlternativesResponse(source.Barcode, ranked));
    }

    private ProductAnalysis BuildAnalysis(Product product)
    {
        var score = scoringEngine.CalculateScore(product.Nutriments);
        var band = scoringEngine.ClassifyHealthBand(score);
        var insights = scoringEngine.Score(product, categoryAverages: null);

        return new ProductAnalysis(product, score, band, insights);
    }
}
