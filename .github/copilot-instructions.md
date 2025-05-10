# Project Overview

TgJobAdAnalytics is an analytics service for analyzing job advertisements from Telegram channels. The application extracts and processes job postings, detects salary information using pattern matching, normalizes currencies, and generates statistical reports. It identifies similar messages using locality-sensitive hashing to avoid duplicates and provides insights on salary trends over time.

## Technologies
- ASP.NET Core for the application framework
- Entity Framework Core with SQLite for data storage
- MathNet.Numerics for statistical operations
- Scriban for HTML templating
- Regex pattern matching for salary detection
- Xunit and NSubstitute for testing
- Parallel processing for performance optimization
- Mathematical models for outlier detection and similarity analysis

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