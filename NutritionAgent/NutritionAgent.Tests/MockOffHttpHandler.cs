using System.Net;
using System.Text;
using NutritionAgent.Infrastructure;

namespace NutritionAgent.Tests;

internal sealed class MockOffHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string pathContains, HttpStatusCode status, string jsonBody) =>
        _responses[pathContains] = (status, jsonBody);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        foreach (var (key, response) in _responses)
        {
            if (path.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(response.Status)
                {
                    Content = new StringContent(response.Body, Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
    }
}

internal static class TestFixtures
{
    public static string Load(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }

    public static HttpClient CreateOffClient(Action<MockOffHttpHandler> configure)
    {
        var handler = new MockOffHttpHandler();
        configure(handler);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/")
        };
    }

    public static FoodFetcher CreateFoodFetcher(
        Action<MockOffHttpHandler>? configureProduct = null,
        Action<MockOffHttpHandler>? configureSearch = null)
    {
        var productHandler = new MockOffHttpHandler();
        configureProduct?.Invoke(productHandler);
        var productClient = new HttpClient(productHandler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/")
        };

        var searchHandler = new MockOffHttpHandler();
        configureSearch?.Invoke(searchHandler);
        var searchClient = new HttpClient(searchHandler)
        {
            BaseAddress = new Uri("https://search.openfoodfacts.org/")
        };

        return new FoodFetcher(productClient, searchClient);
    }
}
