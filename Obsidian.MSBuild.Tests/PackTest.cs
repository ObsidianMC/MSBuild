using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Obsidian.MSBuild.Tests;

[TestClass]
public class PackTest
{
    private string root = null!;
    private string publishDir = null!;

    /// <summary>Lays out a fake publish folder plus assets/ and data/ trees in a fresh temp directory.</summary>
    [TestInitialize]
    public void CreateFakeProject()
    {
        this.root = Path.Combine(Path.GetTempPath(), "obby-" + Guid.NewGuid().ToString("N"));
        this.publishDir = Path.Combine(this.root, "publish");

        WriteFile(Path.Combine(this.publishDir, "Ruby.dll"), RandomBytes(4096));
        WriteFile(Path.Combine(this.publishDir, "Ruby.pdb"), RandomBytes(100));
        WriteFile(Path.Combine(this.publishDir, "Ruby.deps.json"), Encoding.UTF8.GetBytes("{}"));
        WriteFile(Path.Combine(this.publishDir, "GemLib.dll"), RandomBytes(2048));
        WriteFile(Path.Combine(this.publishDir, "testhost.dll"), RandomBytes(10));
        WriteFile(Path.Combine(this.root, "assets", "ruby", "textures", "item", "ruby.png"), RandomBytes(64));
        WriteFile(Path.Combine(this.root, "data", "ruby", "recipes", "ruby.json"), Encoding.UTF8.GetBytes("{}"));
    }

    [TestCleanup]
    public void DeleteFakeProject()
    {
        Directory.Delete(this.root, recursive: true);
    }

    [TestMethod]
    public void Pack_WritesVersion2Archive()
    {
        var task = this.CreateTask();

        Assert.IsTrue(task.Execute());
        Assert.AreEqual(Path.Combine(this.publishDir, "Ruby.obby"), task.PackedFile);

        using var archive = ZipFile.OpenRead(task.PackedFile);

        var expectedEntries = new[]
        {
            "GemLib.dll",
            "Ruby.deps.json",
            "Ruby.dll",
            "Ruby.pdb",
            "assets/ruby/textures/item/ruby.png",
            "data/ruby/recipes/ruby.json",
            "obsidian.mod.json"
        };

        CollectionAssert.AreEquivalent(expectedEntries, archive.Entries.Select(entry => entry.FullName).ToArray());
        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(this.publishDir, "Ruby.dll")),
            ReadEntry(archive, "Ruby.dll"));

        using var metadata = JsonDocument.Parse(ReadEntry(archive, "obsidian.mod.json"));
        var mod = metadata.RootElement;

        Assert.AreEqual("ruby", mod.GetProperty("modId").GetString());
        Assert.AreEqual("Ruby \"Gems\"", mod.GetProperty("displayName").GetString());
        Assert.AreEqual("1.2.0", mod.GetProperty("version").GetString());
        Assert.AreEqual("Ruby", mod.GetProperty("assembly").GetString());
        Assert.AreEqual("1.0.0", mod.GetProperty("apiVersion").GetString());
        Assert.AreEqual("Adds rubies.", mod.GetProperty("description").GetString());
        Assert.AreEqual("https://example.com/ruby", mod.GetProperty("projectUrl").GetString());
        Assert.AreEqual("both", mod.GetProperty("side").GetString());
        CollectionAssert.AreEqual(
            new[] { "Tides", "Ada" },
            mod.GetProperty("authors").EnumerateArray().Select(author => author.GetString()).ToArray());

        var dependencies = mod.GetProperty("dependencies").EnumerateArray().ToArray();
        Assert.AreEqual(2, dependencies.Length);

        Assert.AreEqual("gemlib", dependencies[0].GetProperty("modId").GetString());
        Assert.AreEqual("required", dependencies[0].GetProperty("type").GetString());
        Assert.AreEqual("[1.0,2.0)", dependencies[0].GetProperty("versionRange").GetString());
        Assert.AreEqual("after", dependencies[0].GetProperty("ordering").GetString());
        Assert.AreEqual("server", dependencies[0].GetProperty("side").GetString());

        Assert.AreEqual("optionallib", dependencies[1].GetProperty("modId").GetString());
        Assert.AreEqual("optional", dependencies[1].GetProperty("type").GetString());
        Assert.IsFalse(dependencies[1].TryGetProperty("versionRange", out _));
        Assert.IsFalse(dependencies[1].TryGetProperty("ordering", out _));
    }

    [TestMethod]
    public void Pack_UsesProjectMetadataFileVerbatim()
    {
        var custom = Encoding.UTF8.GetBytes("""{ "modId": "ruby", "version": "9.9.9", "assembly": "Ruby" }""");
        WriteFile(Path.Combine(this.root, "obsidian.mod.json"), custom);

        var task = this.CreateTask();
        task.PluginMetadataFile = Path.Combine(this.root, "obsidian.mod.json");

        Assert.IsTrue(task.Execute());

        using var archive = ZipFile.OpenRead(task.PackedFile);
        CollectionAssert.AreEqual(custom, ReadEntry(archive, "obsidian.mod.json"));
    }

    private Pack CreateTask()
    {
        var gemlib = new TaskItem("gemlib");
        gemlib.SetMetadata("Version", "[1.0,2.0)");
        gemlib.SetMetadata("Type", "Required");
        gemlib.SetMetadata("Ordering", "after");
        gemlib.SetMetadata("Side", "server");

        var optionalLib = new TaskItem("ignored");
        optionalLib.SetMetadata("Id", "optionallib");
        optionalLib.SetMetadata("Required", "false");

        return new Pack
        {
            BuildEngine = new Mock<IBuildEngine>().Object,
            PluginPublishDir = this.publishDir,
            PluginAssembly = "Ruby",
            PluginVersion = "1.2.0",
            PluginId = "ruby",
            PluginName = "Ruby \"Gems\"",
            PluginApiVersion = "1.0.0",
            PluginAuthors = "Tides, Ada",
            PluginDescription = "Adds rubies.",
            ProjectUrl = "https://example.com/ruby",
            PluginAssetsDir = Path.Combine(this.root, "assets"),
            PluginDataDir = Path.Combine(this.root, "data"),
            PluginDependencies = [gemlib, optionalLib]
        };
    }

    private static byte[] ReadEntry(ZipArchive archive, string path)
    {
        using var stream = archive.GetEntry(path)!.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    private static void WriteFile(string path, byte[] contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, contents);
    }

    private static byte[] RandomBytes(int length)
    {
        return RandomNumberGenerator.GetBytes(length);
    }
}
