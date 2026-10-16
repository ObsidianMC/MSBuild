namespace Obsidian.MSBuild
{
    /// <summary>
    /// One entry of the "dependencies" array in obsidian.mod.json.
    /// Optional fields left null are omitted from the JSON so the reader applies its defaults
    /// (type "required", any version, ordering "none", side "both").
    /// </summary>
    public sealed class PluginDependency
    {
        public string ModId { get; }

        /// <summary>required, optional, incompatible or discouraged.</summary>
        public string Type { get; }

        /// <summary>A NuGet/Maven style range such as "[1.0,2.0)" or a minimum version.</summary>
        public string VersionRange { get; }

        /// <summary>none, before or after.</summary>
        public string Ordering { get; }

        /// <summary>both, server or client.</summary>
        public string Side { get; }

        public PluginDependency(string modId, string type, string versionRange, string ordering, string side)
        {
            this.ModId = modId;
            this.Type = type;
            this.VersionRange = versionRange;
            this.Ordering = ordering;
            this.Side = side;
        }
    }
}
