# Import Pipeline Improvement Plan

## Scope

Services analyzed:
- `TelegramChatImportService`
- `TelegramAdPersistenceService`
- `TelegramMessagePersistenceService`
- `TelegramChatPersistenceService`
- `SimilarityCalculator`

---

## Memory Improvements (Primary)

### 1. Stream JSON deserialization in `ReadChatFromFile`

**Current behavior:** Allocates a `byte[json.Length]` buffer and deserializes the entire `TgChat` (including all `TgMessage` objects) into memory at once — double allocation (raw bytes + object graph).

**Proposal:** Use `JsonSerializer.DeserializeAsync` directly on the `FileStream` to eliminate the intermediate byte buffer. Consider `JsonSerializer.DeserializeAsyncEnumerable` on messages if the model is restructured for incremental processing.

**Dedup safety:** ✅ Safe — dedup operates on persisted `AdEntity` rows, not raw JSON.

---

### 2. Discard `TgChat.Messages` after message persistence

**Current behavior:** The full `List<TgMessage>` lives for the duration of `Process()` — through message, ad, and chat persistence — even though `TelegramAdPersistenceService` re-reads messages from the DB.

**Proposal:** Pass `(long Id, string Name)` downstream instead of the full `TgChat`, or restructure to allow discarding the message list after `TelegramMessagePersistenceService.Upsert` returns.

**Dedup safety:** ✅ Safe in principle. Requires model refactor — `TgChat` is a `readonly record struct`, so the `Messages` property cannot be nulled out directly.

---

### 3. Paginated DB reads in `AddAll` / `AddOnlyNew`

**Current behavior:** Both methods materialize the entire message set from the DB into a `List<MessageEntity>` (including `TextEntries` dictionaries and `Tags`).

**Proposal:** Use keyset pagination (`Skip`/`Take` or cursor-based) to process messages in bounded batches instead of loading all at once.

**Dedup safety:** ✅ Safe — same set of `AdEntity` objects is produced and persisted regardless of fetch strategy.

---

### 4. Remove `ConcurrentBag<AdEntity>` intermediate

**Current behavior:** `ProcessAndInsert` accumulates all `AdEntity` objects into a `ConcurrentBag`, then copies to a `List` — briefly doubling collection memory.

**Proposal:** Partition messages into chunks before the parallel section and produce `List<AdEntity>` directly per chunk, merging without the bag intermediate.

**Dedup safety:** ✅ Safe — pure collection refactor; same entities reach the batch loop.

---

### 5. Push `AddOnlyNew` diff logic into SQL

**Current behavior:** `AddOnlyNew` materializes three `HashSet<Guid>` collections simultaneously (`existingMessageIds`, `existingAdMessageIds`, `diff`). For millions of messages this is significant (each `Guid` = 16 bytes + set overhead).

**Proposal:** Replace the three-set in-memory diff with a single SQL query using `LEFT JOIN` / `WHERE NOT EXISTS` so only the needed message IDs are returned.

**Dedup safety:** ✅ Safe — only changes which messages are fetched, not how they are processed.

---

### 6. Lightweight deduplication — load IDs and signatures instead of full ad text

**Current behavior:** `DeduplicateAds` calls `_similarityCalculator.Distinct(ads)` where `ads` is loaded with `ToListAsync()` on the full ads table including the `Text` column — the single largest memory spike in the pipeline.

**Proposal:** Switch `DeduplicateAds` to call `DistinctPersistent`, which already exists and queries `VectorIndex` + `VectorStore` using pre-stored signatures without needing `ad.Text`. Load only `Id`, `Date`, and audit fields for the ad list passed in.

**Dedup safety:** ⚠️ Safe with design choice. `DistinctPersistent` uses stored signatures and respects vectorization config versioning. It already handles the fallback to `Distinct` when persistence services are unavailable. Since `DeduplicateAds` runs immediately after import in the same session, config drift is not a concern.

---

### 7. Pool `StringBuilder` in `GetText` local function

