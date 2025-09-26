# TgJobAdAnalytics

## Calculation Methodology
1.	**Ingestion:** Messages are collected from monitored Telegram chats and persisted with their original UTC timestamps.
2.	**Extraction:** Each message is parsed; job ads are identified and salary mentions extracted via pattern matching (lower/upper bounds, currency, and period).
3.	**Normalization:** Currencies and periods are normalized to a common base; numeric ranges are stored (lower/upper) together with a normalized midpoint when needed.
4.	**De‑duplication:** Similar ads are detected (locality-sensitive hashing / text similarity) and flagged so unique-ad statistics exclude repeats.
5.	**Validation & Filtering:** Invalid or ambiguous salary parses are discarded; statistics exclude incomplete current-month data.
6.	**Aggregation:** Metrics (counts, unique counts, salary distributions, averages, etc.) are computed per grouping interval and per source.
7.	**Reporting:** Values are formatted and charts rendered. The reporting window ends on the last day of the previous month to avoid partial intervals.


## Copy latest report to `dist`

A PowerShell script is provided to copy the latest generated report (all locales) into a deployable `dist` folder at the solution root.

What it does
- Finds the newest run under `Output/<runFolder>` (prefers `yyyyMMdd-HHmmssZ` naming; falls back to latest by write time).
- Copies every locale’s `index.html` into `dist/<locale>/index.html`.
- Creates `dist` if missing and clears old contents (preserves `.gitkeep` if present).

Run from the solution root
- Windows PowerShell:
  ```ps
  powershell -NoProfile -ExecutionPolicy Bypass -File .\dist.ps1
  ```
- PowerShell 7 (cross-platform):
  ```ps
  pwsh -NoProfile -File ./dist.ps1
  ```

Parameters
- -OutputRoot: path to the output folder (default: ./Output)
- -DistRoot: path to the dist folder (default: ./dist)

Examples
  ```ps
  powershell -NoProfile -File ./dist.ps1 -OutputRoot ./src/TgJobAdAnalytics/Output -DistRoot ./dist
  ```

Visual Studio (optional)
- Tools > External Tools > Add:
  - Title: Copy Latest Report
  - Command: powershell.exe
  - Arguments: -NoProfile -ExecutionPolicy Bypass -File $(SolutionDir)dist.ps1
  - Initial directory: $(SolutionDir)

Notes
- The script always copies all locales found in the latest run.
- Exits 0 on success; non-zero otherwise.