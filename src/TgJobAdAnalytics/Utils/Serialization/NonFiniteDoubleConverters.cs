using System.Text.Json;
using System.Text.Json.Serialization;

namespace TgJobAdAnalytics.Utils.Serialization;

/// <summary>
/// JSON converter that maps non-finite <see cref="double"/> values (NaN, +Infinity, -Infinity) to null when writing, ensuring valid JSON output.
/// </summary>
public sealed class NonFiniteDoubleConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDouble();


    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsFinite(value))
            writer.WriteNumberValue(value);
        else
            writer.WriteNullValue();
    }
}
