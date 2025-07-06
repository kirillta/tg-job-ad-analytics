using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Models.Messages;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services.Messages;

public sealed partial class MessageProcessor
{
    public MessageProcessor(ParallelOptions parallelOptions)
    {
        _parallelOptions = parallelOptions;
    }


    public List<Message> Get(TgChat tgChat)
    {
        var chat = new ChatInfo(tgChat.Id, tgChat.Name);

        var adMessages = new ConcurrentBag<Message>();
        Parallel.ForEach(tgChat.Messages, _parallelOptions, tgMessage =>
        {
            if (!ShouldProcess(tgMessage))
                return;
            
            var message = Get(chat, tgMessage);
            if (message is null)
                return;

            adMessages.Add(message.Value);
        });

        return [.. adMessages];
    }


    private Message? Get(ChatInfo chatInfo, TgMessage tgMessage)
    {
        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var date = DateOnly.FromDateTime(tgMessage.Date);
        return new Message(GetSequentialId(), date, text, chatInfo, tgMessage.Id, Salary.Empty);


        long GetSequentialId()
        {
            lock (_lock)
            {
                return _id++;
            }
        }


        string GetText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var entity in tgMessage.TextEntities)
            {
                if (entity.Type is TgTextEntryType.PlainText)
                    stringBuilder.Append(entity.Text);
            }

            if (stringBuilder.Length < MinimalValuebleMessageLength)
                return string.Empty;

            return TextNormalizer.NormalizeTextEntry(stringBuilder.ToString());
        }
    }


    private static bool ShouldProcess(TgMessage tgMessage)
    {
        if (!IsAd())
            return false;

        if (IsCurrentMonth())
            return false;

        return true;


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
    }


    private static readonly HashSet<string> AdTags =
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
    ];


    private const int MinimalValuebleMessageLength = 300;


    private int _id;
    private readonly Lock _lock = new();
    private readonly ParallelOptions _parallelOptions;
}
