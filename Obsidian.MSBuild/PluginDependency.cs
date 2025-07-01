namespace Obsidian.MSBuild;
public readonly struct PluginDependency(string id, string version, bool required)
{
    public string Id { get; } = id;

    public string Version { get; } = version;

    public bool Required { get; } = required;
}