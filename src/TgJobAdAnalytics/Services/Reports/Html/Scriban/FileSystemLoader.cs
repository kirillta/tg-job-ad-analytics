using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace TgJobAdAnalytics.Services.Reports.Html.Scriban;

public class FileSystemLoader : ITemplateLoader
{
    public FileSystemLoader(string? rootPath = null)
    {
        _rootPath = rootPath ?? string.Empty;
    }


    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        return Path.Combine(_rootPath, templateName.Replace('/', Path.DirectorySeparatorChar));
    }


    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var fullPath = Path.GetFullPath(templatePath);
        return File.ReadAllText(fullPath);
    }


    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var fullPath = Path.GetFullPath(templatePath);
        return await File.ReadAllTextAsync(fullPath);
    }


    private readonly string _rootPath;
}
