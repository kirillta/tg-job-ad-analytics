using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Data.Messages.Converters;

public class TextEntriesConverter : ValueConverter<List<KeyValuePair<TgTextEntryType, string>>, string>
{
    public TextEntriesConverter(JsonSerializerOptions serializerOptions) 
        : base(v => ConvertToString(v, serializerOptions), v => ConvertFromString(v, serializerOptions))
    {
    }


    private static string ConvertToString(List<KeyValuePair<TgTextEntryType, string>> entries, JsonSerializerOptions serializerOptions)
    {
        if (entries == null || entries.Count == 0)
            return ArraySymbol;

        var serializable = entries.Select(kv => new { Type = kv.Key, kv.Value }).ToList();
        return JsonSerializer.Serialize(serializable, serializerOptions);
    }


    private static List<KeyValuePair<TgTextEntryType, string>> ConvertFromString(string json, JsonSerializerOptions serializerOptions)
    {
        if (string.IsNullOrEmpty(json) || json == ArraySymbol)
            return [];

        var deserialized = JsonSerializer.Deserialize<List<TextEntryData>>(json, serializerOptions);
        return deserialized?.Select(item => new KeyValuePair<TgTextEntryType, string>(item.Type, item.Value)).ToList() ?? [];
    }


    private class TextEntryData
    {
        public TgTextEntryType Type { get; set; }
        public string Value { get; set; } = string.Empty;
    }


    private const string ArraySymbol = "[]";
}
