using Scriban;
using Scriban.Runtime;
using System.Text.Json;
using TgJobAdAnalytics.Models.Reports.Html;

namespace TgJobAdAnalytics.Services.Reports.Html.Scriban;

internal class TemplateRenderer
{
    public TemplateRenderer(string templatesPath)
    {
        _templatesPath = templatesPath;
        _loader = new FileSystemLoader(_templatesPath);
    }


    public string Render(object model)
    {
        var template = GetTemplate();
        var context = CreateContext(model);

        return template.Render(context);
    }


    private Template GetTemplate()
    {
        var reportTemplatePath = Path.Combine(_templatesPath, "MainTemplate.sbn");
        var templateContent = File.ReadAllText(reportTemplatePath);
        
        return Template.Parse(templateContent);
    }


    private TemplateContext CreateContext(object model)
    {
        var scriptObject = new ScriptObject();
        scriptObject.Import(model);

        var context = new TemplateContext
        {
            TemplateLoader = _loader,
            EnableRelaxedMemberAccess = true
        };

        var helpers = new ScriptObject();
        helpers.Import("dump", new Func<object, string>(DumpObject));

        if (model is ReportModel reportModel)
        {
            var localizationWrapper = new ScriptObject();
            foreach (var kv in reportModel.Localization)
                localizationWrapper.Add(kv.Key, ToScriptFriendly(kv.Value));
            
            helpers.Add("l", localizationWrapper);
        }

        context.PushGlobal(helpers);
        context.PushGlobal(scriptObject);

        return context;
    }


    private static object ToScriptFriendly(object value)
    {
        if (value is Dictionary<string, object> dict)
        {
            var so = new ScriptObject();

            foreach (var kv in dict)
                so.Add(kv.Key, ToScriptFriendly(kv.Value));
            
            return so;
        }

        return value;
    }


    private string DumpObject(object obj) 
        => JsonSerializer.Serialize(obj, _jsonOptions);


    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly FileSystemLoader _loader;
    private readonly string _templatesPath;
}
