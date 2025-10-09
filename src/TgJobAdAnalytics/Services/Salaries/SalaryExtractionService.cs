using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Models.Salaries.Enums;
using TgJobAdAnalytics.Services.Levels;

namespace TgJobAdAnalytics.Services.Salaries;

/// <summary>
/// Extracts structured salary information from advertisement entities using an LLM (Chat API) with a constrained JSON schema
/// and enriches the result with a detected position level via <see cref="PositionLevelResolver"/>. Produces a fully populated
/// <see cref="SalaryEntity"/> when salary data is present; returns null otherwise.
/// </summary>
public sealed class SalaryExtractionService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SalaryExtractionService"/>.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create the service logger.</param>
    /// <param name="chatClient">OpenAI chat client used for structured extraction.</param>
    /// <param name="positionLevelResolver">Resolver for determining position level when salary is present.</param>
    public SalaryExtractionService(ILoggerFactory loggerFactory, ChatClient chatClient, PositionLevelResolver positionLevelResolver)
    {
        _chatClient = chatClient;
        _logger = loggerFactory.CreateLogger<SalaryExtractionService>();
        _positionLevelResolver = positionLevelResolver;
    }


    /// <summary>
    /// Extracts salary data and position level for the supplied advertisement.
    /// </summary>
    /// <param name="ad">Advertisement entity.</param>
    /// <param name="messageTags">Associated message tag strings used for position level heuristics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A populated <see cref="SalaryEntity"/> or null if no salary is detected.</returns>
    public async Task<SalaryEntity?> Process(AdEntity ad, List<string> messageTags, CancellationToken cancellationToken)
    {
        var salaryResponse = await ExtractSalary(ad, cancellationToken);
        if (salaryResponse is null)
            return null;

        var level = await _positionLevelResolver.Resolve(messageTags, ad.Text, cancellationToken);

        return new SalaryEntity
        {
            AdId = ad.Id,
            Date = ad.Date,
            Currency = salaryResponse.Value.Currency,
            LowerBound = salaryResponse.Value.LowerBound,
            UpperBound = salaryResponse.Value.UpperBound,
            Period = salaryResponse.Value.Period,
            Status = ProcessingStatus.Extracted,
            Level = level
        };
    }


    private async Task<ChatGptSalaryResponse?> ExtractSalary(AdEntity ad, CancellationToken cancellationToken)
    {
        try
        {
            var completion = await _chatClient.CompleteChatAsync([SystemPrompt, ad.Text], ChatOptions, cancellationToken);
            var raw = completion.Value.Content[0].Text;
            var response = JsonSerializer.Deserialize<ChatGptSalaryResponse>(raw, JsonSerializerOptions);

            Console.Write($"\rSalary extracted for ad {ad.Id}                                                ");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            _logger.LogError(ex, "Error extracting salary from ad {AdId}: {Message}", ad.Id, ex.Message);
        }

        return null;
    }


    private const string SystemPrompt = 
    """
    ## Salary Extraction Instructions

    Extract salary details from Russian job ads. Return results only in the specified JSON format.

    ## Steps
        
    1. **Presence**: If no explicit salary → `p:false` and other fields `null`.
    2. **Bounds**:
        * Two values → `lb` (lower), `ub` (upper).
        * One value → decide if it's lower (`от`) or upper (`до`).
        * If an exact amount is given without `от`/`до`, try to infer from context (e.g., "от" for lower). If uncertain, default to **upper bound**.
    3. **Period**:
        * **Project**: "за проект".
        * **Day**: "в день", "дневная ставка", "в сутки". Typical daily: $10–$100 / 1k–10k RUB.
        * **Month**: default. Typical monthly: $1k–$5k+ / 100k–350k+ RUB.
        * `$500` is **unlikely daily** → prefer month/project unless clearly daily.
    4. **Currency**:
        * RUB (руб, ₽), USD ($, долларов), EUR (€‚ евро).
        * `$` alone = USD unless contradicted.
    5. **Multiple/conflicts**: Choose the broadest relevant range; if tie, pick the first. If phrasing conflicts with plausibility, use the heuristics above.
        
    ## Preprocessing
        
    * Normalize numbers: remove thousand separators, convert `к/k/тыс.` → `000`, handle commas/points and dash variants in ranges.
    * Currency may appear before/after amount.
    * "на руки" = net, "гросс/до вычета" = gross (metadata only).
        
    ## Output Schema (short keys)
        
    Return **compact JSON** with these keys:
        
    * `p` → salary_present (true|false)
    * `lb` → lower_bound (integer|null)
    * `ub` → upper_bound (integer|null)
    * `prd` → period ("month"|"day"|"project"|null)
    * `cur` → currency ("RUB"|"USD"|"EUR"|null)

    ## Examples

    "от 2000 USD до 3000 USD в месяц" → `{"p":true,"lb":2000,"ub":3000,"prd":"month","cur":"USD"}`
    "до 150000 руб. за проект" → `{"p":true,"lb":null,"ub":150000,"prd":"project","cur":"RUB"}`
    "от 50 EUR в день" → `{"p":true,"lb":50,"ub":null,"prd":"day","cur":"EUR"}`
    "Обсуждается" → `{"p":false,"lb":null,"ub":null,"prd":null,"cur":null}`        
    """;

    private static readonly ChatCompletionOptions ChatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat
        (
            jsonSchemaFormatName: "SalaryExtraction",
            jsonSchemaFormatDescription: "Extract structured salary information from job ads.",
            jsonSchema: BinaryData.FromString(
                """
                {
                    "type": "object",
                    "additionalProperties": false,
                    "properties": {
                        "p":  { 
                            "type": "boolean",
                            "description": "Indicates if salary information is present in the ad."
                        },
                        "lb": { 
                            "type": ["integer", "null"],
                            "description": "The lower bound of the salary range, or null if not applicable."
                        },
                        "ub": { 
                            "type": ["integer", "null"],
                            "description": "The upper bound of the salary range, or null if not applicable."
                        },
                        "prd": {
                            "type": ["string", "null"],
                            "enum": ["month", "day", "project", null],
                            "description": "The payment period for the salary."
                        },
                        "cur": {
                            "type": ["string", "null"],
                            "enum": ["RUB", "USD", "EUR", null],
                            "description": "The currency of the salary."
                        }
                    },
                    "required": ["p", "lb", "ub", "prd", "cur"]
                }
                """)
        )
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    
    private readonly ChatClient _chatClient;
    private readonly ILogger<SalaryExtractionService> _logger;
    private readonly PositionLevelResolver _positionLevelResolver;
}