using System.Text.Json;
using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Utils.Serialization;

/// <summary>
/// JSON converter that maps non-finite nullable <see cref="double"/> values (NaN, +Infinity, -Infinity) to null when writing, ensuring valid JSON output.
/// </summary>
public sealed class NonFiniteNullableDoubleConverter : JsonConverter<double?>
{
    /// <inheritdoc />
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return reader.GetDouble();
    }


    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (double.IsFinite(value.Value))
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
