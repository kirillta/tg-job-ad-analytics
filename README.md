# TgJobAdAnalytics

Analytics and reporting for job advertisements collected from Telegram channels. It ingests exported Telegram chat JSON, extracts job ads, parses salaries (multi‑currency, ranges, period normalization), deduplicates similar ads, classifies position level & tech stack, and generates static HTML reports (multi‑locale) with salary statistics and distributions.

## Key Features
- Salary extraction & normalization (range, currency, period → unified monthly USD equivalent)
- Historical FX rate backfilling (local CSV cache + remote source)
- Duplicate / near duplicate detection (LSH + MinHash)
- Position level inference (Junior/Middle/Senior, etc.)
- Technology stack mapping (channel → stack; optional backfill pipelines)
- Adaptive OpenAI powered augmentation (salary / level refinement)
- Monthly & yearly aggregated statistics + per‑stack comparisons
- Deterministic IDs & vector store for similarity experiments

## High Level Flow
1. Import Telegram chat export JSON files (messages + ads) from `sources/`.
2. Persist raw chats/messages/ads into SQLite (migrations applied automatically).
3. Extract & normalize salaries; enrich with position level & stack info.
4. Run optional pipelines (deduplication, vector init, backfills, etc.).
5. Generate HTML report(s) into `output/<run-timestamp>/<locale>/index.html`.
6. (Optional) Copy latest report into distributable `dist/` folder.

## Project Layout (essential parts)
- `src/TgJobAdAnalytics` – application
  - `Data` – EF Core entities & context (SQLite)
  - `Services/Uploads` – Telegram JSON ingestion
  - `Services/Salaries` – extraction, normalization, FX rates
  - `Services/Messages` – similarity & hashing
  - `Services/Levels` – position level logic
  - `Services/Stacks` – channel → stack mapping & pipelines
  - `Services/Reports` – statistics + HTML generation (Scriban)
  - `Services/Pipelines` – optional post‑processing steps
  - `Utils/HostHelper.cs` – host & service registration
- `tests/Tests` – test project

## Prerequisites
- .NET 9 SDK
- OpenAI API key if using AI enhancements
- SQLite (no manual setup needed – file DB created automatically)

## Configuration (appsettings.json excerpts)
```json
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=analytics.db" },
  "Upload": {
    "Mode": "Append",   // Skip | Append | Clean
    "BatchSize": 10000
  },
  "SiteMetadata": {
    "BaseUrl": "https://example.com/",
    "SiteName": "Job Ad Analytics",
    "PrimaryLocale": "en",
    "Locales": ["en"],
    "JsonLdType": "WebSite",
    "LocalizationPath": "config/localization",
    "DefaultOgImagePath": "img/og.png"
  }
}
```
Additional automatically resolved operational folders (created relative to solution root on first run):
- `sources/` – place Telegram export JSON files here
- `config/rates/rates.csv` – FX rate cache (auto created / appended)
- `config/stacks/channel-stacks.json` – channel → stack mapping file
- `output/` – run outputs (timestamped)
- `dist/` – final distributable (via script)

## Telegram Export Input
Export chats (JSON) via Telegram desktop. Place all exported JSON files (one folder per channel or combined) inside the `sources/` directory before running. Existing data handling depends on `Upload:Mode`:
- `Skip` – do not import
- `Append` – import only unseen messages
- `Clean` – purge message/ad tables then import

## Running
From solution root:
```
dotnet run --project src/TgJobAdAnalytics
```
Passing pipeline names (optional) executes only those additional steps after import & salary extraction:
```
dotnet run --project src/TgJobAdAnalytics -- DistinctAdsPipeline InitVectorsPipeline
```
If no pipeline names are supplied only import + salary/level extraction + report generation are performed.

### Available Pipelines (names to pass as args)
- `DistinctAdsPipeline` – mark near-duplicate ads
- `InitVectorsPipeline` – build vectors for similarity
- `DeterministicIdMigrationPipeline` – (one‑off) ensure fixed IDs
- `AssignDotnetStackToChatsPipeline` – classify .NET related chats
- `SalaryLevelUpdatePipeline` – recompute position levels

You can chain any number; they run sequentially in provided order.

## FX Rates
A local CSV cache (`config/rates/rates.csv`) is maintained. Missing day ranges are filled by extrapolating nearest prior available day when necessary.

## Reports
Generated to `output/<UTC-timestamp>/[locale]/index.html` plus assets. Each run creates a new timestamped folder; older runs are preserved.

## Create Distribution (dist/)
A helper script copies the latest run’s locale folders into a clean `dist/` directory.
PowerShell (Windows / cross‑platform with PowerShell 7):
```
powershell -NoProfile -File ./dist.ps1 -OutputRoot ./output -DistRoot ./dist
```
Parameters (optional):
```
-OutputRoot ./output -DistRoot ./dist
```
Result: `dist/<locale>/index.html` ready for static hosting (GitHub Pages, Netlify, etc.).

## Environment Variables
- `PNKL_OPEN_AI_KEY` – OpenAI API key (omit to disable related enhancements)

## Tests
```
dotnet test
```

## Build Release
```
dotnet publish src/TgJobAdAnalytics -c Release -o publish
```
Binary will perform migrations and process data the same way as `dotnet run` (pass pipeline names as arguments).

## License
MIT – see `LICENSE.txt`.

---
**Built with ❤️ using .NET 9**