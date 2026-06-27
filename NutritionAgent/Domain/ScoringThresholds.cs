namespace NutritionAgent.Domain;

/// <summary>
/// Deterministic thresholds for scoring, health bands, and insight flags (per 100g).
/// </summary>
public static class ScoringThresholds
{
    // Health band cutoffs (score 0–100)
    public const int HealthyMinScore = 70;
    public const int ModerateMinScore = 40;

    // Score calculation — baseline and per-nutrient weights
    public const double ScoreBaseline = 100;
    public const double SugarBaselineGrams = 5;
    public const double SugarPenaltyPerGram = 1.5;
    public const double SugarMaxPenalty = 40;

    public const double SaturatedFatBaselineGrams = 1.5;
    public const double SaturatedFatPenaltyPerGram = 3;
    public const double SaturatedFatMaxPenalty = 25;

    public const double SaltBaselineGrams = 0.3;
    public const double SaltPenaltyPerGram = 10;
    public const double SaltMaxPenalty = 20;

    public const double FatBaselineGrams = 3;
    public const double FatPenaltyPerGram = 1;
    public const double FatMaxPenalty = 15;

    public const double ProteinBaselineGrams = 5;
    public const double ProteinBonusPerGram = 2;
    public const double ProteinMaxBonus = 15;

    public const double FiberBaselineGrams = 3;
    public const double FiberBonusPerGram = 2;
    public const double FiberMaxBonus = 20;

    // Insight flag thresholds (per 100g)
    public const decimal HighSugarGrams = 22.5m;
    public const decimal HighSaturatedFatGrams = 5m;
    public const decimal HighSaltGrams = 1.5m;
    public const decimal GoodProteinGrams = 8m;
    public const decimal GoodFiberGrams = 6m;

    public const string Disclaimer =
        "This information is for educational purposes only and is not medical advice.";
}
