using System.Text.RegularExpressions;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

internal sealed partial class SalaryPattenrFactory
{
    public static List<SalaryPattern> Get()
    {
        return
        [
            new SalaryPattern(Pattern1Regex(), Currency.RUB, BoundaryType.Both, "50k-100k"),
            new SalaryPattern(Pattern2Regex(), Currency.RUB, BoundaryType.Both, "от 70к до 150к рублей"),
            new SalaryPattern(Pattern4Regex(), Currency.USD, BoundaryType.Both, "от 4000 до 5000$"),
            new SalaryPattern(Pattern5Regex(), Currency.RUB, BoundaryType.Both, "110-160 тр/мес"),
            //new SalaryPattern(Pattern6Regex(), Currency.RUB, BoundaryType.Both, "300 000 - 420 000 gross"),
            new SalaryPattern(Pattern7Regex(), Currency.RUB, BoundaryType.Both, "от 200 000 до 270 000 рублей"),
            new SalaryPattern(Pattern8Regex(), Currency.USD, BoundaryType.Upper, "до 4000$ gross"),
            new SalaryPattern(Pattern9Regex(), Currency.RUB, BoundaryType.Upper, "до 330 гросс"),
            new SalaryPattern(Pattern10Regex(), Currency.USD, BoundaryType.Both, "$2500-$4500"),
            new SalaryPattern(Pattern11Regex(), Currency.RUB, BoundaryType.Both, "130-200к руб"),
            new SalaryPattern(Pattern12Regex(), Currency.RUB, BoundaryType.Upper, "до 190 000"),
            new SalaryPattern(Pattern13Regex(), Currency.RUB, BoundaryType.Both, "вилка 80-120"),
            new SalaryPattern(Pattern14Regex(), Currency.RUB, BoundaryType.Lower, "от 100 000 рублей"),
            new SalaryPattern(Pattern15Regex(), Currency.RUB, BoundaryType.Both, "зп 150-190k"),
            new SalaryPattern(Pattern16Regex(), Currency.USD, BoundaryType.Upper, "salary 3000$"),
            new SalaryPattern(Pattern17Regex(), Currency.RUB, BoundaryType.Both, "зп 120-180"),
            new SalaryPattern(Pattern18Regex(), Currency.USD, BoundaryType.Both, "1500-3000$"),
            new SalaryPattern(Pattern19Regex(), Currency.RUB, BoundaryType.Upper, "зп 150 000 р"),
            new SalaryPattern(Pattern20Regex(), Currency.RUB, BoundaryType.Lower, "от 70 000 на руки")
        ];
    }

    
    // 50k-100k
    [GeneratedRegex(@"(\d{1,3}k)-(\d{1,3}k)", RegexOptions.Compiled)]
    private static partial Regex Pattern1Regex();

    // от 70к до 150к рублей
    [GeneratedRegex(@"от\s*(\d{1,3}к)\s*до\s*(\d{1,3}к)\s*рублей", RegexOptions.Compiled)]
    private static partial Regex Pattern2Regex();

    // от 4000 до 5000$
    [GeneratedRegex(@"от\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*до\s*(\d{1,3}(?:[ ,]?\d{3})*)\$", RegexOptions.Compiled)]
    private static partial Regex Pattern4Regex();

    // 110-160 тр/мес
    [GeneratedRegex(@"(\d{1,3})-(\d{1,3})\s*тр/мес", RegexOptions.Compiled)]
    private static partial Regex Pattern5Regex();

    // 300 000 - 420 000 gross
    [GeneratedRegex(@"(\d{1,3}(?:[ ,]?\d{3})*)\s*-\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*gross", RegexOptions.Compiled)]
    private static partial Regex Pattern6Regex();

    // от 200 000 до 270 000 рублей
    [GeneratedRegex(@"от\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*до\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*рублей", RegexOptions.Compiled)]
    private static partial Regex Pattern7Regex();

    // до 4000$ gross
    [GeneratedRegex(@"до\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*\$\s*gross", RegexOptions.Compiled)]
    private static partial Regex Pattern8Regex();

    // до 330 гросс
    [GeneratedRegex(@"до\s*(\d{1,3})\s*гросс", RegexOptions.Compiled)]
    private static partial Regex Pattern9Regex();

    // $2500-$4500
    [GeneratedRegex(@"\$(\d{1,3}(?:[ ,]?\d{3})*)\s*-\s*\$(\d{1,3}(?:[ ,]?\d{3})*)", RegexOptions.Compiled)]
    private static partial Regex Pattern10Regex();

    // 130-200к руб
    [GeneratedRegex(@"(\d{1,3})-(\d{1,3}к)\s*руб", RegexOptions.Compiled)]
    private static partial Regex Pattern11Regex();

    // до 190 000
    [GeneratedRegex(@"до\s*(\d{1,3}(?:[ ,]?\d{3})*)", RegexOptions.Compiled)]
    private static partial Regex Pattern12Regex();

    // вилка 80-120
    [GeneratedRegex(@"вилка\s*(\d{1,3})-(\d{1,3})", RegexOptions.Compiled)]
    private static partial Regex Pattern13Regex();

    // от 100 000 рублей
    [GeneratedRegex(@"от\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*рублей", RegexOptions.Compiled)]
    private static partial Regex Pattern14Regex();

    // зп 150-190k
    [GeneratedRegex(@"зп\s*(\d{1,3})-(\d{1,3}k)", RegexOptions.Compiled)]
    private static partial Regex Pattern15Regex();

    // salary 3000$
    [GeneratedRegex(@"salary\s*(\d{1,3}(?:[ ,]?\d{3})*)\$", RegexOptions.Compiled)]
    private static partial Regex Pattern16Regex();

    // зп 120-180
    [GeneratedRegex(@"зп\s*(\d{1,3})-(\d{1,3})", RegexOptions.Compiled)]
    private static partial Regex Pattern17Regex();

    // 1500-3000$
    [GeneratedRegex(@"(\d{1,3}(?:[ ,]?\d{3})*)-(\d{1,3}(?:[ ,]?\d{3})*)\$", RegexOptions.Compiled)]
    private static partial Regex Pattern18Regex();

    // зп 150 000 р
    [GeneratedRegex(@"зп\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*р", RegexOptions.Compiled)]
    private static partial Regex Pattern19Regex();

    // от 70 000 на руки
    [GeneratedRegex(@"от\s*(\d{1,3}(?:[ ,]?\d{3})*)\s*на\s*руки", RegexOptions.Compiled)]
    private static partial Regex Pattern20Regex();
}
