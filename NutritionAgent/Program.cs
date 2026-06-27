using System.Net;
using NutritionAgent.Domain;
using NutritionAgent.Endpoints;
using NutritionAgent.Infrastructure;
using NutritionAgent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenFoodFactsOptions>(
    builder.Configuration.GetSection(OpenFoodFactsOptions.SectionName));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Nutrition Intelligence Agent API";
        document.Info.Version = "v1";
        document.Info.Description =
            "Lookup packaged food by barcode via Open Food Facts, compute a rule-based nutrition score, " +
            "and suggest healthier alternatives. No API keys required.";
        return Task.CompletedTask;
    });
});

builder.Services.AddHttpClient("OpenFoodFacts", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenFoodFactsOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

    if (!string.IsNullOrWhiteSpace(options.UserAgent))
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All
});

builder.Services.AddHttpClient("OpenFoodFactsSearch", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenFoodFactsOptions>>().Value;
    client.BaseAddress = new Uri(options.SearchBaseUrl.TrimEnd('/') + "/");

    if (!string.IsNullOrWhiteSpace(options.UserAgent))
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All
});

builder.Services.AddSingleton<FoodFetcher>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new FoodFetcher(
        factory.CreateClient("OpenFoodFacts"),
        factory.CreateClient("OpenFoodFactsSearch"));
});

builder.Services.AddSingleton<NutritionScoringEngine>();
builder.Services.AddSingleton<ProductService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Nutrition API v1");
        options.DocumentTitle = "Nutrition Intelligence Agent";
    });
}

app.UseCors();

app.MapProductEndpoints();

app.Run();

public partial class Program;
