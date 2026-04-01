# Changelog

## 0.2.0 (2026-03-31)

- Add nested object tracking with dot-notation paths (e.g. `Address.City`)
- Add collection diff reporting individual element additions, removals, and modifications
- Add `Rollback()` method to revert a tracked object to its snapshot state
- Add `CollectionDiff`, `CollectionChange`, and `CollectionChangeKind` types
- Add optional `CollectionDiff` property to `PropertyChange` record

## 0.1.7 (2026-03-31)

- Standardize README to 3-badge format with emoji Support section
- Update CI actions to v5 for Node.js 24 compatibility
- Add GitHub issue templates, dependabot config, and PR template

## 0.1.6 (2026-03-24)

- Add unit tests
- Add test step to CI workflow

## 0.1.5 (2026-03-23)

- Sync .csproj description with README

## 0.1.4 (2026-03-22)

- Add dates to changelog entries

## 0.1.3 (2026-03-17)

- Rename Install section to Installation in README per package guide

## 0.1.2 (2026-03-16)

- Add badges, Development section to README
- Add GenerateDocumentationFile, RepositoryType, PackageReadmeFile to .csproj

## 0.1.1 (2026-03-16)

## 0.1.0 (2026-03-16)

- Initial release
- `ChangeTracker<T>` with reflection-based property snapshot and diff
- `[TrackChanges]`, `[IgnoreChanges]`, `[SensitiveProperty]` attributes
- `PropertyChange` record for individual change entries
- `ChangeSet` with JSON serialization via `ToJson()` / `FromJson()`
