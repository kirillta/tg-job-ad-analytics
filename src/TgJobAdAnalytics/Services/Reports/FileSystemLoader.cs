using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace TgJobAdAnalytics.Services.Reports;

public class FileSystemLoader : ITemplateLoader
{
    private readonly string _rootPath;

    public FileSystemLoader(string rootPath)
    {
        _rootPath = rootPath;
    }


    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        return Path.Combine(_rootPath, templateName.Replace('/', Path.DirectorySeparatorChar));
    }


    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, templatePath));
        return File.ReadAllText(fullPath);
    }


    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, templatePath));
        return await File.ReadAllTextAsync(fullPath);
    }
}
