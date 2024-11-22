using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Telegram;
using TgJobAdAnalytics.Services.Salaries;

namespace TgJobAdAnalytics.Services;

public sealed partial class MessageProcessor
{
    public MessageProcessor(SalaryService salaryService)
    {
        _salaryService = salaryService;
    }


    public List<Message> Get(List<TgMessage> tgMessages)
    {
        var adMessages = new ConcurrentBag<Message>();
        Parallel.ForEach(tgMessages, new ParallelOptions { MaxDegreeOfParallelism = 1 }, tgMessage =>
        {
            var message = Get(tgMessage);
            if (message is not null)
                adMessages.Add(message.Value);
        });

        return [.. adMessages];
    }


    public Message? Get(TgMessage tgMessage)
    {
        if (!IsAd())
            return null;

        if (IsCurrentMonth())
            return null;

        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var date = DateOnly.FromDateTime(tgMessage.Date);
        return new Message(tgMessage.Id, date, text, Salary.Empty);


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

            return ClearText(stringBuilder.ToString());
        }


        static string ClearText(string text)
        {
            Span<char> clearedText = stackalloc char[text.Length];
            text.AsSpan().ToLowerInvariant(clearedText);
            clearedText = ExcludeNonAlphabeticOrNumbers(clearedText);
            clearedText = ReplaceMultipleSpacesWithOne(clearedText);

            return clearedText.Trim().ToString();
        }
    }


    private static readonly HashSet<string> AdTags = 
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
    ];
    

    private static Span<char> ExcludeNonAlphabeticOrNumbers(Span<char> text)
    {
        Span<char> result = new char[text.Length];
        int index = 0;
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                result[index++] = ch;
            }
        }

        return result[..index];
    }


    private static Span<char> ReplaceMultipleSpacesWithOne(Span<char> text)
    {
        Span<char> result = new char[text.Length];
        int index = 0;
        bool inSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!inSpace)
                {
                    result[index++] = ' ';
                    inSpace = true;
                }
            }
            else
            {
                result[index++] = ch;
                inSpace = false;
            }
        }

        return result[..index];
    }


    private readonly SalaryService _salaryService;
}
