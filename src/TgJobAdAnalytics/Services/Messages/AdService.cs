using Scriban.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Messages;

public class AdService
{
    public AdService()
    {
    }


    public void Generate(List<MessageEntity> messages)
    {
        var timeStamp = DateTime.UtcNow;

        var results = new List<AdEntity>(messages.Count);
        foreach (var message in messages) 
        {
            if(!ShouldProcess(message))
                continue;

            var ad = Get(message, timeStamp);
            if (ad is null)
                continue;

            results.Add(ad);
        }

        return;
    }


    private static AdEntity? Get(MessageEntity message, DateTime timeStamp)
    {
        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return new AdEntity
        {
            Date = DateOnly.FromDateTime(message.TelegramMessageDate),
            Text = text,
            MessageId = message.Id,
            CreatedAt = timeStamp,
            UpdatedAt = timeStamp
        };


        string GetText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var entity in message.TextEntries)
            {
                if (entity.Key is TgTextEntryType.PlainText)
                    stringBuilder.Append(entity.Value);
            }

            if (stringBuilder.Length < MinimalValuebleAdLength)
                return string.Empty;

            return TextNormalizer.Normalize(stringBuilder.ToString());
        }
    }


    private static bool ShouldProcess(MessageEntity message)
    {
        if (!IsAd())
            return false;

        if (IsCurrentMonth())
            return false;

        return true;


        bool IsAd()
        {
            var hashTags = message.TextEntries
                .Where(entity => entity.Key is TgTextEntryType.HashTag)
                .ToList();

            if (hashTags.Count == 0)
                return false;

            if (!hashTags.Any(tag => AdTags.Contains(tag.Value)))
                return false;

            return true;
        }


        bool IsCurrentMonth()
            => message.TelegramMessageDate.Year == DateTime.UtcNow.Year &&
                message.TelegramMessageDate.Month == DateTime.UtcNow.Month;
    }


    private const int MinimalValuebleAdLength = 300;

    private static readonly HashSet<string> AdTags =
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
        "#fulltime",
        "#удаленка",
        "#офис",
        "#remote",
        "#удалёнка",
        "#удаленно",
        "#parttime",
        "#гибрид",
        "#work",
        "#удаленнаяработа",
        "#office",
        "#релокация",
        "#relocation"
    ];
}
