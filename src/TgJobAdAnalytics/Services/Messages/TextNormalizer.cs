using System.Buffers;

namespace TgJobAdAnalytics.Services.Messages;

public static class TextNormalizer
{
    public static string NormalizeTextEntry(string text)
    {
        var rentedArray = ArrayPool<char>.Shared.Rent(text.Length);
        var clearedText = rentedArray.AsSpan(0, text.Length);
        text.Normalize().AsSpan().CopyTo(clearedText);

        clearedText = ReplaceDashesWithOne(clearedText);
        clearedText = ExcludeNonAlphabeticOrNumbers(clearedText);
        clearedText = ReplaceMultipleSpacesWithOne(clearedText);
        clearedText = ReplaceCurrencyNamesWithSymbols(clearedText);
        clearedText = ReplaceMultipleSpacesWithOne(clearedText);
        clearedText = RemoveThousandSeparators(clearedText);
        clearedText = RemoveSpaceBetweenDigitAndCurrencySign(clearedText);
        clearedText = RemoveSpaceBetweenSalaryRangeBounds(clearedText);

        var result = clearedText.Trim().ToString();

        Array.Clear(rentedArray);
        ArrayPool<char>.Shared.Return(rentedArray);

        return result;
    }


    public static string NormalizeAd(string text)
    {
        var rentedArray = ArrayPool<char>.Shared.Rent(text.Length);
        var clearedText = rentedArray.AsSpan(0, text.Length);
        text.CopyTo(clearedText);

        clearedText = RemoveSpaceBeforeSeparator(clearedText);
        clearedText = ReplaceMultipleSpacesWithOne(clearedText);

        var result = clearedText.Trim().ToString();

        Array.Clear(rentedArray);
        ArrayPool<char>.Shared.Return(rentedArray);

        return result;
    }


    private static bool IsCurrencySymbol(char ch)
        => ch == '$' || ch == '€' || ch == '₽';


    private static Span<char> ReplaceDashesWithOne(Span<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '—' || text[i] == '–')
                text[i] = '-';
        }

        return text;
    }


    private static Span<char> ExcludeNonAlphabeticOrNumbers(Span<char> text)
    {
        int index = 0;
        foreach (var ch in text)
        {
            if (IsValidCharacter(ch))
                text[index++] = ch;
        }

        return text[..index];


        static bool IsValidCharacter(char ch)
            => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) || ch == '-' || ch == '#' || IsCurrencySymbol(ch);
    }


    private static Span<char> ReplaceMultipleSpacesWithOne(Span<char> text)
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


    private static Span<char> ReplaceCurrencyNamesWithSymbols(Span<char> text)
    {
        Span<char> result = stackalloc char[text.Length];
        text.CopyTo(result);

        if (ContainsCurrencyName(result, RubNames.Span))
            result = ReplaceCurrencyName(result, RubNames.Span, '₽');

        if (ContainsCurrencyName(result, UsdNames.Span))
            result = ReplaceCurrencyName(result, UsdNames.Span, '$');

        if (ContainsCurrencyName(result, EuroNames.Span))
            result = ReplaceCurrencyName(result, EuroNames.Span, '€');

        result.CopyTo(text);
        return text[..result.Length];


        static bool ContainsCurrencyName(ReadOnlySpan<char> text, ReadOnlySpan<string> currencyNames)
        {
            foreach (var name in currencyNames)
            {
                if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }


        static Span<char> ReplaceCurrencyName(Span<char> text, ReadOnlySpan<string> currencyNames, char symbol)
        {
            foreach (var name in currencyNames)
            {
                var nameSpan = name.AsSpan();
                var index = MemoryExtensions.IndexOf(text, nameSpan, StringComparison.OrdinalIgnoreCase);

                while (index != -1)
                {
                    text.Slice(index, nameSpan.Length).Fill(' ');
                    text[index] = symbol;

                    var nextIndex = MemoryExtensions.IndexOf(text[(index + nameSpan.Length)..], nameSpan, StringComparison.OrdinalIgnoreCase);
                    if (nextIndex == -1)
                        break;

                    index += nextIndex + nameSpan.Length;
                }
            }

            return text;
        }
    }


    private static Span<char> RemoveThousandSeparators(Span<char> text)
    {
        int index = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ' && i > 0 && i < text.Length - 1 && char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1]))
                continue;

            text[index++] = text[i];
        }

        return text[..index];
    }


    private static Span<char> RemoveSpaceBeforeSeparator(Span<char> text)
    {
        int index = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '.' || text[i] == ',' || text[i] == ';' || text[i] == ':')
            {
                if (i > 0 && text[i - 1] == ' ')
                    continue;
            }

            text[index++] = text[i];
        }

        return text[..index];
    }


    private static Span<char> RemoveSpaceBetweenDigitAndCurrencySign(ReadOnlySpan<char> text)
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


    private static Span<char> RemoveSpaceBetweenSalaryRangeBounds(Span<char> text)
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


    private static readonly ReadOnlyMemory<string> UsdNames = new[]
    {
        "долларов",
        "доллар",
        "usd"
    };

    private static readonly ReadOnlyMemory<string> RubNames = new[]
    {
        "рублей",
        "рубль",
        "рубли",
        "руб",
        " р ",
        " р.",
        " р,"
    };

    private static readonly ReadOnlyMemory<string> EuroNames = new[]
    {
        "euro",
        "евро",
        "eur"
    };
}
