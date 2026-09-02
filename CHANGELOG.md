# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

The major version encodes the Jellyfin line a build belongs to: **11.x** for Jellyfin 10.11,
**12.x** for Jellyfin 12. The two are built from the same source.

## Versions

- [Unreleased](#unreleased)

## [Unreleased]

### Added
- **`<customid>` becomes readable from a `tvshow.nfo`.** The plugin registers
  `MetadataProvider.Custom` as an external id, and that is the whole of it: Jellyfin's NFO
  parser builds the elements it will read from the registered external ids
  (`_validProviderIds.TryAdd(info.Key + "Id", info.Key)`), and nothing registered the key
  `Custom`, so the element was dropped without a word.
  - **What it is good for, in both directions.** Series that must not be merged get
    different values; release folders that belong to one series get the same one. It wins
    over the provider ids because `Series.GetUserDataKeys` inserts Custom last and therefore
    first, and `CreatePresentationUniqueKey` groups on `userdatakeys[0]`.
  - **And it survives.** The metadata editor can already set the id
    (`ItemUpdateController` assigns `item.ProviderIds` wholesale), but only into the
    database. From an NFO the value is on disk and outlives a full rebuild of the library.
  - **The key cannot be renamed.** `Key` has to be exactly `Custom`, because that is what
    the grouping code asks for; a nicer `CustomMerge` would produce a readable
    `<custommergeid>` that nothing reads. Taken from the enum rather than typed, so an
    upstream rename breaks the build instead of silently producing a dead id.
  - **Series only.** Automatic grouping by provider id is a `Series` behaviour, gated on the
    library's `EnableAutomaticSeriesGrouping`. Offering the field where it changes nothing
    would be a field that lies.
- **The measurement the plugin rests on**, against 10.11.11 on 2026-09-02 and with a
  positive control in the same run: `<custom>`, `<customid>` and `<custom_id>` were written
  into one `tvshow.nfo` with three distinguishable values, plus a `<zap2itid>`. After the
  refresh the Zap2It value was on the item and none of the other three were - so the file
  was read, and the three were genuinely ignored rather than the file skipped.
