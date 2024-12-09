using System.Text.RegularExpressions;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

internal sealed partial class SalaryPattenrFactory
{
    public static List<SalaryPattern> Get()
    {
        return
        [
            new SalaryPattern(Pattern01Regex(), Currency.RUB, BoundaryType.Both, "10-100k, 10k-100k"),
            new SalaryPattern(Pattern02Regex(), Currency.RUB, BoundaryType.Both, "10000-100000"),
            new SalaryPattern(Pattern11Regex(), Currency.USD, BoundaryType.Both, "$1000, 1000$"),
            new SalaryPattern(Pattern12Regex(), Currency.USD, BoundaryType.Both, "$1000-$2000, 1000$-2000$, 1000-2000$, $1000-2000"),
            new SalaryPattern(Pattern13Regex(), Currency.USD, BoundaryType.Both, "от 1000 до 2000$, от 1000$ до 2000$, от $1000 до $2000"),
            new SalaryPattern(Pattern14Regex(), Currency.USD, BoundaryType.Lower, "от $1000, от 1000$"),
            new SalaryPattern(Pattern15Regex(), Currency.USD, BoundaryType.Upper, "до $1000, до 1000$"),
            new SalaryPattern(Pattern21Regex(), Currency.EUR, BoundaryType.Both, "€1000, 1000€"),
            new SalaryPattern(Pattern22Regex(), Currency.EUR, BoundaryType.Both, "€1000-€2000, 1000€-2000€, 1000-2000€, €1000-2000"),
            new SalaryPattern(Pattern23Regex(), Currency.EUR, BoundaryType.Both, "от 1000 до 2000€, от 1000€ до 2000€, от €1000 до €2000"),
            new SalaryPattern(Pattern24Regex(), Currency.EUR, BoundaryType.Lower, "от €1000, от 1000€"),
            new SalaryPattern(Pattern25Regex(), Currency.EUR, BoundaryType.Upper, "до €1000, до 1000€"),
            new SalaryPattern(Pattern31Regex(), Currency.RUB, BoundaryType.Both, "₽1000, 1000₽"),
            new SalaryPattern(Pattern32Regex(), Currency.RUB, BoundaryType.Both, "₽10000-₽20000, 10000₽-20000₽, 10000-20000₽, ₽10000-20000"),
            new SalaryPattern(Pattern33Regex(), Currency.RUB, BoundaryType.Both, "от 1000 до 2000₽, от 1000₽ до 2000₽, от ₽1000 до ₽2000"),
            new SalaryPattern(Pattern34Regex(), Currency.RUB, BoundaryType.Lower, "от ₽1000, от 1000₽"),
            new SalaryPattern(Pattern35Regex(), Currency.RUB, BoundaryType.Upper, "до ₽1000, до 1000₽")
        ];
    }

    
    // 10k-100k, 10-100k
    [GeneratedRegex(@"(\d{2,3}k?)-(\d{3}k)", RegexOptions.Compiled)]
    private static partial Regex Pattern01Regex();

    // 10000-100000
    [GeneratedRegex(@"(\d+000)-(\d+000)", RegexOptions.Compiled)]
    private static partial Regex Pattern02Regex();


    // $1000, 1000$
    [GeneratedRegex(@"(?:\$(\d{3,5})|(\d{3,5})\$)", RegexOptions.Compiled)]
    private static partial Regex Pattern11Regex();

    // $1000-$2000, 1000$-2000$, 1000-2000$, $1000-2000
    [GeneratedRegex(@"(\$\d{3,5})-(\$\d{3,5})|(\d{3,5})\$-(\d{3,5})\$|(\d{3,5})-(\d{3,5})\$", RegexOptions.Compiled)]
    private static partial Regex Pattern12Regex();

    // от 1000 до 2000$, от 1000$ до 2000$, от $1000 до $2000
    [GeneratedRegex(@"от\s*(?:\$)?(\d{3,5})(?:\$)?\s*до\s*(?:\$)?(\d{3,5})\$?", RegexOptions.Compiled)]
    private static partial Regex Pattern13Regex();

    // от $1000, от 1000$
    [GeneratedRegex(@"от\s*(?:\$)?(\d{3,5})\$?", RegexOptions.Compiled)]
    private static partial Regex Pattern14Regex();

    // до $1000, до 1000$
    [GeneratedRegex(@"до\s*(?:\$)?(\d{3,5})\$?", RegexOptions.Compiled)]
    private static partial Regex Pattern15Regex();


    // €1000, 1000€
    [GeneratedRegex(@"(?:\€(\d{3,5})|(\d{3,5})\€)", RegexOptions.Compiled)]
    private static partial Regex Pattern21Regex();

    // €1000-€2000, 1000€-2000€, 1000-2000€, €1000-2000
    [GeneratedRegex(@"(€\d{3,5})-(€\d{3,5})|(\d{3,5})€-(\d{3,5})€|(\d{3,5})-(\d{3,5})€", RegexOptions.Compiled)]
    private static partial Regex Pattern22Regex();

    // от 1000 до 2000€, от 1000€ до 2000€, от €1000 до €2000
    [GeneratedRegex(@"от\s*(?:€)?(\d{3,5})(?:€)?\s*до\s*(?:€)?(\d{3,5})€?", RegexOptions.Compiled)]
    private static partial Regex Pattern23Regex();

    // от €1000, от 1000€
    [GeneratedRegex(@"от\s*(?:€)?(\d{3,5})€?", RegexOptions.Compiled)]
    private static partial Regex Pattern24Regex();

    // до €1000, до 1000€
    [GeneratedRegex(@"до\s*(?:€)?(\d{3,5})€?", RegexOptions.Compiled)]
    private static partial Regex Pattern25Regex();


    // ₽1000, 1000₽
    [GeneratedRegex(@"(?:\₽(\d{4,6})|(\d{4,6})\₽)", RegexOptions.Compiled)]
    private static partial Regex Pattern31Regex();

    // ₽10000-₽20000, 10000₽-20000₽, 10000-20000₽, ₽10000-20000
    [GeneratedRegex(@"(₽\d{4,6})-(₽\d{4,6})|(\d{4,6})₽-(\d{4,6})₽|(\d{4,6})-(\d{4,6})₽", RegexOptions.Compiled)]
    private static partial Regex Pattern32Regex();

    // от 1000 до 2000₽, от 1000₽ до 2000₽, от ₽1000 до ₽2000
    [GeneratedRegex(@"от\s*(?:₽)?(\d{4,6})(?:₽)?\s*до\s*(?:₽)?(\d{4,6})₽?", RegexOptions.Compiled)]
    private static partial Regex Pattern33Regex();

    // от ₽1000, от 1000₽
    [GeneratedRegex(@"от\s*(?:₽)?(\d{4,6})₽?", RegexOptions.Compiled)]
    private static partial Regex Pattern34Regex();

    // до ₽1000, до 1000₽
    [GeneratedRegex(@"до\s*(?:₽)?(\d{4,6})₽?", RegexOptions.Compiled)]
    private static partial Regex Pattern35Regex();
}
