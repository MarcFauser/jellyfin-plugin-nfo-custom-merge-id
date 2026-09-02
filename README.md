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
- A refresh overwrites provider ids but never removes one. Changing a value takes effect;
  taking one away needs the metadata editor, or the item to be built again.
- The value is opaque. A GUID works, and so does a readable slug - the second is easier to
  recognise in a diff a year later.

## Building

```powershell
dotnet build Jellyfin.Plugin.NfoCustomMergeId.slnx -c Release
```

`net9.0` is the Jellyfin 10.11 build, `net10.0` the Jellyfin 12 one. Both come from the same
source; the major version of the assembly says which line a build belongs to.

## Status

The gap this closes has been reported upstream. If Jellyfin registers the id itself, this
plugin becomes redundant - which is the right shape for something that exists because of a
missing registration.
