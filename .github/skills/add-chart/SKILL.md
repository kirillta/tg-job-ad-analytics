---
name: add-chart
description: 'Add or extend Chart.js graphs in HTML reports. Use when: adding new chart types, overlaying series, combining bar+line, adding dropdowns/selectors to charts, modifying chart data pipeline. Covers C# data prep, Scriban templates, JS rendering, localization.'
argument-hint: 'Describe the chart to add or modify'
---

# Adding Charts to Reports

## When to Use

- Adding a new chart or graph to the HTML report
- Adding overlay series (e.g. per-stack lines on a bar chart)
- Adding dropdown selectors to filter or switch chart data
- Combining chart types (bar + line) in one graph
- Modifying how data flows from C# to Chart.js

## Architecture Overview

Data flows through four layers:

```
C# Data (Report/ReportGroup)
  → C# HTML Builder (ChartBuilder, HtmlReportExporter)
    → Scriban Templates (Chart.sbn, embedded JSON)
      → JavaScript (charts.js, Chart.js library)
```

Each chart's data is **fully embedded as JSON** in the HTML — no server calls at runtime.

## Data Pipeline

### 1. Report Model (`Models/Reports/Report.cs`)

A `Report` has three data channels:

| Property | Purpose | Chart behavior |
|----------|---------|----------------|
| `Results` | Primary dataset (bars/line) | Always visible |
| `Variants` | Switchable alternative datasets | Dropdown-switched (one visible at a time) |
| `SeriesOverlays` | Always-present additional series | Rendered as extra datasets, can be toggled |

### 2. Chart Model (`Models/Reports/Html/ChartModel.cs`)

- `DataModel.Dataset` — primary dataset
- `DataModel.AdditionalDatasets` — overlay line series from `SeriesOverlays`
- `DatasetModel.TypeOverride` — enables mixed charts (e.g. `"line"` on a bar chart)
- `DatasetModel.Tension` — curve smoothing (`0.1` default, `0.4` for line charts)

### 3. ChartBuilder (`Services/Reports/Html/ChartBuilder.cs`)

- `Build(Report)` — builds the primary chart + additional datasets from `SeriesOverlays`
- `BuildData(label, results, tension)` — builds a single `DataModel` for variants
- Uses `_overlayBorderColors` / `_overlayBackgroundColors` for overlay series
- Uses `_backgroundColors` / `_borderColors` for primary datasets

### 4. HtmlReportExporter (`Services/Reports/Html/HtmlReportExporter.cs`)

- `BuildReportItem(Report)` — orchestrates chart + variant building
- Derives `tension` from `report.Type` before building variant `DataModel`s

## Procedure: Adding a New Chart

### Step 1 — Prepare Data (C#)

1. Add a method to the appropriate calculator (e.g. `AdStatsCalculator`, `SalaryStatisticsCalculator`)
2. Return a `Report` with:
   - `title`: localization key (e.g. `"report.ads.my_new_chart"`)
   - `results`: primary `Dictionary<string, double>`
   - `type`: `ChartType.Bar`, `ChartType.Line`, etc.
   - `variants`: (optional) switchable datasets for dropdown
   - `seriesOverlays`: (optional) always-visible line overlays
3. Include the report in the `ReportGroup` returned by `GenerateAll()`

### Step 2 — Add Localization

Add the title key to all three locale files:
- `src/TgJobAdAnalytics/Locales/en.json`
- `src/TgJobAdAnalytics/Locales/ru.json`
- `src/TgJobAdAnalytics/Locales/es.json`

### Step 3 — Template Layout

In `ReportGroupTemplate.sbn`, decide on layout:
- **Full-width**: Add the report code to the full-width conditional block
- **Grid (2-column)**: Exclude from the full-width block (default behavior)

### Step 4 — Verify

Build and inspect the generated HTML to confirm:
- Chart renders with correct type
- Dropdowns populate and switch data correctly
- Overlay series toggle as expected
- Tooltips show correct values
- Localization works across all three locales

## Procedure: Adding Overlay Series to an Existing Chart

