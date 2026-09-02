using System;
using Jellyfin.Plugin.NfoCustomMergeId.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.NfoCustomMergeId;

/// <summary>
/// Entry point of the plugin. It contributes nothing but the external id registration in
/// <see cref="CustomMergeIdExternalId"/>; this class exists because Jellyfin discovers
/// plugin assemblies through it.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "NFO Custom Merge ID";

    /// <inheritdoc />
    public override string Description =>
        "Makes <customid> readable from a tvshow.nfo. Jellyfin already groups series by that " +
        "id and has no way to set it from disk; with this, two release folders can be forced " +
        "apart or forced together, and the decision survives a rebuild of the database.";

    /// <inheritdoc />
    /// <remarks>
    /// Fixed for the life of the plugin: Jellyfin keys the installed folder and the update
    /// check on it, so a new one would look like a different plugin.
    /// </remarks>
    public override Guid Id => Guid.Parse("b7f4c2a1-5e93-4d68-9c07-3a1f8e2d6b40");
}
