using System.Text.RegularExpressions;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Levels;

/// <summary>
/// Rule based position level resolver using provided ad tags.
/// </summary>
public sealed class PositionLevelResolver
{
    public static PositionLevel Resolve(IEnumerable<string> tags)
    {
        if (tags is null)
            return PositionLevel.Unknown;

        var maxRank = 0;
        var detected = PositionLevel.Unknown;

        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var tag = Normalize(raw);
            if (tag.Length == 0)
                continue;

            if (_exact.TryGetValue(tag, out var level))
            {
                Update(level);
                continue;
            }

            foreach (var (regex, levelCandidate) in _patterns)
            {
                if (maxRank >= (int)PositionLevel.Manager)
                    break;

                if (regex.IsMatch(tag))
                {
                    Update(levelCandidate);
                    break;
                }
            }
        }

        return detected;


        void Update(PositionLevel level)
        {
            var rank = (int)level;
            if (rank > maxRank)
            {
                maxRank = rank;
                detected = level;
            }
        }
    }


    private static string Normalize(string tag)
    {
        tag = tag.Trim().ToLowerInvariant();
        if (tag.StartsWith('#'))
            tag = tag[1..];

        while (tag.Contains("__"))
            tag = tag.Replace("__", "_");

        return tag;
    }


    private static readonly Dictionary<string, PositionLevel> _exact = new()
    {
        // Intern
        ["intern"] = PositionLevel.Intern,
        ["intership"] = PositionLevel.Intern,
        ["interships"] = PositionLevel.Intern,
        ["trainee"] = PositionLevel.Intern,
        ["стажер"] = PositionLevel.Intern,
        ["стажёр"] = PositionLevel.Intern,
        ["стажерка"] = PositionLevel.Intern,
        ["стажировка"] = PositionLevel.Intern,
        ["стажировки"] = PositionLevel.Intern,
        ["student"] = PositionLevel.Intern,
        ["студент"] = PositionLevel.Intern,
        ["студенты"] = PositionLevel.Intern,
        // Junior
        ["junior"] = PositionLevel.Junior,
        ["juniorbackend"] = PositionLevel.Junior,
        ["juniordeveloper"] = PositionLevel.Junior,
        ["juniorsystemsanalyst"] = PositionLevel.Junior,
        ["strong_junior"] = PositionLevel.Junior,
        ["strongjunior"] = PositionLevel.Junior,
        ["jr"] = PositionLevel.Junior,
        ["джун"] = PositionLevel.Junior,
        ["джуниор"] = PositionLevel.Junior,
        ["джуны"] = PositionLevel.Junior,
        ["джунытожепрограммисты"] = PositionLevel.Junior,
        // Middle
        ["middle"] = PositionLevel.Middle,
        ["middledeveloper"] = PositionLevel.Middle,
        ["middledevops"] = PositionLevel.Middle,
        ["middleangular"] = PositionLevel.Middle,
        ["middleplus"] = PositionLevel.Middle,
        ["mid"] = PositionLevel.Middle,
        ["middl"] = PositionLevel.Middle,
        ["midddle"] = PositionLevel.Middle,
        ["midle"] = PositionLevel.Middle,
        ["midlle"] = PositionLevel.Middle,
        ["middlle"] = PositionLevel.Middle,
        ["mидл"] = PositionLevel.Middle,
        ["миддл"] = PositionLevel.Middle,
        ["мидл"] = PositionLevel.Middle,
        ["мидль"] = PositionLevel.Middle,
        ["intermediate"] = PositionLevel.Middle,
        // Senior
        ["senior"] = PositionLevel.Senior,
        ["senior_"] = PositionLevel.Senior,
        ["senior_developer"] = PositionLevel.Senior,
        ["seniordeveloper"] = PositionLevel.Senior,
        ["seniorangular"] = PositionLevel.Senior,
        ["seniordevops"] = PositionLevel.Senior,
        ["seniorfullstack"] = PositionLevel.Senior,
        ["seniornetcoredeveloper"] = PositionLevel.Senior,
        ["sr"] = PositionLevel.Senior,
        ["senoir"] = PositionLevel.Senior,
        ["senor"] = PositionLevel.Senior,
        // Lead
        ["lead"] = PositionLevel.Lead,
        ["leader"] = PositionLevel.Lead,
        ["teamlead"] = PositionLevel.Lead,
        ["team_lead"] = PositionLevel.Lead,
        ["techlead"] = PositionLevel.Lead,
        ["тимлид"] = PositionLevel.Lead,
        ["техлид"] = PositionLevel.Lead,
        ["tehlid"] = PositionLevel.Lead,
        ["tl"] = PositionLevel.Lead,
        ["tl_senior"] = PositionLevel.Lead,
        ["ведущий"] = PositionLevel.Lead,
        ["ведущийразработчик"] = PositionLevel.Lead,
        ["ведущийразработчикnet"] = PositionLevel.Lead,
        ["лид"] = PositionLevel.Lead,
        // Architect
        ["architect"] = PositionLevel.Architect,
        ["applicationarchitect"] = PositionLevel.Architect,
        ["systemarchitect"] = PositionLevel.Architect,
        ["itarchitect"] = PositionLevel.Architect,
        ["solutionarchitect"] = PositionLevel.Architect,
        ["solution_architect"] = PositionLevel.Architect,
        ["архитектор"] = PositionLevel.Architect,
        // Manager
        ["manager"] = PositionLevel.Manager,
        ["projectmanager"] = PositionLevel.Manager,
        ["pm"] = PositionLevel.Manager,
        ["cto"] = PositionLevel.Manager,
        ["head"] = PositionLevel.Manager,
        ["headofit"] = PositionLevel.Manager,
        ["руководитель"] = PositionLevel.Manager
    };


    private static readonly (Regex regex, PositionLevel level)[] _patterns =
    [
        (new Regex("^senior(_|[a-z])?", RegexOptions.Compiled), PositionLevel.Senior),
        (new Regex("^sr$", RegexOptions.Compiled), PositionLevel.Senior),
        (new Regex("^seno(i?)r$", RegexOptions.Compiled), PositionLevel.Senior),
        (new Regex("^mid(d?l?e?)", RegexOptions.Compiled), PositionLevel.Middle),
        (new Regex("^intermediate$", RegexOptions.Compiled), PositionLevel.Middle),
        (new Regex("^junior", RegexOptions.Compiled), PositionLevel.Junior),
        (new Regex("^jr$", RegexOptions.Compiled), PositionLevel.Junior),
        (new Regex("^джун", RegexOptions.Compiled), PositionLevel.Junior),
        (new Regex("(^|_)lead$", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("team[_]?lead", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("techlead", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("^tl(_senior)?$", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("^тимлид$", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("^техлид$", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("^ведущий.*", RegexOptions.Compiled), PositionLevel.Lead),
        (new Regex("[a-z]*architect$", RegexOptions.Compiled), PositionLevel.Architect),
        (new Regex("^архитектор$", RegexOptions.Compiled), PositionLevel.Architect),
        (new Regex("manager$", RegexOptions.Compiled), PositionLevel.Manager),
        (new Regex("projectmanager$", RegexOptions.Compiled), PositionLevel.Manager),
        (new Regex("^pm$", RegexOptions.Compiled), PositionLevel.Manager),
        (new Regex("^cto$", RegexOptions.Compiled), PositionLevel.Manager),
        (new Regex("^head(ofit)?$", RegexOptions.Compiled), PositionLevel.Manager),
        (new Regex("^руководитель$", RegexOptions.Compiled), PositionLevel.Manager)
    ];
}