1. Compute per-series data as `Dictionary<string, Dictionary<string, double>>`
2. Pass it as `seriesOverlays` in the `Report` constructor
3. `ChartBuilder` auto-generates `AdditionalDatasets` with `typeOverride: "line"` and `tension: 0.4`
4. The `Chart.sbn` template auto-renders an `overlay-stack-select` dropdown when `additional_datasets.size > 0`
5. The JS in `charts.js` hides overlays by default and toggles them via the dropdown

## Pitfalls and Gotchas

### CRITICAL: ReportGroupLocalizer Drops Data

`ReportGroupLocalizer.Localize()` reconstructs `ChartModel.DataModel` during localization. **Any new properties added to `DataModel` or `DatasetModel` must be explicitly passed through** in the localizer — they are NOT auto-preserved because `DataModel` and `DatasetModel` are `readonly record struct`s with explicit constructors.

**Symptoms**: Chart renders with no dropdown, missing overlay lines, default tension instead of custom value.

**Check**: After adding new properties, search `ReportGroupLocalizer.cs` for every `new ChartModel.DataModel(` and `new ChartModel.DatasetModel(` call and ensure all properties are forwarded.

### Variant Keys Must Match Localization Patterns

- `Variants` keys matching `PositionLevel` enum values (e.g. `"Junior"`, `"Senior"`) get localized via `LocalizeVariantKey()`
- `"All"` maps to `variant.all` localization key
- Other keys (e.g. stack names like `"dotnet"`) pass through unchanged
- The `variant-order` script in `Chart.sbn` only orders level-based variants — custom keys fall back to natural order

### Chart.sbn Conditional Rendering

The variant controls block renders when `report.variants != null`. The overlay controls block renders when `report.chart.data.additional_datasets.size > 0`. These are **independent conditions** — a chart can have one, both, or neither.

### Tension Consistency

When a chart uses `ChartType.Line`, tension must be set in **three places**:
1. `ChartBuilder.GetLineDataset()` — base chart
2. `ChartBuilder.BuildData()` — variant datasets (passed via `HtmlReportExporter`)
3. `ChartBuilder` overlay loop — additional datasets

Default `tension: 0.1` (subtle curve). Line charts use `0.4` (smoother).

### additionalDatasets JSON Array

The `additionalDatasets` array in `Chart.sbn` is always serialized (even when empty: `[]`). The JS handles empty arrays gracefully. `backgroundColor` and `borderColor` in overlay datasets are single-element arrays `[color]` — the JS unwraps them with `Array.isArray(x)?x[0]:x`.

### Mixed Chart Types

Chart.js supports per-dataset `type` override. Set `DatasetModel.TypeOverride = "line"` to render a line dataset inside a bar chart. The primary chart type comes from `data-chart-type` attribute on the container `<div>`.

### CRITICAL: Scriban Property Name Mapping (snake_case)

Scriban's `StandardMemberRenamer` converts PascalCase to snake_case, but **consecutive uppercase letters are NOT separated by underscores**. The rule: an underscore is inserted before an uppercase char only if `i > 0` AND the previous char was lowercase.

| C# Property | Scriban Name | NOT this |
|---|---|---|
| `TypeOverride` | `type_override` | — |
| `BackgroundColor` | `background_color` | — |
| `YAxisId` | `yaxis_id` | ~~`y_axis_id`~~ |
| `IDToken` | `idtoken` | ~~`id_token`~~ |

**Always verify** the Scriban name by tracing through the renamer logic character-by-character, or by inspecting the generated HTML output after a test run. A wrong property name silently resolves to `null` (due to `EnableRelaxedMemberAccess = true`) with no error.

### Y-Axis Scaling — Dual Axes for Overlays

When overlay series have much smaller values than the primary dataset (e.g. per-stack counts vs total counts), use a **second Y-axis** (`y1`) on the right side. Set `DatasetModel.YAxisId = "y1"` in `ChartBuilder`, serialize as `"yAxisID"` in `Chart.sbn`, and configure the `y1` scale in `charts.js`.

Key JS considerations:
- Initialize `y1.suggestedMax` to a small value (e.g. `1`) since all overlays start hidden
- On dropdown change, recompute `y1.max` using only the **selected** stack's data (not all stacks combined)
- Use hard `max` (not `suggestedMax`) in the change handler — Chart.js may ignore `suggestedMax` when hidden dataset data is still present
- When switching back to "All Stacks" (no overlay), reset: `y1.max = undefined; y1.suggestedMax = 1`
