using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NutritionAgent.Tests.Integration;

public class ProductEndpointsTests : IClassFixture<NutritionWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductEndpointsTests(NutritionWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetProduct_ValidBarcode_Returns200()
    {
        var response = await _client.GetAsync("/products/3017620422003");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nutella", json.GetProperty("productName").GetString());
        Assert.True(json.TryGetProperty("nutritionScore", out _));
        Assert.True(json.TryGetProperty("healthBand", out _));
        Assert.Equal("e", json.GetProperty("nutriscoreGrade").GetString());
    }

    [Fact]
    public async Task GetProduct_InvalidBarcode_Returns404()
    {
        var response = await _client.GetAsync("/products/invalid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetProduct_ResponseContainsNutritionInsights()
    {
        var response = await _client.GetAsync("/products/3017620422003");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var insights = json.GetProperty("nutritionInsights");
        Assert.False(string.IsNullOrWhiteSpace(insights.GetProperty("summary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(insights.GetProperty("concerns").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(insights.GetProperty("positives").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(insights.GetProperty("disclaimer").GetString()));
    }

    [Fact]
    public async Task GetProduct_DoesNotIncludeAlternativesArray()
    {
        var response = await _client.GetAsync("/products/3017620422003");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(json.TryGetProperty("alternatives", out _));
    }

    [Fact]
    public async Task GetAlternatives_ValidBarcode_Returns200WithAlternatives()
    {
        var response = await _client.GetAsync("/products/3017620422003/alternatives");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("3017620422003", json.GetProperty("sourceBarcode").GetString());
        Assert.True(json.GetProperty("alternatives").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetAlternatives_InvalidBarcode_Returns404()
    {
        var response = await _client.GetAsync("/products/invalid/alternatives");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_WhitespaceBarcode_Returns400()
    {
        var response = await _client.GetAsync("/products/%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetProduct_OffProductMissingCode_Returns404()
    {
        var response = await _client.GetAsync("/products/nocode");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_EmptyNutriments_ReturnsUnknownHealthBand()
    {
        var response = await _client.GetAsync("/products/9999999999999");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Unknown", json.GetProperty("healthBand").GetString());
    }

    [Fact]
    public async Task GetProduct_ValidEanBarcode_Returns200()
    {
        var response = await _client.GetAsync("/products/5000159407236");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mars", json.GetProperty("productName").GetString());
    }

    [Fact]
    public async Task GetAlternatives_SourceGradeA_Returns200EmptyArray()
    {
        var response = await _client.GetAsync("/products/1111111111111/alternatives");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.GetProperty("alternatives").GetArrayLength());
    }
}
