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

- **A build and release path, so the plugin can actually be installed.** `build.ps1` publishes
  both target frameworks, writes the `meta.json` that Jellyfin's `PluginManager` reads from the
  plugin folder, packs one ZIP per Jellyfin line into `dist\`, and maintains `manifest.json`.
  Until now there was source but no artifact - and without a `meta.json` beside the assembly
  the server does not load a plugin at all.
  - **Taken from the sibling plugins in this family rather than written fresh**, because the
    guards in it were paid for there: one version means one artifact, a run that will be
    refused must not leave a rewritten manifest behind, and a rebuild without `-Changelog`
    must not blank the catalogue text of a version that already has one. Where a comment
    reports something that happened, it now names the project it happened in - an inherited
    justification reading as if it had been measured here would be worse than none.
  - **`category` is `MoviesAndShows`, and that is measured.** The value looks like free text
    and is not: the official catalogue
    (`repo.jellyfin.org/files/plugin/manifest.json`, 34 packages) uses only Administration,
    General, MoviesAndShows, Music, Anime, Books, LiveTV and Subtitles. The plausible-looking
    `Metadata` belongs to no filter at all.
    - **Now a `ValidateSet` parameter rather than a literal**, so that mistake fails before
      the build instead of silently in the catalogue. Suggested by the Poster Overlays
      session after this measurement reached it.
    - **And the literal it replaced could never have been corrected anyway.** The package
      header is inherited from the existing `manifest.json` (`$package = $loaded[0]`); the
      literal in the `else` branch only applies when no manifest exists yet. Measured rather
      than believed: the header was set to `General` by hand and a rebuild left it `General`.
      It is now written on every run, like `owner` - the same trap that one had already
      sprung. Changing only the header is free, incidentally: it is not in the ZIP, so the
      published checksums stayed identical across the whole experiment.
    - The copy inside `meta.json` is never displayed at all - `PluginInfo`, what
      `GET /Plugins` returns, has no category field (read at `release-10.11.z`: Name, Version,
      ConfigurationFileName, Description, Id, CanUninstall, HasImage, Status). It travels in
      the ZIP though, so changing *that* one after a release would cost a new version.
  - **Angle brackets in the catalogue text are safe.** `<customid>` appears verbatim in the
    description; `jellyfin-web` on `release-10.11.z` renders it as plain JSX text in
    `plugin.tsx`, so React escapes it rather than swallowing it as an unknown tag.
  - **One check added beyond the inherited set**, and forced to fire rather than trusted:
    every assembly named in `meta.json` must be present in the package. A typo there would
    otherwise produce a plugin that installs and then does nothing.
    - **Exercised in isolation against the real artifact**, with the check lifted out of the
      shipped `build.ps1` rather than copied: the true name passes, an invented one aborts,
      a realistic typo (`…NfoCustomMergeI.dll`) aborts - and so does
      `…NfoCustomMergeId.Missing.dll`, which is the case that matters. That name *satisfies*
      check 6, because check 6 is a prefix filter (`-notlike 'Jellyfin.Plugin.NfoCustomMergeId.*'`)
      while check 7 compares exactly. The two are therefore independent rather than one
      restating the other.
    - Running the full build to break it does not work: changing `assemblies` changes
      `meta.json`, hence the ZIP, hence its checksum - and the checksum guard sits *before*
      the package checks, so it aborts first and the new check never runs. A red run proves
      the guard that actually fired, which is not necessarily the one under test.

- **A catalogue tile.** `logo.svg` shows the two release folders as two cards held together by
  a clasp, framed by angle brackets - which is the whole plugin in one picture. Flat colours
  rather than a gradient, because the thing has to survive being drawn at 48 px; checked at
  256, 96 and 48 before it was committed. `build.ps1` picks up any `logo.*` beside it and
  writes `imageUrl` into the manifest, so no release had to be rebuilt for this: the logo is
  not part of the ZIP.
- **The upstream reports are linked rather than asserted.**
  [#17769](https://github.com/jellyfin/jellyfin/issues/17769) is the gap this plugin closes -
  `MetadataProvider.Custom` documented as the merge override and unreachable from an NFO.
  [#17770](https://github.com/jellyfin/jellyfin/issues/17770) is the failure it was found
  through, and it is deliberately named as one this plugin does **not** fix: `<customid>`
  hands an owner a lever, it does not make the raw grouping key plausible or the merge
  visible in the log. Both open. A claim of "reported upstream" that cannot be looked up is
  indistinguishable from an unsupported one.

- **Verified on a live server, not only built.** Installed on Jellyfin 10.11.11 on
  2026-09-02, in three steps, each one answering a question the previous one could not.
  - **Is the registration picked up at all?** `GET /Items/{id}/ExternalIdInfos` on a series
    returned **6** entries before the install and **7** after, the new one being
    `Key=Custom  Name=Custom Merge ID`. So Jellyfin discovers an `IExternalId` from a plugin
    assembly by itself - no `IPluginServiceRegistrator` needed, which until then was read in
    the source rather than measured.
  - **Is the element read?** `<customid>v-1984-final-battle</customid>` written into a
    `tvshow.nfo`, then a refresh: `ProviderIds` went from `Imdb=… Tmdb=…` to
    `Custom=v-1984-final-battle Imdb=… Tmdb=…`. A positive result needs no positive control
    - the control is only owed for a negative one, which is why the earlier measurement
    carried a `<zap2itid>` and this one did not.
  - **Does it actually override?** This is the step that could have been skipped and would
    have proved nothing if it had been. Two folders were given **the same** `<tvdbid>-1</tvdbid>`
    on purpose - the exact collision that had merged them into one series the night before -
    while their `customid` values differed. They stayed separate, stable across three
    consecutive polls. Without that collision both would have stayed separate anyway, and
    "they are separate" would have measured nothing at all. The planted `-1` was removed
    afterwards and the item rebuilt to clear it, file count and bytes unchanged.

### Fixed
- **The `pre-commit` hook could not fire.** It came over with the rest of the scaffolding
  and its path filter still read `^Jellyfin\.Plugin\.JFLint/.*\.(cs|csproj)$` - a path this
  repository does not contain, so the guard exited 0 on every commit and the changelog it
  enforces was never actually enforced. It had no chance to be noticed either: the first
  commit carried a changelog anyway and the second touched no source.
  - **Forced to fire rather than read as correct**, all four directions in a throwaway
    repository: with the repaired filter a source-only commit aborts, a commit carrying
    `CHANGELOG.md` passes, and a commit touching only `README.md` is not intercepted - while
    the old hook lets that first case through. The last of the four is the one that turns
    "the hook is now right" into "the hook was wrong".
