using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Salaries;

namespace TgJobAdAnalytics.Services;

public sealed class MessageProcessor
{
    public MessageProcessor(SalaryService salaryService)
    {
        _salaryService = salaryService;
    }


    public async ValueTask<List<Message>> Get(List<TgMessage> tgMessages)
    {
        var adMessages = new ConcurrentBag<Message>();
        await Parallel.ForEachAsync(tgMessages, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async (tgMessage, cancellationToken) =>
        {
            var message = await Get(tgMessage);
            if (message is not null)
                adMessages.Add(message.Value);
        });

        return [.. adMessages.OrderByDescending(message => message.Date)];
    }


    public async ValueTask<Message?> Get(TgMessage tgMessage)
    {
        if (!IsAd())
            return null;

        if (IsCurrentMonth())
            return null;

        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var date = DateOnly.FromDateTime(tgMessage.Date);
        var salary = await _salaryService.Get(text, date);
        if (salary.Currency == Currency.Unknown)
            return null;

        return new Message(tgMessage.Id, date, text, salary);


        bool IsAd()
        {
            var hashTags = tgMessage.TextEntities
                .Where(entity => entity.Type is TgTextEntryType.HashTag)
                .ToList();

            if (hashTags.Count == 0)
                return false;

            if (!hashTags.Any(tag => AdTags.Contains(tag.Text)))
                return false;

            return true;
        }


        bool IsCurrentMonth() 
            => tgMessage.Date.Year == DateTime.Now.Year && 
                tgMessage.Date.Month == DateTime.Now.Month;


        string GetText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var entity in tgMessage.TextEntities)
            {
                if (entity.Type is TgTextEntryType.PlainText)
                    stringBuilder.Append(entity.Text);
            }

            return stringBuilder.ToString();
        }
    }


    private readonly HashSet<string> AdTags = 
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
    ];


    private readonly SalaryService _salaryService;
}
