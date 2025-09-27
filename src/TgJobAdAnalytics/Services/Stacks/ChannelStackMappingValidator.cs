using Microsoft.EntityFrameworkCore;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Stacks;

namespace TgJobAdAnalytics.Services.Stacks;

/// <summary>
/// Validates mapping content: duplicates, unknown stacks, empty file.
/// </summary>
public sealed class ChannelStackMappingValidator
{
    public ChannelStackMappingValidator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task ValidateOrThrow(ChannelStackMapping mapping, CancellationToken cancellationToken)
    {
        if (mapping.Channels.Count == 0)
            throw new InvalidOperationException("Stack mapping contains no channels.");

        var duplicates = mapping.Channels
            .GroupBy(c => Normalize(c.Channel))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Duplicate channel entries in stack mapping: {string.Join(", ", duplicates)}");

        var canonicalStacks = await _dbContext.TechnologyStacks
            .AsNoTracking()
            .Select(s => s.Name.ToLowerInvariant())
            .ToHashSetAsync(cancellationToken);

        var unknown = mapping.Channels
            .Select(c => c.Stack?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(s => !canonicalStacks.Contains(s))
            .Distinct()
            .ToList();

        if (unknown.Count > 0)
            throw new InvalidOperationException($"Unknown stack names in mapping (not in canonical set): {string.Join(", ", unknown)}");
    }


    private static string Normalize(string s)
        => (s ?? string.Empty).Trim().ToLowerInvariant();


    private readonly ApplicationDbContext _dbContext;
}
