using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TgJobAdAnalytics.Data;
using TgJobAdAnalytics.Models.Salaries;

namespace TgJobAdAnalytics.Services.Salaries;

public sealed class AdProcessor
{
    public AdProcessor(ApplicationDbContext dbContext, IOptions<ParallelOptions> parallelOptions, ChatClient chatClient/*, SalaryServiceFactory salaryServiceFactory*/)
    {
        _chatClient = chatClient;
        _dbContext = dbContext;
        _parallelOptions = parallelOptions.Value;
        //_salaryServiceFactory = salaryServiceFactory;
    }


    public async Task Process()
    {
        var ads = await _dbContext.Ads
            .AsNoTracking()
            //.Where(ad => ad.Salary == null || ad.Salary.Currency == null)
            .Select(x => x)
            .ToListAsync();

        if (ads.Count == 0)
            return;

        var message = ads.First().Text;

        //var completion = await _chatClient.CompleteChatAsync([SystemPrompt, message], _chatOptions);
        var json = """{"salary_present":true,"lower_bound":7000,"upper_bound":7500,"period":"month","currency":"USD"}""";// completion.Value.Content[0].Text;

        
        var salaryResponse = System.Text.Json.JsonSerializer.Deserialize<ChatGptSalaryResponse>(json, _jsonSerializerOptions);

    }


    private const string SystemPrompt = 
    """
        ## Instructions for Salary Extraction from Job Ads

        ### Overview

        Extract structured salary information from free-text job ads, mainly written in Russian. Job ads may or may not include salary details.

        ### Preprocessing & Normalization

        * Normalize numerals: handle spaces/non‑breaking spaces as thousand separators (e.g., `200 000` → `200000`), commas/points (`150,0 тыс.` → `150000`), dashes (`–`, `—`, `-`) in ranges, and shorthand (`к`, `k`, `тыс.` → `000`).
        * Accept currency symbols before or after the amount (e.g., `$2000`, `2000 USD`, `2000$`, `2000 дол.`).
        * Treat words like **«на руки»/«чистыми»** as **net**; **«gross/гросс/до вычета»** as **gross**. (This does not change the output schema but can be used to resolve conflicts.)

        ### Extraction Criteria:

        1. **Salary Presence**:

            * Determine if the salary is explicitly mentioned.
            * Mark clearly if no salary information is present.

        2. **Salary Bounds**:

            * Extract both lower and upper bounds if provided (e.g., "от 100,000 до 150,000 руб.").
            * If only one value is provided, determine if it represents:

                * **Lower bound** (e.g., "от 100,000 руб.")
                * **Upper bound** (e.g., "до 150,000 руб.")
                * **Exact value** (if explicitly clear, e.g., "фиксировано 120,000 руб.")

        3. **Payment Period**:

            * Determine if the salary is specified per:

                * **Project** (e.g., "за проект")
                * **Day** (e.g., "в день", "дневная ставка", "в сутки")
                * **Month** (default assumption if not specified explicitly; look for "в месяц", "в мес", "месячная")
            * **Heuristics (sanity checks):**

                * Daily wages typically fall in the range **\$10–\$100** (or equivalents). For RUB, typical daily ranges are **1,000–10,000 RUB**.
                * Monthly wages typically fall in the range **\$1,000–\$5,000+** (or equivalents). For RUB, typical monthly ranges are **100,000–350,000+ RUB**.
                * Values around **\$500** are **unlikely daily**; prefer **month** or **project** unless the ad clearly states daily.

        4. **Currency**:

            * Identify currency explicitly stated in the ad. Currency is almost always one of:

                * **RUB** (руб, рублей, ₽)
                * **USD** (\$, долларов, USD)
                * **EUR** (€‚ евро, EUR)
            * If the symbol **\$** appears without a word, assume **USD** unless the ad elsewhere clearly indicates another currency.

        5. **Multiple Mentions / Conflicts**:

            * If multiple salary figures exist (e.g., for different grades), choose the **broadest clearly stated range** relevant to the role; if equal, choose the **first** explicit range in the ad.
            * If period and amount conflict (e.g., daily vs monthly plausibility), use the **Heuristics** above and the strongest explicit phrase ("в месяц", "в день", "за проект").

        ### Explicit Output Format (JSON):

        Always strictly return results in the following JSON structure. All fields should be present and filled as specified:

        ```json
        {
            "salary_present": true|false,
            "lower_bound": integer|null,
            "upper_bound": integer|null,
            "period": "month"|"day"|"project"|null,
            "currency": "RUB"|"USD"|"EUR"|null
        }
        ```

        * Use `null` explicitly if a field is not applicable or not mentioned.

        ### Example Cases:

        * **Case 1**: "Зарплата от 2000 USD до 3000 USD в месяц"

        ```json
        {
            "salary_present": true,
            "lower_bound": 2000,
            "upper_bound": 3000,
            "period": "month",
            "currency": "USD"
        }
        ```

        * **Case 2**: "Оплата до 150,000 руб. за проект"

        ```json
        {
            "salary_present": true,
            "lower_bound": null,
            "upper_bound": 150000,
            "period": "project",
            "currency": "RUB"
        }
        ```

        * **Case 3**: "Ставка от 50 EUR в день"

        ```json
        {
            "salary_present": true,
            "lower_bound": 50,
            "upper_bound": null,
            "period": "day",
            "currency": "EUR"
        }
        ```

        * **Case 4**: "Обсуждается на собеседовании"

        ```json
        {
            "salary_present": false,
            "lower_bound": null,
            "upper_bound": null,
            "period": null,
            "currency": null
        }
        ```
    """;


    private static readonly ChatCompletionOptions _chatOptions = new()
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat
        (
            jsonSchemaFormatName: "SalaryExtraction",
            jsonSchemaFormatDescription: "Extract structured salary information from job ads.",
            jsonSchema: BinaryData.FromString(
                """
                {
                    "type": "object",
                    "properties": {
                        "salary_present": {
                            "type": "boolean",
                            "description": "Indicates if salary information is present in the ad."
                        },
                        "lower_bound": {
                            "type": ["integer", "null"],
                            "description": "The lower bound of the salary range, or null if not applicable."
                        },
                        "upper_bound": {
                            "type": ["integer", "null"],
                            "description": "The upper bound of the salary range, or null if not applicable."
                        },
                        "period": {
                            "type": ["string", "null"],
                            "enum": ["Month", "Day", "Project", null],
                            "description": "The payment period for the salary."
                        },
                        "currency": {
                            "type": ["string", "null"],
                            "enum": ["RUB", "USD", "EUR", null],
                            "description": "The currency of the salary."
                        }
                    },
                    "required": ["salary_present", "lower_bound", "upper_bound", "period", "currency"]
                }
                """)
        )
    };

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly ChatClient _chatClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly ParallelOptions _parallelOptions;
    private readonly SalaryServiceFactory _salaryServiceFactory;
}
