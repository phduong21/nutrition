namespace NutritionAgent.Domain;

public sealed class NutritionScoringEngine
{
    public NutritionInsights Score(Product product, CategoryAverages? categoryAverages)
    {
        var nutriments = product.Nutriments;
        var concerns = FlagConcerns(nutriments);
        var positives = FlagPositives(nutriments);
        var summary = CompareToCategory(nutriments, categoryAverages, product.ProductName);

        return new NutritionInsights(
            Summary: summary,
            Concerns: concerns,
            Positives: positives,
            Disclaimer: ScoringThresholds.Disclaimer);
    }

    public IReadOnlyList<AlternativeRecommendation> RankAlternatives(
        Product source,
        IReadOnlyList<Product> candidates)
    {
        return candidates
            .OrderBy(c => GradeRank(c.NutriScoreGrade))
            .ThenByDescending(c => CalculateScore(c.Nutriments))
            .Select(c => new AlternativeRecommendation(
                c.Barcode,
                c.ProductName,
                c.NutriScoreGrade ?? "unknown",
                BuildAlternativeRationale(source, c)))
            .ToList();
    }

    public int CalculateScore(Nutriments nutriments)
    {
        var score = ScoringThresholds.ScoreBaseline;

        score -= Penalty(
            nutriments.Sugars100g,
            ScoringThresholds.SugarBaselineGrams,
            ScoringThresholds.SugarPenaltyPerGram,
            ScoringThresholds.SugarMaxPenalty);

        score -= Penalty(
            nutriments.SaturatedFat100g,
            ScoringThresholds.SaturatedFatBaselineGrams,
            ScoringThresholds.SaturatedFatPenaltyPerGram,
            ScoringThresholds.SaturatedFatMaxPenalty);

        score -= Penalty(
            nutriments.Salt100g,
            ScoringThresholds.SaltBaselineGrams,
            ScoringThresholds.SaltPenaltyPerGram,
            ScoringThresholds.SaltMaxPenalty);

        score -= Penalty(
            nutriments.Fat100g,
            ScoringThresholds.FatBaselineGrams,
            ScoringThresholds.FatPenaltyPerGram,
            ScoringThresholds.FatMaxPenalty);

        score += Bonus(
            nutriments.Proteins100g,
            ScoringThresholds.ProteinBaselineGrams,
            ScoringThresholds.ProteinBonusPerGram,
            ScoringThresholds.ProteinMaxBonus);

        score += Bonus(
            nutriments.Fiber100g,
            ScoringThresholds.FiberBaselineGrams,
            ScoringThresholds.FiberBonusPerGram,
            ScoringThresholds.FiberMaxBonus);

        return (int)Math.Clamp(Math.Round(score), 0, 100);
    }

    public HealthBand ClassifyHealthBand(int score)
    {
        if (score >= ScoringThresholds.HealthyMinScore)
            return HealthBand.Healthy;
        if (score >= ScoringThresholds.ModerateMinScore)
            return HealthBand.Moderate;
        return HealthBand.Poor;
    }

    private static string FlagConcerns(Nutriments nutriments)
    {
        var flags = new List<string>();

        if (nutriments.Sugars100g >= ScoringThresholds.HighSugarGrams)
            flags.Add($"High sugar ({FormatGrams(nutriments.Sugars100g)}/100g).");

        if (nutriments.SaturatedFat100g >= ScoringThresholds.HighSaturatedFatGrams)
            flags.Add($"High saturated fat ({FormatGrams(nutriments.SaturatedFat100g)}/100g).");

        if (nutriments.Salt100g >= ScoringThresholds.HighSaltGrams)
            flags.Add($"High salt ({FormatGrams(nutriments.Salt100g)}/100g).");

        return flags.Count > 0
            ? string.Join(" ", flags)
            : "No significant nutrient concerns identified.";
    }

    private static string FlagPositives(Nutriments nutriments)
    {
        var flags = new List<string>();

        if (nutriments.Proteins100g >= ScoringThresholds.GoodProteinGrams)
            flags.Add($"Good protein content ({FormatGrams(nutriments.Proteins100g)}/100g).");

        if (nutriments.Fiber100g >= ScoringThresholds.GoodFiberGrams)
            flags.Add($"Good fiber content ({FormatGrams(nutriments.Fiber100g)}/100g).");

        return flags.Count > 0
            ? string.Join(" ", flags)
            : "No standout positive nutrient highlights.";
    }

    private static string CompareToCategory(
        Nutriments nutriments,
        CategoryAverages? averages,
        string productName)
    {
        if (averages is null)
            return $"Nutrition profile for {productName} based on per-100g nutriments.";

        var comparisons = new List<string>();

        AddComparison(comparisons, "sugar", nutriments.Sugars100g, averages.Sugars100g);
        AddComparison(comparisons, "saturated fat", nutriments.SaturatedFat100g, averages.SaturatedFat100g);
        AddComparison(comparisons, "protein", nutriments.Proteins100g, averages.Proteins100g);
        AddComparison(comparisons, "fiber", nutriments.Fiber100g, averages.Fiber100g);
        AddComparison(comparisons, "salt", nutriments.Salt100g, averages.Salt100g);

        if (comparisons.Count == 0)
            return $"Nutrition profile for {productName}; category comparison data unavailable.";

        return $"Compared to category average: {string.Join("; ", comparisons)}.";
    }

    private static void AddComparison(
        List<string> comparisons,
        string nutrient,
        decimal? productValue,
        decimal? categoryAverage)
    {
        if (productValue is null || categoryAverage is null || categoryAverage == 0)
            return;

        var direction = productValue > categoryAverage ? "above" : "below";
        comparisons.Add($"{nutrient} is {direction} category average");
    }

    private static string FormatGrams(decimal? value) =>
        value?.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) ?? "0";

    private static string BuildAlternativeRationale(Product source, Product candidate)
    {
        var sourceGrade = source.NutriScoreGrade?.ToUpperInvariant() ?? "?";
        var candidateGrade = candidate.NutriScoreGrade?.ToUpperInvariant() ?? "?";
        var parts = new List<string> { $"Nutri-Score {candidateGrade} vs source {sourceGrade}" };

        if (source.Nutriments.Sugars100g is not null && candidate.Nutriments.Sugars100g is not null &&
            candidate.Nutriments.Sugars100g < source.Nutriments.Sugars100g)
        {
            var reduction = source.Nutriments.Sugars100g.Value - candidate.Nutriments.Sugars100g.Value;
            var percent = source.Nutriments.Sugars100g.Value == 0
                ? 100
                : Math.Round(reduction / source.Nutriments.Sugars100g.Value * 100);
            parts.Add($"{percent}% less sugar per 100g");
        }

        return string.Join("; ", parts) + ".";
    }

    private static int GradeRank(string? grade) =>
        string.IsNullOrWhiteSpace(grade) ? int.MaxValue : grade.Trim().ToLowerInvariant()[0] switch
        {
            'a' => 1,
            'b' => 2,
            'c' => 3,
            'd' => 4,
            'e' => 5,
            _ => int.MaxValue
        };

    private static double Penalty(decimal? value, double baseline, double perGram, double maxPenalty)
    {
        if (value is null || value <= (decimal)baseline)
            return 0;

        var excess = (double)(value.Value - (decimal)baseline);
        return Math.Min(excess * perGram, maxPenalty);
    }

    private static double Bonus(decimal? value, double baseline, double perGram, double maxBonus)
    {
        if (value is null || value <= (decimal)baseline)
            return 0;

        var excess = (double)(value.Value - (decimal)baseline);
        return Math.Min(excess * perGram, maxBonus);
    }
}
