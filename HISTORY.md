# History

A narrative log of how this project came to be. The structured, versioned list of changes
lives in CHANGELOG.md; this file records the reasoning.

## 2026-09-02 - Two folders of the same show became one, and nothing said why

A library holds four incarnations of the same franchise: a 1983 two-part miniseries, its
1984 sequel miniseries, the 1984-85 weekly series, and a 2009 remake. Four folders, four
separate entries - until one of them was given `<tvdbid>-1</tvdbid>` as a guard against
being pulled into a neighbour. The guard caused exactly what it was meant to prevent: two
entries collapsed into one, the miniseries stopped existing as a series of its own, and its
episode reappeared underneath the sequel.

The reason is in `Series.cs`, and it is sharper than "a matching provider id merges things":

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

Three links, all of them checked at the time: the library had automatic grouping switched
on; the grouping key is `userdatakeys[0]`; and that entry is the **TVDB id**, because it is
inserted last and therefore lands first. The value is taken raw, with no prefix and no
plausibility check, so two folders holding `-1` produce one and the same key. TMDb, AniDB
and AniList never appear in that list at all - which is why the same sentinel had been used
in those fields for years without ever causing trouble.

## The lever Jellyfin already has, and cannot reach

`Custom` sits at the front of that list, ahead of TVDB and IMDb, and the enum says outright
what it is for:

> This metadata provider is for users and/or plugins to override the default merging
> behaviour.

Which is precisely the missing tool - a value that says "this folder is its own show" or
"these folders are one show", independently of what any provider thinks. Except that there
is no way to write it from disk. The NFO parser builds the set of elements it will read
from the **registered** external ids:

```csharp
foreach (var info in ProviderManager.GetExternalIdInfos(item.Item))
{
    _validProviderIds.TryAdd(info.Key + "Id", info.Key);
}
```

and nothing registers the key `Custom`. Measured against 10.11.11 rather than assumed, with
a positive control in the same run: `<custom>`, `<customid>` and `<custom_id>` were written
into one `tvshow.nfo` with three distinguishable values, and a `<zap2itid>` alongside them.
After the refresh the Zap2It value had arrived and none of the other three had. The file was
read; the three were ignored.

Setting the id through the metadata editor works - `ItemUpdateController` assigns
`item.ProviderIds = request.ProviderIds` wholesale - but it then exists only in the
database. For a library whose owner treats the database as disposable and everything on
disk as the truth, that is the wrong place for it.

## Why this is a plugin and not a patch

Registering an external id is all it takes, and a plugin may do it: one class, four
members, no server change. The key has to be exactly `Custom`, because that is the one the
grouping code reads - a friendlier `CustomMerge` would give a readable `<custommergeid>`
that nothing ever looks at.

The same three lines would be the fix upstream, and the gap has been reported. Until it
lands, this plugin closes it; afterwards it becomes redundant, which is the right shape for
a plugin that exists because of a missing registration.
