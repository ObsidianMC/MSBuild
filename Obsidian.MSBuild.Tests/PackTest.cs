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

    private const string PrivateKey = """
        -----BEGIN PRIVATE KEY-----
        MIICdQIBADANBgkqhkiG9w0BAQEFAASCAl8wggJbAgEAAoGBAMU/F0sTHCUeJL3U
        /rnmcTGQpSdGQrfaAEOTdVZlaNskLL1QyQUi+ejbiFhisDDr2w6yFJCtQ8f717cn
        mHLzf3tOZtnLCz5cRcABkNu+9XOPBUpHt+VV445NlpbraWZKEmsXZgJ0yv/jBv8i
        HoGIPyvODmn19kbVH0ODOvcVTLmTAgMBAAECgYALC6tsQteypGt+Te0tz9/K3MTC
        3EZkMUsOfbV2bxteGjp/J4T6SqkgBxsth+lB9BNCUWqhZ3KCQnIkCY2Z8lTTI7My
        dVgFhO2ch8mEsNlcCH3ZY1HmOzdmO8scExlSE9X4ZgThpCrvE6FRpaIbHnRVko2S
        +AJJsuznlpHcjuE7gQJBAOl+oWLCdCcJFMUS1ruSyTtU7HO/Vwu/gqumrpxBtIDX
        CSJHq71+PL3yfSZ/SXxRSC0X+HI3CcK8DrG4joOOUrMCQQDYQg2uAXC3Ug02xNtp
        6+GVREFJWSCyYVQYJcRw0ayu3H/z6cRv1LKA/UKUQzrMNic6n0iHJaiwRS0K4t7S
        We2hAkAK8PshBJmqxpspjPNxALTbSeR2nA25KDU4U+w0uEN8EheEerVKgOLZx8Yj
        iq1n3Osz6b6jo36amHNb0pkjAwVPAkASBf9J30jbnnUHeYSn4Ubdv+CJEmqNM1tk
        39DtbiwsLqhjVbpPb7So13KzFJ9T4beHRTswOE1E058bZykW8vPBAkB8OQ4TbQRs
        oTBYOAc/F4JVGrqma04CjEPUn4J/5qzSScI8hkImfJvtZk8Dd8lQ/wpb94KTfWwR
        4IiXOmwPm/Zt
        -----END PRIVATE KEY-----
        """;

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
            PluginDependencies = [],
            BuildEngine = this.buildEngine.Object,
            PluginName = "My Sample Plugin",
            PluginSigningKey = PrivateKey,
            PluginPublishDir = "."
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
            PluginPublishDir = ".",
            PluginDependencies = [],
            BuildEngine = this.buildEngine.Object,
            PluginName = "My Sample Plugin",
            ProjectUrl = "https://obsidianmc.net",
            PluginSigningKey = PrivateKey,
        };

        var success = packTask.Execute();

        Assert.IsTrue(success);
        Assert.IsTrue(File.Exists(packTask.PackedFile));
        using (var file = File.OpenRead(packTask.PackedFile))
        {
            using var reader = new BinaryReader(file);

            var headerId = Encoding.ASCII.GetString(reader.ReadBytes(4));
            var apiVersion = reader.ReadString();

            var hash = reader.ReadBytes(SHA384.HashSizeInBytes);
            var signed = reader.ReadBoolean();
            if (signed)
            {
                var length = reader.ReadInt32();
                reader.ReadBytes(length);
            }

            var dataLength = reader.ReadInt32();
            var dataPos = file.Position;

            using (var sha384 = SHA384.Create())
            {
                var verifyHash = sha384.ComputeHash(file);
                var hashString = Convert.ToHexString(verifyHash);

                Assert.IsTrue(verifyHash.SequenceEqual(hash), $"Expected {Convert.ToHexString(hash)} got {hashString}");
            }

            file.Position = dataPos;

            var assemblyName = reader.ReadString();
            var version = reader.ReadString();

            var name = reader.ReadString();
            var id = reader.ReadString();
            var authors = reader.ReadString();
            var description = reader.ReadString();
            var projectUrl = reader.ReadString();

            var dependencies = new List<PluginDependency>();
            var dependsLength = reader.ReadInt32();
            for (int i = 0; i < dependsLength; i++)
            {
                dependencies.Add(new(reader.ReadString(), reader.ReadString(), reader.ReadBoolean()));
            }

            var entryCount = reader.ReadInt32();

            Assert.AreEqual("OBBY", headerId);
            Assert.AreEqual("1.0.0", apiVersion);

            Assert.AreEqual("MySamplePluginTemplate", assemblyName);
            Assert.AreEqual("1.0.0", version);

            Assert.AreEqual("My Sample Plugin", name);
            Assert.AreEqual("obsidianteam.mysampleplugin", id);
            Assert.AreEqual("ObsidianTeam", authors);
            Assert.AreEqual("No description provided", description);
            Assert.AreEqual("https://obsidianmc.net", projectUrl);
            Assert.AreEqual(0, dependencies.Count);

            Console.WriteLine($"Hash: {Convert.ToHexString(hash)}");
            Console.WriteLine($"Data Length: {dataLength}");

            Assert.IsTrue(signed);
            Assert.AreEqual(37, entryCount);
        }

        File.Delete(packTask.PackedFile);
    }
}