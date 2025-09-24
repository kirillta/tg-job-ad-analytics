using TgJobAdAnalytics.Services.Localization;

namespace TgJobAdAnalytics.Services.Reports.Html;

public sealed class UiLocalizer
{ 
    public UiLocalizer(ILocalizationProvider localizationProvider)
    {
        _localizationProvider = localizationProvider;
    }


    public Dictionary<string, object> BuildLocalizationDictionary(string locale)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ui"] = GetUiRoot(locale),
            ["variant"] = GetVariantRoot(locale),
            ["level"] = GetLevelRoot(locale)
        };


        Dictionary<string, object> GetLevelRoot(string locale)
        {
            var levelRoot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in _levelKeys)
            {
                var shortKey = key.Split('.')[1];
                levelRoot[shortKey] = _localizationProvider.Get(locale, key); 
            }

            return levelRoot;
        }


        Dictionary<string, object> GetVariantRoot(string locale) 
            => new(StringComparer.OrdinalIgnoreCase)
            {
                ["all"] = _localizationProvider.Get(locale, "variant.all")
            };


        Dictionary<string, object> GetUiRoot(string locale)
        {
            var uiRoot = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var fullKey in _uiKeys)
            {
                var path = fullKey.Substring(3).Split('.');

                Dictionary<string, object> current = uiRoot;
                for (int i = 0; i < path.Length; i++)
                {
                    var segment = path[i];
                
                    var isLeaf = i == path.Length - 1;
                    if (isLeaf)
                    {
                        string localizedValue;
                        localizedValue = _localizationProvider.Get(locale, fullKey); 
                        current[segment] = localizedValue;
                        
                        continue;
                    }

                    if (!current.TryGetValue(segment, out var next) || next is not Dictionary<string, object> nextDict)
                    {
                        nextDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        current[segment] = nextDict;
                    }

                    current = nextDict;
                }
            }

            return uiRoot;
        }
    }


    private static readonly string[] _levelKeys =
    [
        "level.intern",
        "level.junior",
        "level.middle",
        "level.senior",
        "level.lead",
        "level.architect",
        "level.manager"
    ];

    private static readonly string[] _uiKeys =
    [
        "ui.updated",
        "ui.button.show_table",
        "ui.button.hide_table",
        "ui.data_sources.title",
        "ui.data_sources.messages_label",
        "ui.data_sources.salaries_label",
        "ui.data_sources.explainer",
        "ui.footer.author",
        "ui.footer.source",
        "ui.footer.built_with",
        "ui.footer.and",
        "ui.chart.position_label"
    ];


    private readonly ILocalizationProvider _localizationProvider;
}
