# Project Overview

TgJobAdAnalytics is an analytics service for analyzing job advertisements from Telegram channels. The application imports and processes job postings, extracts salary information and position levels using OpenAI structured outputs (with regex-based fallbacks), normalizes currencies, and generates multi-locale statistical reports. It identifies similar messages using locality-sensitive hashing to avoid duplicates, compares salary trends across technology stacks, and provides insights on salary distributions over time.

## Technologies
- .NET 9.0 console application with Microsoft.Extensions.Hosting (Generic Host)
- Entity Framework Core with SQLite for data storage
- OpenAI API for structured salary extraction and position level classification
- MathNet.Numerics for statistical operations
- Scriban for multi-locale HTML report templating
- Regex pattern matching for position level resolution
- Xunit and NSubstitute for testing
- Parallel processing and adaptive rate limiting for performance optimization
- Locality-sensitive hashing (MinHash) for duplicate detection and similarity analysis
- Pipeline-based architecture for pluggable data processing steps

# Code Style

- Take line lenght as 160 chars.
- Use two blank lines between constructors, methods, delegates.
- Use one blank line between properties.
- Use no blank lines between private fields.
- Place private fields at the end of the file.
- Use named parameters for clarity, especially when creating new objects(e.g., `id: Guid.NewGuid()`).
- Ensure correct parameter names are used.
- Ensure unused usings are removed.
- Always use `DateTime.UtcNow` instead of `DateTime.Now`.


# Code Annotations And Comments

- Use XML documentation.
- Add XML documentation to classes, using <inheritdoc/> for interface implementation methods and standard XML documentation for the class itself and any members not defined in the interface.
- Only add comments to public members, not private ones.
- Do not comment tests and test classes.
- Use comments in code only for complex behavior.