using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        context.PushGlobal(scriptObject);

        return context;
    }


    private readonly FileSystemLoader _loader;
    private readonly string _templatesPath;
}
