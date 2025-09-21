using Scriban;
using Scriban.Runtime;
using System.Text.Json;

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

        var scriptObjectWithFunctions = new ScriptObject();
        scriptObjectWithFunctions.Import("dump", new Func<object, string>(DumpObject));
       
        context.PushGlobal(scriptObjectWithFunctions);
        context.PushGlobal(scriptObject);

        return context;
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
