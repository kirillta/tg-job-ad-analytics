using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;
using TgJobAdAnalytics.Models.Levels;

namespace TgJobAdAnalytics.Services.Levels;

public sealed class PositionLevelExtractionService
{
    public PositionLevelExtractionService(ILoggerFactory loggerFactory, ChatClient chatClient)
    {
        _logger = loggerFactory.CreateLogger<PositionLevelExtractionService>();
        
        _chatClient = chatClient;
    }


    public async Task<PositionLevel> Process(string adText, CancellationToken cancellationToken)
    { 
        if (string.IsNullOrWhiteSpace(adText))
            return PositionLevel.Unknown;

        var response = await ExtractPositionLevel(adText, cancellationToken);
        return response.Level;
    }


    private async Task<ChatGptPositionLevelResponse> ExtractPositionLevel(string text, CancellationToken cancellationToken)
    { 
        try 
        {
            var completion = await _chatClient.CompleteChatAsync([SystemPrompt, text], ChatOptions, cancellationToken);
            var raw = completion.Value.Content[0].Text;
            var response = JsonSerializer.Deserialize<ChatGptPositionLevelResponse>(raw);

            Console.Write($"\rPosition level extracted: {response.Level}                ");

            return response;
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error extracting position level from text: {Text}", text);
        }

        return ChatGptPositionLevelResponse.Empty;
    }


    private const string SystemPrompt =
    """
    ## Position Level Extraction Instructions

    ## Levels

    Map mentions in job ads to one of these levels:
    0 → Unknown
    1 → Intern (стажёр, trainee, internship, практикант)
    2 → Junior (джуниор, младший, начинающий)
    3 → Middle (мидл, middle, опытный, самостоятельный)
    4 → Senior (сеньор, senior, ведущий разработчик)
    5 → Lead (тимлид, lead, руководитель разработки, head of dev)
    6 → Architect (архитектор, architect, solution architect, system architect)
    7 → Manager (менеджер, руководитель, project manager, product manager, engineering manager)

    ## Rules

    * If multiple levels are mentioned (e.g., Middle/Senior), pick the highest.
    * If ambiguous, prefer more specific signals (e.g., “Team Lead” → Lead, not Senior).
    * If no clear level found, return 0 (Unknown).

    ## Output Schema (short keys)

    Always return compact JSON with one key:

    lvl → integer (0–7)

    Return **compact JSON** with these keys:
    
    * `pl` → position level (0|1|2|3|4|5|6|7)
    
    ## Examples
    
    "Senior C# Developer, remote" → `{"pl":4}`
    """;

    private static readonly ChatCompletionOptions ChatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat
        (
            jsonSchemaFormatName: "PositionLevelExtraction",
            jsonSchemaFormatDescription: "Extract structured position level information from job ads.",
            jsonSchema: BinaryData.FromString(
            """
            {
                "type": "object",
                "additionalProperties": false,
                "properties": {
                    "pl":  { 
                        "type": "integer",
                        "enum": [0,1,2,3,4,5,6,7],
                        "description": "Position level: 0=Unknown, 1=Intern, 2=Junior, 3=Middle, 4=Senior, 5=Lead, 6=Architect, 7=Manager"
                    }
                },
                "required": ["pl"]
            }
            """)
        )
    };


    private readonly ChatClient _chatClient;
    private readonly ILogger<PositionLevelExtractionService> _logger;
}