**Current behavior:** Each message creates a new `StringBuilder` under `Parallel.ForEach` — thousands of concurrent allocations.

**Proposal:** Use `ObjectPool<StringBuilder>` (from `Microsoft.Extensions.ObjectPool`) or `ValueStringBuilder` / stackalloc for short texts.

**Dedup safety:** ✅ Safe — produces the same normalized text → same shingles → same MinHash signatures.

---

## Speed Improvements (Secondary)

### 8. Parallel file processing in `ImportFromJson`

**Current behavior:** Files are processed sequentially in a `foreach` loop.

**Proposal:** Process multiple files concurrently using `Parallel.ForEachAsync` with bounded parallelism, using separate `DbContext` instances per file.

**Dedup safety:** ✅ Safe — each file/chat is independent. `DeduplicateAds` runs after all files finish, so the full ad set remains available.

---

### 9. Bulk insert instead of `AddRangeAsync` + `SaveChangesAsync`

**Current behavior:** Each batch calls `AddRangeAsync` + `SaveChangesAsync`, which generates individual `INSERT` statements via EF Core.

**Proposal:** Use bulk insert (EF Core extensions or raw multi-value `INSERT`) to reduce round-trips. SQLite supports multi-value inserts natively.

**Dedup safety:** ✅ Safe — write-path optimization only; same data reaches the DB.

---

### 10. Move MinHash computation into `Parallel.ForEach`

**Current behavior:** `GenerateMinHashSignature` is CPU-bound but called sequentially in the batch loop after the parallel section has finished.

**Proposal:** Move signature computation into the `Parallel.ForEach` alongside ad entity creation (text is already available there). Collect `(AdEntity, uint[], int)` tuples from the parallel section and upsert them sequentially in the batch loop as before.

**Dedup safety:** ⚠️ Safe with constraint. `MinHashCalculator.GenerateSignature` only reads pre-built hash functions and creates a local `uint[]` — thread-safe. `ApplicationDbContext` is not thread-safe, so DB upsert calls (`VectorStore`, `VectorIndex`) must remain in the sequential batch loop.

---

### 11. Single `ExecuteUpdateAsync` for dedup marking

**Current behavior:** `DeduplicateAds` issues one `ExecuteUpdateAsync` per chunk with a `WHERE Id IN (...)` subquery — many round-trips for large sets.

**Proposal:** Use a single `ExecuteUpdateAsync` with the full ID set, or a temp-table approach to let SQLite do a single indexed join.

**Dedup safety:** ✅ Safe — only changes how `IsUnique = true` is written, not how uniqueness is determined.

---

### 12. Skip existence check for `New` state in `ProcessAndInsert`

**Current behavior:** Each batch queries `_dbContext.Ads.Where(a => batchIds.Contains(a.Id))` even when the state is `New` and the table is known empty for that chat.

**Proposal:** Pass the `UploadedDataState` down to `ProcessAndInsert` and skip the per-batch existence check when state is `New`.

**Dedup safety:** ✅ Safe — deterministic GUIDs guarantee no collisions across chats; the check is a no-op for new chats.

---

## Risk Summary

| Risk level | Proposals |
|------------|-----------|
| ✅ No risk | 1, 3, 4, 5, 7, 8, 9, 11, 12 |
| ⚠️ Safe with constraint | 10 — keep DB writes sequential |
| ⚠️ Safe with design choice | 6 — switch to `DistinctPersistent`; 2 — requires model refactor |

---

## Recommended Starting Points

1. **Proposal 6** — switch `DeduplicateAds` to `DistinctPersistent`. Highest memory impact, lowest risk, zero new code required.
2. **Proposal 1** — stream JSON deserialization. Eliminates the largest single per-file allocation.
3. **Proposal 5** — push diff to SQL. Eliminates three large `HashSet<Guid>` allocations on every incremental import.
4. **Proposal 10** — move MinHash into parallel section. Best CPU throughput gain with no correctness risk if upserts stay sequential.
