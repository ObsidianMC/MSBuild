using Microsoft.Build.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace Obsidian.MSBuild.Tests;

[TestClass]
public class PackTest
{
    private Mock<IBuildEngine> buildEngine;
    private List<BuildErrorEventArgs> errors;

    [TestInitialize()]
    public void Startup()
    {
        this.buildEngine = new();
        this.errors = [];
        this.buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => errors.Add(e));
    }

    [TestMethod]
    public void Pack_Success()
    {
        var packTask = new Pack
        {
            PluginApiVersion = "1.0.0",
            PluginId = "obsidianteam.mysampleplugin",
            PluginAssembly = "MySamplePluginTemplate",
            PluginAuthors = "ObsidianTeam",
            PluginVersion = "1.0.0",
            PluginPublishDir = ".\\Resources\\",
            PluginDependencies = [],
            BuildEngine = this.buildEngine.Object,
            PluginName = "My Sample Plugin"
        };

        var success = packTask.Execute();

        Assert.IsTrue(success);
        Assert.IsTrue(File.Exists(packTask.PackedFile));

        File.Delete(packTask.PackedFile);
    }

    [TestMethod]
    public void Pack_Read_Success()
    {
        var packTask = new Pack
        {
            PluginApiVersion = "1.0.0",
            PluginId = "obsidianteam.mysampleplugin",
            PluginAssembly = "MySamplePluginTemplate",
            PluginAuthors = "ObsidianTeam",
            PluginVersion = "1.0.0",
            PluginPublishDir = ".\\Resources\\",
            PluginDependencies = [],
            BuildEngine = this.buildEngine.Object,
            PluginName = "My Sample Plugin"
        };

        var success = packTask.Execute();

        Assert.IsTrue(success);
        Assert.IsTrue(File.Exists(packTask.PackedFile));

        using (var file = File.OpenRead(packTask.PackedFile))
        {
            using var reader = new BinaryReader(file);

            var headerId = Encoding.ASCII.GetString(reader.ReadBytes(4));
            var apiVersion = reader.ReadString();

            var assemblyName = reader.ReadString();
            var version = reader.ReadString();
            var name = reader.ReadString();
            var id = reader.ReadString();
            var authors = reader.ReadString();
            var description = reader.ReadString();

            var hash = reader.ReadBytes(SHA384.HashSizeInBytes);
            var signed = reader.ReadBoolean();
            var dataLength = reader.ReadInt32();
            var entryCount = reader.ReadInt32();

            Assert.AreEqual("OBBY", headerId);
            Assert.AreEqual("1.0.0", apiVersion);

            Assert.AreEqual("MySamplePluginTemplate", assemblyName);
            Assert.AreEqual("1.0.0", version);

            Assert.AreEqual("My Sample Plugin", name);
            Assert.AreEqual("obsidianteam.mysampleplugin", id);
            Assert.AreEqual("ObsidianTeam", authors);
            Assert.AreEqual("No description provided", description);

            Console.WriteLine($"Hash: {Convert.ToHexString(hash)}");
            Console.WriteLine($"Data Length: {dataLength}");

            Assert.IsFalse(signed);
            Assert.AreEqual(9, entryCount);
        }

        File.Delete(packTask.PackedFile);
    }
}