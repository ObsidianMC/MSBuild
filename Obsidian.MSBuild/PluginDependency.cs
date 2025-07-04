namespace Obsidian.MSBuild
{
    public readonly struct PluginDependency
    {
        public string Id { get; }

        public string Version { get; }

        public bool Required { get; }
        public PluginDependency(string id, string version, bool required)
        {
            this.Id = id;
            this.Version = version;
            this.Required = required;
        }
    }
}
