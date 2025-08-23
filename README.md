# TgJobAdAnalytics

## Calculation Methodology
1.	**Ingestion:** Messages are collected from monitored Telegram chats and persisted with their original UTC timestamps.
2.	**Extraction:** Each message is parsed; job ads are identified and salary mentions extracted via pattern matching (lower/upper bounds, currency, and period).
3.	**Normalization:** Currencies and periods are normalized to a common base; numeric ranges are stored (lower/upper) together with a normalized midpoint when needed.
4.	**De‑duplication:** Similar ads are detected (locality-sensitive hashing / text similarity) and flagged so unique-ad statistics exclude repeats.
5.	**Validation & Filtering:** Invalid or ambiguous salary parses are discarded; statistics exclude incomplete current-month data.
6.	**Aggregation:** Metrics (counts, unique counts, salary distributions, averages, etc.) are computed per grouping interval and per source.
7.	**Reporting:** Values are formatted and charts rendered. The reporting window ends on the last day of the previous month to avoid partial intervals.