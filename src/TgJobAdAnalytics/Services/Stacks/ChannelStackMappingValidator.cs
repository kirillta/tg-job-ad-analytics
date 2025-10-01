using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

public sealed class ChannelStackMappingValidator
{
    public ChannelStackMappingValidator(ILoggerFactory loggerFactory, ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = loggerFactory.CreateLogger<ChannelStackMappingValidator>();
    }


    public void ValidateOrThrow(ChannelStackMapping mapping)
    {
        if (mapping.Channels.Count == 0)
            throw new InvalidOperationException("Stack mapping contains no channels.");

        var duplicateChatIds = mapping.Channels
            .GroupBy(c => c.ChatId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateChatIds.Count > 0)
        {
            _logger.LogCritical("Duplicate chat ids in stack mapping: {ChatIds}", string.Join(", ", duplicateChatIds));
            throw new InvalidOperationException($"Duplicate chat ids in stack mapping: {string.Join(", ", duplicateChatIds)}");
        }

        var canonicalStacks = _dbContext.TechnologyStacks
            .AsNoTracking()
            .Select(s => s.Name.ToLowerInvariant())
            .ToHashSet();

        var unknown = mapping.Channels
            .Select(c => c.StackName?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(s => !canonicalStacks.Contains(s))
            .Distinct()
            .ToList();

        if (unknown.Count > 0)
        {
            _logger.LogCritical("Unknown stack names in mapping: {StackNames}", string.Join(", ", unknown));
            throw new InvalidOperationException($"Unknown stack names in mapping: {string.Join(", ", unknown)}");
        }

    }


    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChannelStackMappingValidator> _logger;
}
