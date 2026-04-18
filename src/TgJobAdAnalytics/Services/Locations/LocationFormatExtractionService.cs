using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;
using TgJobAdAnalytics.Models.Locations;
using TgJobAdAnalytics.Models.Locations.Enums;

namespace TgJobAdAnalytics.Services.Locations;

/// <summary>
/// Extracts employer location and work format from job advertisement text using a single LLM call
/// with a constrained JSON schema. Returns <see cref="VacancyLocation.Unknown"/> and <see cref="WorkFormat.Unknown"/>
/// on error or when no relevant information is present.
/// </summary>
public sealed class LocationFormatExtractionService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationFormatExtractionService"/> class.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create a logger for this service.</param>
    /// <param name="chatClient">OpenAI chat client used for structured extraction.</param>
    public LocationFormatExtractionService(ILoggerFactory loggerFactory, ChatClient chatClient)
    {
        _logger = loggerFactory.CreateLogger<LocationFormatExtractionService>();
        _chatClient = chatClient;
    }


    /// <summary>
    /// Extracts vacancy location and work format from the supplied advertisement text.
    /// </summary>
    /// <param name="adText">Full advertisement text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of detected <see cref="VacancyLocation"/> and <see cref="WorkFormat"/>.</returns>
    public async Task<(VacancyLocation Location, WorkFormat Format)> Process(string adText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adText))
            return (VacancyLocation.Unknown, WorkFormat.Unknown);

        return await Extract(adText, cancellationToken);
    }


    private async Task<(VacancyLocation, WorkFormat)> Extract(string text, CancellationToken cancellationToken)
    {
        try
        {
            var completion = await _chatClient.CompleteChatAsync([SystemPrompt, text], ChatOptions, cancellationToken);
            var raw = completion.Value.Content[0].Text;
            var response = JsonSerializer.Deserialize<ChatGptLocationFormatResponse>(raw);

            Console.Write($"\rLocation/format extracted: {response.Location}/{response.Format}                                                   ");

            return (response.Location, response.Format);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            _logger.LogError(ex, "Error extracting location and work format.");
        }

        return (VacancyLocation.Unknown, WorkFormat.Unknown);
    }


    private const string SystemPrompt =
    """
    ## Location and Work Format Extraction Instructions

    Extract two fields from Russian job advertisements and return them in the specified JSON format.

    ## Field 1: Vacancy Location (`loc`)

    Classify the **employer's or office's** geographic location. Use WHERE THE JOB IS, not where the candidate can live.

    | Value | Meaning | Examples |
    |-------|---------|---------|
    | 0 | Unknown | No location information |
    | 1 | Russia | Москва, Санкт-Петербург, Россия, РФ, Новосибирск, Екатеринбург, any Russian city |
    | 2 | Belarus | Минск, Беларусь, РБ, Брест, Гродно, any Belarusian city |
    | 3 | CIS | Казахстан, Узбекистан, Кыргызстан, Армения, Грузия, Азербайджан, Таджикистан, Молдова, Алматы |
    | 4 | Europe | Германия, Нидерланды, Польша, Чехия, Австрия, Франция, UK, Великобритания, Берлин, Амстердам |
    | 5 | US | США, USA, United States, Нью-Йорк, Сан-Франциско, any US city |
    | 6 | Middle East | ОАЭ, Дубай, Израиль, Турция, Саудовская Аравия, Абу-Даби, Тель-Авив |
    | 7 | Other | Global/international companies with no named office location, "worldwide" teams |

    **Rules:**
    - "Remote from Russia" → loc = 1 (employer is in Russia).
    - "Remote worldwide, no office" → loc = 7.
    - If multiple locations, choose the primary employer office.

    ## Field 2: Work Format (`fmt`)

    Classify the required work arrangement.

    | Value | Meaning | Examples |
    |-------|---------|---------|
    | 0 | Unknown | No work format information |
    | 1 | OnSite | Офис, office, on-site, requires physical presence |
    | 2 | Hybrid | Гибрид, hybrid, частично офис + удалённо |
    | 3 | RemoteDomestic | Удалённо с ограничением по стране/региону ("удалёнка, только Россия/СНГ") |
    | 4 | RemoteWorldwide | Полная удалёнка без ограничений ("удалёнка", "remote", "fully remote", "from anywhere") |

    **Rules:**
    - "Удалёнка" without a geographic restriction → 4 (RemoteWorldwide).
    - "Удалёнка, только РФ/СНГ" → 3 (RemoteDomestic).
    - Office with 1–2 days remote per week → 2 (Hybrid).
    - When unclear between remote types, default to 4 (RemoteWorldwide).

    ## Output Schema

    Return compact JSON with exactly two integer fields:
    - `loc` → integer (0–7)
    - `fmt` → integer (0–4)

    ## Examples

    "Разработчик, офис в Москве" → `{"loc":1,"fmt":1}`
    "Senior developer, fully remote, Berlin-based company" → `{"loc":4,"fmt":4}`
    "Ищем разработчика, удалёнка, только РФ" → `{"loc":1,"fmt":3}`
    "Backend engineer, remote-first, Tel Aviv HQ" → `{"loc":6,"fmt":4}`
    "Full remote, global team, no office" → `{"loc":7,"fmt":4}`
    "Гибрид, Минск" → `{"loc":2,"fmt":2}`
    """;


    private static readonly ChatCompletionOptions ChatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "LocationFormatExtraction",
            jsonSchemaFormatDescription: "Extract vacancy location and work format from job ads.",
            jsonSchema: BinaryData.FromString(
            """
            {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                    "loc": {
                        "type": "integer",
                        "enum": [0, 1, 2, 3, 4, 5, 6, 7],
                        "description": "Vacancy location: 0=Unknown, 1=Russia, 2=Belarus, 3=CIS, 4=Europe, 5=US, 6=MiddleEast, 7=Other"
                    },
                    "fmt": {
                        "type": "integer",
                        "enum": [0, 1, 2, 3, 4],
                        "description": "Work format: 0=Unknown, 1=OnSite, 2=Hybrid, 3=RemoteDomestic, 4=RemoteWorldwide"
                    }
                },
                "required": ["loc", "fmt"]
            }
            """))
    };


    private readonly ChatClient _chatClient;
    private readonly ILogger<LocationFormatExtractionService> _logger;
}
