using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Data.Messages;
using TgJobAdAnalytics.Data.Salaries;
using TgJobAdAnalytics.Models.Salaries;
using TgJobAdAnalytics.Services.Levels;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class SalaryExtractionService
{
    public SalaryExtractionService(ILoggerFactory loggerFactory, ApplicationDbContext dbContext, ChatClient chatClient)
    {
        _logger = loggerFactory.CreateLogger<SalaryExtractionService>();

        _chatClient = chatClient;
        _dbContext = dbContext;
    }


    public async Task<SalaryEntity?> Process(AdEntity ad)
    {
        var salaryResponse = await ExtractSalary(ad);
        if (salaryResponse is null)
            return null;

        var entry = await BuildEntity(salaryResponse.Value, ad);

        _dbContext.Salaries.Add(entry);
        await _dbContext.SaveChangesAsync();

        return entry;
    }


    private async Task<SalaryEntity> BuildEntity(ChatGptSalaryResponse salaryResponse, AdEntity ad) 
    {
        var tags = await GetMessageTags(ad.MessageId);
        var level = PositionLevelResolver.Resolve(tags);

        return new()
        {
            AdId = ad.Id,
            Date = ad.Date,
            Currency = salaryResponse.Currency,
            LowerBound = salaryResponse.LowerBound,
            UpperBound = salaryResponse.UpperBound,
            Period = salaryResponse.Period,
            Status = ProcessingStatus.Extracted,
            Level = level
        };
    }


    private async Task<List<string>> GetMessageTags(Guid messageId)
    {
        var message = await _dbContext.Messages.FindAsync(messageId);
        return message?.Tags ?? [];
    }


    private async Task<ChatGptSalaryResponse?> ExtractSalary(AdEntity ad)
    {
        try
        {
            var completion = await _chatClient.CompleteChatAsync([SystemPrompt, ad.Text], ChatOptions);
            return JsonSerializer.Deserialize<ChatGptSalaryResponse?>(completion.Value.Content[0].Text, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
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
           * **Day**: "в день", "дневная ставка", "в сутки". Typical daily: \$10–\$100 / 1k–10k RUB.
           * **Month**: default. Typical monthly: \$1k–\$5k+ / 100k–350k+ RUB.
           * `$500` is **unlikely daily** → prefer month/project unless clearly daily.
        4. **Currency**:
           * RUB (руб, ₽), USD (\$, долларов), EUR (€‚ евро).
           * `$` alone = USD unless contradicted.
        5. **Multiple/conflicts**: Choose the broadest relevant range; if tie, pick the first. If phrasing conflicts with plausibility, use the heuristics above.
        
        ## Preprocessing
        
        * Normalize numbers: remove thousand separators, convert `к/k/тыс.` → `000`, handle commas/points and dash variants in ranges.
        * Currency may appear before/after amount.
        * "на руки" = net, "гросс/до вычета" = gross (metadata only).
        
        ## Output Schema (short keys)
        
        Return **compact JSON** with these keys:
        
        * `p` → salary\_present (true|false)
        * `lb` → lower\_bound (integer|null)
        * `ub` → upper\_bound (integer|null)
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
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SalaryExtractionService> _logger;
}