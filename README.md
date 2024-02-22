# Obsidian.MSBuild
An msbuild package that packs your plugins when published. 
This uses a format created by the tModloader team to pack and publish mods for terraria.

# Installation
Nuget package: https://www.nuget.org/packages/Obsidian.MSBuild
```
Install-Package Obsidian.MSBuild
```

# Instructions
If you're doing this manually and didn't use a template then you're going to create a new project.
Install the package.

Open up your csproj and make sure you have this in either a new or exisiting PropertGroup.

```csproj
<PropertyGroup Name="PackPlugin" AfterTargets="Publish">
    <PublishDir>$(PublishUrl)</PublishDir> <!-- this would usually be your publish directory. -->
    <ApiVersion>{{Obsidian.API VERSION}}</ApiVersion <!-- this would be the Obsidian.API version -->
    <PluginName>{{YOUR PLUGIN NAME}}</PluginName>
    <PluginVersion>{{YOUR PLUGIN VERSION}}</PluginVersion>
    <SigningKey>{{YOUR SIGNING KEY}}</SigningKey> <!-- this is optional. used for signing plugins. -->
</PropertyGroup>
```

After that just publish or (build on Release) your plugin and it will pack the plugin for you automatically and output that to the build and or publish directory.