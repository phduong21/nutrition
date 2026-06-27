using System.Net;
using NutritionAgent.Infrastructure;
using NutritionAgent.Services;

namespace NutritionAgent.Tests.Unit;

public class FoodFetcherTests
{
    [Fact]
    public async Task GetProductAsync_ValidBarcode_ReturnsProduct()
    {
        var client = TestFixtures.CreateOffClient(handler =>
            handler.Register(
                "/api/v2/product/3017620422003",
                HttpStatusCode.OK,
                TestFixtures.Load("off-3017620422003.json")));

        var fetcher = new FoodFetcher(client);

        var result = await fetcher.GetProductAsync("3017620422003");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nutella", result.Value!.ProductName);
    }

    [Fact]
    public async Task GetProductAsync_UnknownBarcode_ReturnsNotFound()
    {
        var client = TestFixtures.CreateOffClient(handler =>
            handler.Register(
                "/api/v2/product/invalid",
                HttpStatusCode.OK,
                TestFixtures.Load("off-not-found.json")));

        var fetcher = new FoodFetcher(client);

        var result = await fetcher.GetProductAsync("invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetProductAsync_OffServerError_ReturnsUpstreamFailure()
    {
        var client = TestFixtures.CreateOffClient(handler =>
            handler.Register(
                "/api/v2/product/500",
                HttpStatusCode.InternalServerError,
                "{}"));

        var fetcher = new FoodFetcher(client);

        var result = await fetcher.GetProductAsync("500");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.UpstreamFailure, result.Error!.Kind);
    }

    [Fact]
    public async Task GetCategoryAveragesAsync_WithProducts_ReturnsAverages()
    {
        var client = TestFixtures.CreateOffClient(handler =>
            handler.Register(
                "/api/v2/search",
                HttpStatusCode.OK,
                TestFixtures.Load("off-search-spreads.json")));

        var fetcher = new FoodFetcher(client);

        var result = await fetcher.GetCategoryAveragesAsync("en:spreads");

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value!.Sugars100g);
    }
}
