using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

public sealed class ChannelStackMappingValidator
{
    public ChannelStackMappingValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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
            throw new InvalidOperationException($"Duplicate chat ids in stack mapping: {string.Join(", ", duplicateChatIds)}");

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
            throw new InvalidOperationException($"Unknown stack names in mapping (not in canonical set): {string.Join(", ", unknown)}");
    }


    private readonly ApplicationDbContext _dbContext;
}
