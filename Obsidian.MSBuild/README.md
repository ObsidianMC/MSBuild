# Obsidian.MSBuild
An MSBuild package that packs your plugin into a version 2 `.obby` archive when it is built or published.

# Installation
NuGet package: https://www.nuget.org/packages/Obsidian.MSBuild
```
Install-Package Obsidian.MSBuild
```

# Instructions
Install the package and set the plugin metadata in your csproj:

```xml
<PropertyGroup>
    <!-- Required: the mod id Obsidian and other plugins refer to you by. -->
    <PluginId>ruby</PluginId>

    <!-- Standard properties reused as metadata. -->
    <Version>1.2.0</Version>
    <Title>Ruby</Title>
    <Authors>Tides, Ada</Authors> <!-- comma-separated -->
    <Description>Adds rubies.</Description>
    <ProjectUrl>https://example.com/ruby</ProjectUrl>

    <!-- Optional: the Obsidian.API version you build against. -->
    <PluginApiVersion>1.0.0</PluginApiVersion>

    <!-- Optional: both (default), server or client. -->
    <PluginSide>both</PluginSide>

    <!-- Optional: the folder packed, where the archive is written too. Defaults to the build output folder. -->
    <PluginPublishDir>$(PublishDir)</PluginPublishDir>

    <!-- Optional: the folders packed as assets/ and data/, relative to the project. Default: assets and data. -->
    <PluginAssetsDir>Resources/assets</PluginAssetsDir>
    <PluginDataDir>Resources/data</PluginDataDir>
</PropertyGroup>

<ItemGroup>
    <!-- Optional dependencies on other plugins. -->
    <PluginDependency Include="gemlib" Version="[1.0,2.0)" Type="required" Ordering="after" Side="both" />
    <PluginDependency Include="prettylib" Required="false" />
</ItemGroup>
```

Build (or publish) the plugin and `<AssemblyName>.obby` appears in `PluginPublishDir`.

## Metadata
The task generates `obsidian.mod.json` from the properties above. If an `obsidian.mod.json` file exists in the
project directory (or wherever `PluginMetadataFile` points), it is packed as-is instead.

| JSON field | MSBuild source |
| --- | --- |
| `modId` (required) | `PluginId` |
| `version` (required) | `Version` |
| `assembly` (required) | `AssemblyName` |
| `displayName` | `Title` |
| `apiVersion` | `PluginApiVersion` |
| `authors` | `Authors`, split on commas |
| `description` | `Description` |
| `projectUrl` | `ProjectUrl` |
| `side` | `PluginSide`: `both` (default), `server` or `client` |
| `dependencies` | `@(PluginDependency)` items |

`PluginDependency` item metadata:

| Metadata | JSON field | Values |
| --- | --- | --- |
| `Include` or `Id` | `modId` | the dependency's mod id |
| `Version` (or `VersionRange`) | `versionRange` | a NuGet/Maven range such as `[1.0,2.0)` or a minimum version; any version when omitted |
| `Type` | `type` | `required` (default), `optional`, `incompatible` or `discouraged` |
| `Required` | `type` | `true` maps to `required`, `false` to `optional`; ignored when `Type` is set |
| `Ordering` | `ordering` | `none`, `before` or `after` |
| `Side` | `side` | `both`, `server` or `client` |

## Assets and data
The `assets` and `data` folders of the project are packed under `assets/` and `data/`, laid out like a resource pack
and a data pack: `assets/<namespace>/...` for client resources and `data/<namespace>/...` for server data. Set
`PluginAssetsDir` or `PluginDataDir` in the project to pack other folders (paths relative to the project, or
absolute), or set one empty to pack no such folder. A folder that doesn't exist is skipped.

## Archive layout
A version 2 `.obby` is a ZIP file:

| Path | Contents |
| --- | --- |
| `obsidian.mod.json` | Metadata (see above). |
| `<Assembly>.dll`, `<Assembly>.pdb`, `<Assembly>.deps.json` | The plugin assembly and its publish outputs. |
| `*.dll`, `*.pdb` | Libraries beside the main assembly, as `dotnet publish` lays them out. |
| `assets/<namespace>/...` | Optional client resources. |
| `data/<namespace>/...` | Optional server data. |

Only the top-level files of `PluginPublishDir` are packed; `testhost*` files and the archive itself are skipped.
