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
            byte[]? signature = null;
            if (signed)
            {
                var length = reader.ReadInt32();
                signature = reader.ReadBytes(length);
            }

            var dataLength = reader.ReadInt32();
            var dataPos = file.Position;

            using (var sha384 = SHA384.Create())
            {
                var verifyHash = sha384.ComputeHash(file);
                var hashString = Convert.ToHexString(verifyHash);

                Assert.IsTrue(verifyHash.SequenceEqual(hash), $"Expected {Convert.ToHexString(hash)} got {hashString}");
            }

            if(signed)
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicKey);

                var verified = rsa.VerifyData(hash, signature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);

                Assert.IsTrue(verified, "Invalid signature");
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

            var testEntries = new Dictionary<string, (string, int length, int compressedLength)>();

            for(var i = 0; i < entryCount; i++)
            {
                var entryName = reader.ReadString();
                var entryLength = reader.ReadInt32();
                var entryCompressedLength = reader.ReadInt32();

                testEntries[entryName] = (entryName, entryLength, entryCompressedLength);
            }

            foreach(var (_, entry) in testEntries)
            {
                var data = new byte[entry.compressedLength];

                var bytesRead = reader.Read(data);

                Assert.AreEqual(data.Length, bytesRead);
            }

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