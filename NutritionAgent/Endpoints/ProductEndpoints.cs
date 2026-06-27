using NutritionAgent.Services;

namespace NutritionAgent.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products/{barcode}", GetProductAsync)
            .WithName("GetProduct")
            .WithTags("Products")
            .WithSummary("Get product nutrition analysis")
            .WithDescription(
                "Fetches product data from Open Food Facts by barcode, computes a 0–100 nutrition score, " +
                "classifies a health band, and returns rule-generated nutrition insights.")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        app.MapGet("/products/{barcode}/alternatives", GetAlternativesAsync)
            .WithName("GetAlternatives")
            .WithTags("Products")
            .WithSummary("Get healthier alternatives")
            .WithDescription(
                "Finds same-category products with a strictly better Nutri-Score than the source product. " +
                "Each alternative includes a rule-generated rationale.")
            .Produces<AlternativesApiResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return app;
    }

    private static async Task<IResult> GetProductAsync(
        string barcode,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetProductAnalysisAsync(barcode, cancellationToken);
        if (result.IsSuccess)
        {
            var analysis = result.Value!;
            var product = analysis.Product;

            return Results.Ok(new ProductResponse(
                product.ProductName,
                product.Brands,
                product.Nutriments,
                product.NutriScoreGrade,
                product.IngredientsText,
                analysis.NutritionScore,
                analysis.HealthBand.ToString(),
                analysis.NutritionInsights));
        }

        return MapError(result.Error!);
    }

    private static async Task<IResult> GetAlternativesAsync(
        string barcode,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var result = await productService.GetAlternativesAsync(barcode, cancellationToken);
        if (result.IsSuccess)
        {
            var response = result.Value!;
            return Results.Ok(new AlternativesApiResponse(response.SourceBarcode, response.Alternatives));
        }

        return MapError(result.Error!);
    }

    private static IResult MapError(ServiceError error) =>
        error.Kind switch
        {
            ErrorKind.NotFound => Results.Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status404NotFound,
                title: "Product not found"),
            ErrorKind.UpstreamFailure => Results.Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Upstream service error"),
            _ => Results.Problem(
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request")
        };
}
