using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.NfoCustomMergeId;

/// <summary>
/// Registers <see cref="MetadataProvider.Custom"/> as an external id, which is the whole
/// plugin: it makes <c>&lt;customid&gt;</c> a readable element in a <c>tvshow.nfo</c>.
/// </summary>
/// <remarks>
/// <para>
/// Jellyfin already groups series by that id and says so in its own enum - "This metadata
/// provider is for users and/or plugins to override the default merging behaviour". What it
/// does not do is give anyone a way to SET it from disk. The NFO parser builds its list of
/// readable elements from the REGISTERED external ids:
/// </para>
/// <code>
/// // BaseNfoParser.Fetch
/// foreach (var info in ProviderManager.GetExternalIdInfos(item.Item))
/// {
///     _validProviderIds.TryAdd(info.Key + "Id", info.Key);
/// }
/// </code>
/// <para>
/// and no provider registers the key <c>Custom</c>, so <c>&lt;customid&gt;</c> is dropped
/// without a word. Measured against 10.11.11 on 2026-09-02 with a positive control in the
/// same run: <c>&lt;custom&gt;</c>, <c>&lt;customid&gt;</c> and <c>&lt;custom_id&gt;</c> all
/// arrived nowhere, while a <c>&lt;zap2itid&gt;</c> written into the same file did - so the
/// file was read and the three were genuinely ignored.
/// </para>
/// <para>
/// The name is therefore not free. <see cref="Key"/> must be exactly <c>Custom</c>, because
/// that is the one the grouping code reads (<c>Series.GetUserDataKeys</c> asks for
/// <see cref="MetadataProvider.Custom"/>); a prettier key like <c>CustomMerge</c> would give
/// a readable <c>&lt;custommergeid&gt;</c> that nothing ever looks at.
/// </para>
/// <para>
/// What this buys, in both directions: series that must NOT be merged get different values,
/// series that must be merged across release folders get the same one - and it beats the
/// provider ids, because <c>GetUserDataKeys</c> inserts Custom last and therefore first.
/// Above all the value lives in the NFO, so it survives a rebuild of the database from disk,
/// which an id typed into the metadata editor does not.
/// </para>
/// </remarks>
public class CustomMergeIdExternalId : IExternalId
{
    /// <inheritdoc />
    /// <remarks>
    /// Shown in the metadata editor beside the field. Free text, unlike <see cref="Key"/>.
    /// </remarks>
    public string ProviderName => "Custom Merge ID";

    /// <inheritdoc />
    /// <remarks>
    /// Taken from the enum rather than typed as "Custom", so a rename upstream breaks the
    /// build instead of quietly producing an id nobody reads.
    /// </remarks>
    public string Key => MetadataProvider.Custom.ToString();

    /// <inheritdoc />
    /// <remarks>
    /// Null on purpose: the value is an opaque local marker, not a handle at some website,
    /// so there is no page to link to.
    /// </remarks>
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    /// <remarks>
    /// Series only, and deliberately so. Automatic grouping by provider id is a
    /// <see cref="Series"/> behaviour - <c>Series.CreatePresentationUniqueKey</c> is the
    /// override that reads the key, gated on the library's EnableAutomaticSeriesGrouping.
    /// Offering the field on items where it changes nothing would be a field that lies.
    /// </remarks>
    public bool Supports(IHasProviderIds item) => item is Series;
}
