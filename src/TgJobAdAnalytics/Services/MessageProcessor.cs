using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using TgJobAdAnalytics.Models.Analytics;
using TgJobAdAnalytics.Models.Telegram;

namespace TgJobAdAnalytics.Services;

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
            var message = Get(chat, tgMessage);
            if (message is not null)
                adMessages.Add(message.Value);
        });

        return [.. adMessages];
    }


    private Message? Get(ChatInfo chatInfo, TgMessage tgMessage)
    {
        if (!IsAd())
            return null;

        if (IsCurrentMonth())
            return null;

        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var date = DateOnly.FromDateTime(tgMessage.Date);
        return new Message(GetSequentialId(), date, text, chatInfo, tgMessage.Id, Salary.Empty);


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

            return ClearText(stringBuilder.ToString());
        }


        static string ClearText(string text)
        {
            var rentedArray = ArrayPool<char>.Shared.Rent(text.Length);
            var clearedText = rentedArray.AsSpan(0, text.Length);
            text.AsSpan().ToLowerInvariant(clearedText);

            clearedText = ReplaceDashesWithOne(clearedText);
            clearedText = ExcludeNonAlphabeticOrNumbers(clearedText);
            clearedText = ReplaceMultipleSpacesWithOne(clearedText);
            clearedText = RemoveSpaceBetweenDigitAndCurrencySign(clearedText);
            clearedText = RemoveSpaceBetweenSalaryRangeBounds(clearedText);

            var result = clearedText.Trim().ToString();

            Array.Clear(rentedArray);
            ArrayPool<char>.Shared.Return(rentedArray);

            return result;


            static bool IsCurrencySymbol(char ch)
                => ch == '$' || ch == '€' || ch == '₽';


            static Span<char> ReplaceDashesWithOne(Span<char> text)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '—' || text[i] == '–')
                        text[i] = '-';
                }

                return text;
            }


            static Span<char> ExcludeNonAlphabeticOrNumbers(Span<char> text)
            {
                int index = 0;
                foreach (var ch in text)
                {
                    if (IsValidCharacter(ch))
                        text[index++] = ch;
                }

                return text[..index];


                static bool IsValidCharacter(char ch) 
                    => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch == '-' || IsCurrencySymbol(ch);
            }


            static Span<char> ReplaceMultipleSpacesWithOne(Span<char> text)
            {
                int index = 0;
                bool inSpace = false;
                foreach (var ch in text)
                {
                    if (char.IsWhiteSpace(ch))
                    {
                        if (!inSpace)
                        {
                            text[index++] = ' ';
                            inSpace = true;
                        }
                    }
                    else
                    {
                        text[index++] = ch;
                        inSpace = false;
                    }
                }

                return text[..index];
            }


            static Span<char> RemoveSpaceBetweenDigitAndCurrencySign(ReadOnlySpan<char> text)
            {
                Span<char> result = new char[text.Length];
                int index = 0;
                foreach (var ch in text)
                {
                    if (index > 0 && char.IsDigit(result[index - 1]) && ch == ' ' && index < text.Length - 1 && IsCurrencySymbol(text[index + 1]))
                        continue;

                    result[index++] = ch;
                }

                return result[..index];
            }
            

            static Span<char> RemoveSpaceBetweenSalaryRangeBounds(Span<char> text)
            {
                int index = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && char.IsDigit(text[i - 1]) && text[i] == ' ' && i < text.Length - 1 && text[i + 1] == '-')
                        continue;

                    
                    if (i > 0 && IsCurrencySymbol(text[i - 1]) && text[i] == ' ' && i < text.Length - 1 && text[i + 1] == '-')
                        continue;

                    if (i > 0 && text[i - 1] == '-' && text[i] == ' ' && i < text.Length - 1 && char.IsDigit(text[i + 1]))
                        continue;

                    if (i > 0 && text[i - 1] == '-' && text[i] == ' ' && i < text.Length - 1 && IsCurrencySymbol(text[i + 1]))
                        continue;

                    text[index++] = text[i];
                }

                return text[..index];
            }
        }
    }
    

    private static readonly HashSet<string> AdTags = 
    [
        "#вакансия",
        "#работа",
        "#job",
        "#vacancy",
    ];

    
    private int _id;
    private readonly Lock _lock = new();
    private readonly ParallelOptions _parallelOptions;
}
