namespace TgJobAdAnalytics.Models.Locations.Enums;

/// <summary>
/// Represents the geographic location of the employer or job office.
/// Classified by the employer's physical presence, not the candidate's location or remote eligibility.
/// </summary>
public enum VacancyLocation
{
    Unknown = 0,
    Russia = 1,
    Belarus = 2,
    Cis = 3,
    Europe = 4,
    Us = 5,
    MiddleEast = 6,
    Other = 7
}
