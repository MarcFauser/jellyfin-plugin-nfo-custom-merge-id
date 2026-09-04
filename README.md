# Jellyfin NFO Custom Merge ID

Makes `<customid>` readable from a `tvshow.nfo`, so that whether two release folders become
one series is decided on disk instead of by whatever provider id happens to match.

## The problem

Jellyfin merges series across folders by provider id, and the field it uses is not obvious.
From `Series.cs`:

```csharp
public override List<string> GetUserDataKeys()
{
    var list = base.GetUserDataKeys();
    if (this.TryGetProviderId(MetadataProvider.Imdb, out var key)) { list.Insert(0, key); }
    if (this.TryGetProviderId(MetadataProvider.Tvdb, out key))     { list.Insert(0, key); }
    if (this.TryGetProviderId(MetadataProvider.Custom, out key))   { list.Insert(0, key); }
    return list;
}

public override string CreatePresentationUniqueKey()
{
    if (LibraryManager.GetLibraryOptions(this).EnableAutomaticSeriesGrouping)
    {
        var userdatakeys = GetUserDataKeys();
        if (userdatakeys.Count > 1) { return AddLibrariesToPresentationUniqueKey(userdatakeys[0]); }
    }
    return base.CreatePresentationUniqueKey();
}
```

Each id is inserted at position 0, so the **last one inserted comes first**: `Custom`, then
`Tvdb`, then `Imdb`. The grouping key is `userdatakeys[0]`, taken as the raw value with no
prefix and no plausibility check. TMDb, AniDB and AniList are not in the list at all.

Two consequences worth knowing before touching any NFO:

- Two folders whose **TVDB** ids are equal are one series - including when that id is a
  placeholder such as `-1`. Nothing is logged, and the second entry simply stops existing.
- A placeholder in `<tmdbid>`, `<anidbid>` or `<anilistid>` is harmless for grouping,
  because those fields never form the key.

`Custom` is the intended lever. Its own enum says so:

> This metadata provider is for users and/or plugins to override the default merging
> behaviour.

But nothing registers it as an external id, and the NFO parser only reads elements belonging
to registered ones:

```csharp
foreach (var info in ProviderManager.GetExternalIdInfos(item.Item))
{
    _validProviderIds.TryAdd(info.Key + "Id", info.Key);
}
```

so `<customid>` is dropped without a word.

## What the plugin does

Registers `MetadataProvider.Custom` as an external id for `Series`. That is all of it - one
class, four members. Afterwards `<customid>` is read from a `tvshow.nfo` like any other id,
and it outranks TVDB and IMDb as the grouping key.

## Usage

Keep two shows apart, whatever their provider ids say - one value each, any two distinct
strings will do:

```xml
<!-- .../Show.1983.S01.../tvshow.nfo -->
<tvshow>
  <tmdbid>14141</tmdbid>
  <customid>v-1983-miniseries</customid>
</tvshow>
```

```xml
<!-- .../Show.1984.S01.../tvshow.nfo -->
<tvshow>
  <tmdbid>75896</tmdbid>
  <customid>v-1984-final-battle</customid>
</tvshow>
```

Hold several release folders together as one series - the **same** value in each:

```xml
<customid>tng-remastered</customid>
```

Notes:

- Applies to `Series`. Automatic grouping by provider id is a series behaviour, and it needs
  the library's **Automatic series grouping** switched on - without it,
  `CreatePresentationUniqueKey` never consults the ids at all.
- **It would do nothing for films, and that is a property of the class tree rather than a
  choice.** `Series` overrides `CreatePresentationUniqueKey` and returns `userdatakeys[0]`,
  which is where a `<customid>` lands. `Movie` has no override at all: it inherits `Video`'s,
  which returns `PrimaryVersionId` when a human has merged files as alternate versions, and
  otherwise falls through to `BaseItem` - `Id.ToString("N")`, the item's own id. A film's
  grouping key never looks at a provider id, so offering the field there would be a field
  that lies. Read at `release-10.11.z`, after a neighbouring project measured the same thing
  from the other side.
- **Holding folders together works within one library, not across libraries of different
  metadata language.** What `Series` returns is not the key itself but
  `AddLibrariesToPresentationUniqueKey(userdatakeys[0])`, which appends the preferred
  metadata language and the ids of every collection folder the series sits in:

  ```csharp
  var lang = GetPreferredMetadataLanguage();
  if (!string.IsNullOrEmpty(lang)) { key += "-" + lang; }
  var folders = LibraryManager.GetCollectionFolders(this).Select(i => i.Id.ToString("N")).ToArray();
  return folders.Length == 0 ? key : key + "-" + string.Join('-', folders);
  ```

  Two series carrying the same `<customid>` in libraries of differing language therefore end
  up with **different** presentation keys. For release folders of one show this rarely
  matters - they normally live in the same library - but it is the kind of edge that costs an
  afternoon if you meet it without knowing.
- A refresh overwrites provider ids but never removes one. Changing a value takes effect;
  taking one away needs the metadata editor, or the item to be built again.
- The value is opaque. A GUID works, and so does a readable slug - the second is easier to
  recognise in a diff a year later.

## Building

```powershell
./build.ps1
```

Publishes both target frameworks, writes the `meta.json` that Jellyfin's `PluginManager`
reads from the plugin folder, and packs one installable ZIP per Jellyfin line into `dist\`.
`net9.0` is the Jellyfin 10.11 build, `net10.0` the Jellyfin 12 one - both from the same
source, with the major version of the assembly saying which line a build belongs to.

The artifacts are reproducible: the timestamp and the archive entry times are pinned to the
last commit that touched the plugin, so an unchanged source rebuilds to the same bytes and a
published release keeps its checksum. Measured, not assumed - two runs gave the same MD5.

For a plain compile without packaging:

```powershell
dotnet build Jellyfin.Plugin.NfoCustomMergeId.slnx -c Release
```

## Installing

Extract the ZIP for your line into `<ProgramDataPath>/plugins/NFO Custom Merge ID_<version>/`
on the server and restart it; `GET /System/Info` reports the `ProgramDataPath`.

`./build.ps1 -Changelog '...' -Publish` instead creates one GitHub release per artifact and
pushes `manifest.json`, which is the file a plugin repository URL points at. The releases go
first and the manifest only after the uploaded files have been fetched back and hashed: a
release nobody's manifest names is merely invisible, but a manifest entry without its release
is a failed download in someone's dashboard.

## Status

Both halves of this are reported upstream, and both are open:

- [#17769](https://github.com/jellyfin/jellyfin/issues/17769) - `MetadataProvider.Custom` is
  documented as the merge override but cannot be set from an NFO. That is the gap this plugin
  closes. If Jellyfin registers the id itself, the plugin becomes redundant, which is the
  right shape for something that exists because of a missing registration.
- [#17770](https://github.com/jellyfin/jellyfin/issues/17770) - two unrelated series merge
  silently when their TVDB ids happen to match a placeholder value. That is the failure this
  was found through, and it is **not** what the plugin fixes: `<customid>` hands you a lever,
  it does not make the raw grouping key plausible or the merge visible in the log. A `-1` in
  two `<tvdbid>` elements still collapses two series into one.
