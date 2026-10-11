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
    /// <summary>
    /// THESE ARE RANDOMLY GENERATED KEYS NOT USED FOR ANYTHING IMPORTANT
    /// </summary>
    private const string PrivateKey = """
        -----BEGIN PRIVATE KEY-----
        MIIG/QIBADANBgkqhkiG9w0BAQEFAASCBucwggbjAgEAAoIBgQCUJAAwPuZ2bm1E
        DdM/4fWwWTZT0Ba+vCKtBGWzpv3Bq68rEFBU0wnh2EOdazm2lBFZt60S904qhUpg
        nI3mlYBI71GmIJyXk8lo14IhOEIjRlDezA+51kg6IBNDpr7hD4C+ta5Yi7vKjGtk
        6sXTT8C2xurWPJNUmUzu8OTQfNemoXcTDs/FtQfiixTuMQ7zM5EWmNUiwp6cas2a
        NqdDY2/E/06VO7SQaHwAtlWyIqB0FJZgqMe/W4JmZHC7yA3N2QbXgiva8R6cja6Q
        kmA7jHcHz5r2z/iZNXroiga34iIk1AA36CxSNB/wyxxuO8a9cUwrdOSwM9zxLMFC
        VhL6sSOAcGdKu+ly8785ffgQD88X3fm4TA/2zcHUs3CISvXOfLynpIx9eEMlJar8
        d/MFpJn9CN62RqrFigYXzV8uUJOCPyaFeZKQk//mqMSmpKoITcyx2e4cuNHdBRRs
        +vo9fQARiIymLrKls/kwxqcWq6FgQTAHc5p8FY4nrH7tyM+BAiECAwEAAQKCAYBm
        47I10BolO4EseSW2AuyvtOakw6xogSbcYGd6pYstjl61XDlPENyWPayIk0acZq6+
        T3In8BgcNEN6YoG0GzXkckOVTKU2KfEDnlrFU9urwFS+yaBKhGfZ5xk6LX/5tNjI
        nEshOLwPbAPTLbSElanVyMamUaBKa8chVbK2k178XrzjQbBRMvDtCYhZ/zzQ8Ynd
        1fyCjgW3wf5XE4qMpW2lt2UPBU7d1ZP6sqlGdgJXXU0siU1ivN4O28foxIPB6u9g
        /ydlDXJK1+XM0bGvXZsfZDJ6qSgzGHWjYPHyEDxz3fTZvdG+Q0y4L/Y6rEpN0MiD
        5mJJ/d5jtI+Kzans3kNC5n39Ng+W2o565i0EjReVmq3QVcP7L5T1t/WhqxDsNXdv
        Vu1lRunzEzdFRCNQ57JaXkHMc70dRT3dxi1cWY/Y7Nb3rrK3j7hYD3S+7hMfiiqt
        sJWqsNtntOHChWdqQZXVI2Efof8m+5FyLC4yWZhhCuHCI0dAb60h6D737lhnOBkC
        gcEA54vCxsAqXHkZCoYj1sh6eP9V0y78BLTYKifV/ivMs9SR6ldN/OVcJSstZFwZ
        aqtHu/kau0SWSGGB5TnmYO1zNGpSoFoA5MzrhVrbEnbpv60rDZUvQAj3y3l1lSH5
        vQBpQcAMqzqZN6x2WC6TQ84bmHZHRO5RZKbRHdmDm/6bnvQnInGvcvyDl5bsZ1QW
        AWxjg9DGZdho6pNw39CrJ2qBfwDrPmSI9UX/sTMq+j6nSovBoiqka6HIidNEKdS6
        doBLAoHBAKPJPC+NuPV7CowAQO6VbVbEYaBUW8AhhBfN4qU0oHHIaVJamS2hCmSB
        /QoEXQ4NMddM3wXRRlAkCNjHPtemx6Fx2H0kOsCwf6XwKK9MF3xI8wyk9/4YvANq
        zTGhFFLeq/CpKQ/eWZ1mykwxermH6tN6Ny/k1OEMcrOJuUAC3oJLhuGCMAqOojpl
        fmeuJwtWvZlfBFqR89lerV/iYz69jtsY1VJmf57jsmY5fC/RpSJSfJi01faWUWEI
        Uj1ZU4Q7wwKBwGJ0qEbQ0XJuv7oc3cJnjsRCdmENGnZ522zZcYHZZ/qTidQmeW/u
        qybW3D9PdjNIT4FbZAV7HZf7djtdSluuvAzupOGwQ68Gf6M9xedtDunFHYhyBhxp
        c8xegiP+xW2bbiZaHkj06s+kktHeRBpR2qQSry1dVNjCoiraIb9EHTISyU05IAx5
        2Q6tSyqIs665Qvt629HUmpAcT6Or3Asvm47AekcWgrIgqJ/VjRHJcGMfWB+3mCB4
        M4h/f/11ii/3TwKBwB6ukvTBktWBsC8b2Q4YtfvcHAHB69IpNSqUahHSsv+9sGU6
        DZnrohvD8hgPSzNXq2+OufTICGj45yNc59vUJW+L+ScwQ0VXiwIV5Dk6gufIbqd+
        u+pAze/B8SCL8Ve42PLjbYrId3cyC1GMr1XULVxid7YkIvDpuQ8DDM39+5ri9SiH
        j+JaZ++SlcRsbmoEXM4/a3xf/RNKViYxLbBSKFHI7CVciCnGs+PMfwQiPNIaK7cb
        oT1pWWNZALb3ZdrOqwKBwQCh6qY77hi+3LyZsa3y+o4xhPgAnNYmiXhMU0uLwrPe
        Vt3PvuAKDwSmBNy3qAbKKb0vccLC9CEOP9dnWTNCBxTsC7ff5O0YRo+47xmcfOFf
        67/wfAix1f3bBbQVlv9HtE7id/8l3uJL/KOu9GrZXnpOBLGo46NfJ5k0sUN5RX18
        4nFYcVfCgQ5SmruKh+WNGw8OhJIf+K+RQ/F+etu1mBe7lyFAgd9ted9RRX/1sD62
        9z4AmGSA84+b8FYo7ZghAjI=
        -----END PRIVATE KEY-----
        """;

    /// <summary>
    /// THESE ARE RANDOMLY GENERATED KEYS NOT USED FOR ANYTHING IMPORTANT
    /// </summary>
    private const string PublicKey = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAlCQAMD7mdm5tRA3TP+H1
        sFk2U9AWvrwirQRls6b9wauvKxBQVNMJ4dhDnWs5tpQRWbetEvdOKoVKYJyN5pWA
        SO9RpiCcl5PJaNeCIThCI0ZQ3swPudZIOiATQ6a+4Q+AvrWuWIu7yoxrZOrF00/A
        tsbq1jyTVJlM7vDk0HzXpqF3Ew7PxbUH4osU7jEO8zORFpjVIsKenGrNmjanQ2Nv
        xP9OlTu0kGh8ALZVsiKgdBSWYKjHv1uCZmRwu8gNzdkG14Ir2vEenI2ukJJgO4x3
        B8+a9s/4mTV66IoGt+IiJNQAN+gsUjQf8MscbjvGvXFMK3TksDPc8SzBQlYS+rEj
        gHBnSrvpcvO/OX34EA/PF935uEwP9s3B1LNwiEr1zny8p6SMfXhDJSWq/HfzBaSZ
        /QjetkaqxYoGF81fLlCTgj8mhXmSkJP/5qjEpqSqCE3MsdnuHLjR3QUUbPr6PX0A
        EYiMpi6ypbP5MManFquhYEEwB3OafBWOJ6x+7cjPgQIhAgMBAAE=
        -----END PUBLIC KEY-----
        """;

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
    public void Pack_SignatureVerifiesWithDotNetRsa()
    {
        var task = this.CreateTask();
        task.PluginSigningKey = PrivateKey;

        Assert.IsTrue(task.Execute());

        using var archive = ZipFile.OpenRead(task.PackedFile);
        var signature = ReadEntry(archive, "obsidian.sig");
        var digest = ComputeDigestLikeObsidian(archive);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(PublicKey);

        Assert.IsTrue(
            rsa.VerifyData(digest, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
            "Signature does not verify over the archive digest.");

        digest[0] ^= 0xFF;
        Assert.IsFalse(rsa.VerifyData(digest, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
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

    /// <summary>
    /// The digest Obsidian's reader verifies: SHA-384 over every entry except obsidian.sig in ordinal path order,
    /// each as UTF-8 path, a zero byte and the SHA-384 of its uncompressed contents.
    /// </summary>
    private static byte[] ComputeDigestLikeObsidian(ZipArchive archive)
    {
        using var buffer = new MemoryStream();

        var entries = archive.Entries
            .Where(entry => entry.FullName != "obsidian.sig")
            .OrderBy(entry => entry.FullName, StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            buffer.Write(Encoding.UTF8.GetBytes(entry.FullName));
            buffer.WriteByte(0);
            buffer.Write(SHA384.HashData(ReadEntry(archive, entry.FullName)));
        }

        return SHA384.HashData(buffer.ToArray());
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
