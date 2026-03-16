# Changelog

## 0.1.0 (2026-03-15)

- Initial release
- `ChangeTracker<T>` with reflection-based property snapshot and diff
- `[TrackChanges]`, `[IgnoreChanges]`, `[SensitiveProperty]` attributes
- `PropertyChange` record for individual change entries
- `ChangeSet` with JSON serialization via `ToJson()` / `FromJson()`
