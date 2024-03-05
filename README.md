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
<PropertyGroup>

    <!-- this would usually be your publish directory. -->
    <PublishDir>$(PublishUrl)</PublishDir> 

    <!-- this would be the Obsidian.API version -->
    <PluginApiVersion>{{Obsidian.API VERSION}}</PluginApiVersion> 

    <PluginAssembly>{{YOUR PLUGIN ASSEMBLY}}</PluginAssembly>
    <PluginVersion>{{YOUR PLUGIN VERSION}}</PluginVersion>

    <!-- this is optional. used for signing plugins. must be in xmlformat and can be a file or just a direct value -->
    <PluginSigningKey>{{YOUR SIGNING KEY}}</PluginSigningKey> 

</PropertyGroup>
```

After that just publish or (build on Release) your plugin and it will pack the plugin for you automatically and output that to the build and or publish directory.