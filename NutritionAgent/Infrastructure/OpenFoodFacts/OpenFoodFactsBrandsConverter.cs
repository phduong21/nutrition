using System.Text.Json;
using System.Text.Json.Serialization;

namespace NutritionAgent.Infrastructure.OpenFoodFacts;

/// <summary>
/// OFF returns brands as a string or string[] depending on endpoint/version.
/// </summary>
internal sealed class OpenFoodFactsBrandsConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartArray => ReadArray(ref reader),
            JsonTokenType.Null => null,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);

    private static string? ReadArray(ref Utf8JsonReader reader)
    {
        var brands = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var brand = reader.GetString();
                if (!string.IsNullOrWhiteSpace(brand))
                    brands.Add(brand);
            }
        }

        return brands.Count == 0 ? null : string.Join(", ", brands);
    }
}
