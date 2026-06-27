using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NutritionAgent.Infrastructure;

namespace NutritionAgent.Tests.Integration;

public sealed class NutritionWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var fetcherDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(FoodFetcher));
            if (fetcherDescriptor is not null)
                services.Remove(fetcherDescriptor);

            var handler = new MockOffHttpHandler();
            handler.Register(
                "/api/v2/product/3017620422003",
                System.Net.HttpStatusCode.OK,
                TestFixtures.Load("off-3017620422003.json"));
            handler.Register(
                "/api/v2/product/invalid",
                System.Net.HttpStatusCode.OK,
                TestFixtures.Load("off-not-found.json"));
            handler.Register(
                "/api/v2/search",
                System.Net.HttpStatusCode.OK,
                TestFixtures.Load("off-search-alternatives.json"));

            services.AddSingleton(new FoodFetcher(new HttpClient(handler)
            {
                BaseAddress = new Uri("https://world.openfoodfacts.org/")
            }));
        });
    }
}
