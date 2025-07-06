using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Obsidian.MSBuild
{
    public sealed class Pack : Task
    {
        public const int MinCompressionSize = 1024;
        public const float CompressionTradeoff = 0.9f;

        public const int HashSizeInBits = 384;
        public const int HashSizeInBytes = HashSizeInBits / 8;
        public string PluginPublishDir { get; set; }

        [Required]
        public string PluginName { get; set; }

        [Required]
        public string PluginApiVersion { get; set; }

        [Required]
        public string PluginAssembly { get; set; }

        [Required]
        public string PluginVersion { get; set; }

        [Required]
        public string PluginAuthors { get; set; }

        [Required]
        public string PluginId { get; set; }

        public string ProjectUrl { get; set; }

        public string PluginDescription { get; set; }

        public ITaskItem[] PluginDependencies { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Must be in XML format to be loaded and used properly. 
        /// Can be a file or the element directly.
        ///</summary>
        public string PluginSigningKey { get; set; }

        [Output]
        public string PackedFile { get; set; }

        private void WriteLine(MessageImportance importance, string message, params object[] args)
        {
            this.Log.LogMessage(importance, message, args);
            Console.WriteLine(message, args);
        }

        public override bool Execute()
        {
            var dependencies = this.BuildDependencies();
            var shouldSign = !string.IsNullOrEmpty(this.PluginSigningKey);

            var baseHeaderSize = HashSizeInBytes + HashSizeInBits;
            //+5 to account for the boolean and data length
            var headerSize = shouldSign ? baseHeaderSize + 9 : HashSizeInBytes + 5;

            this.WriteLine(MessageImportance.High, "------ Starting Plugin Packer ------");
            var files = Directory.GetFiles(this.PluginPublishDir).Select(x => new FileInfo(x));

            this.WriteLine(MessageImportance.High, "Gathering entries...");
            var entries = new List<Entry>();
            foreach (var file in files)
            {
                if (file.Name.StartsWith("testhost") || file.Name == $"{this.PluginAssembly}.obby")
                    continue;//Skip testhost files

                this.Add(entries, file);
            }

            this.WriteLine(MessageImportance.High, "Entries gathered. Starting packing process..");

            var filePath = Path.Combine(this.PluginPublishDir, $"{this.PluginAssembly}.obby");
            if (File.Exists(filePath))
                File.Delete(filePath);

            this.PackedFile = filePath;

            using (var fs = new FileStream(filePath, FileMode.CreateNew))
            {

                using (var writer = new BinaryWriter(fs))
                {
                    writer.Write(Encoding.ASCII.GetBytes("OBBY"));
                    writer.Write(this.PluginApiVersion);

                    var hashAndSignatureStartPos = fs.Position;
                    writer.Write(new byte[headerSize]);

                    var dataPos = fs.Position;
                    writer.Write(this.PluginAssembly);
                    writer.Write(this.PluginVersion);

                    writer.Write(this.PluginName);
                    writer.Write(this.PluginId);
                    writer.Write(this.PluginAuthors);
                    writer.Write(this.PluginDescription ?? "No description provided");
                    writer.Write(this.ProjectUrl ?? "No project url");

                    writer.Write(dependencies.Length);

                    foreach (var dependency in dependencies)
                    {
                        writer.Write(dependency.Id);
                        writer.Write(dependency.Version);
                        writer.Write(dependency.Required);
                    }

                    this.WriteLine(MessageImportance.High, "Writing entry headers. ({0})", entries.Count);
                    writer.Write(entries.Count);

                    foreach (var entry in entries)
                    {
                        this.Log.LogMessage(MessageImportance.Low, "Entry ({0})", entry.Name);
                        writer.Write(entry.Name);
                        writer.Write(entry.Length);
                        writer.Write(entry.CompressedLength);
                    }

                    this.WriteLine(MessageImportance.High, "Writing entries. ({0})", entries.Count);
                    foreach (var entry in entries)
                        writer.Write(entry.Data);

                    // set the hash
                    fs.Position = dataPos;
                    byte[] hash;

                    using (var sha384 = SHA384.Create())
                        hash = sha384.ComputeHash(fs);

                    fs.Position = hashAndSignatureStartPos;
                    writer.Write(hash);

                    this.WriteLine(MessageImportance.High, "Hash: {0}", BitConverter.ToString(hash).Replace("-", string.Empty));
                    this.WriteLine(MessageImportance.High, "Hash Length: {0}", hash.Length);

                    writer.Write(shouldSign);
                    if (shouldSign)
                    {
                        if (File.Exists(this.PluginSigningKey))
                            this.PluginSigningKey = File.ReadAllText(this.PluginSigningKey);

                        var signer = SignerUtilities.GetSigner("SHA384WITHRSA");
                        using (var reader = new StringReader(this.PluginSigningKey))
                        {
                            var pemReader = new PemReader(reader);
                            var keyPair = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();

                            signer.Init(true, keyPair);

                            signer.BlockUpdate(hash, 0, hash.Length);

                            var sig = signer.GenerateSignature();

                            this.WriteLine(MessageImportance.High, "Signature length: {0}", sig.Length);
                            writer.Write(sig.Length);
                            writer.Write(sig);
                        }
                    }
                    else
                    {
                        this.WriteLine(MessageImportance.High, "No signature.");
                    }

                    // write data length after hash
                    var dataLength = (int)(fs.Length - hashAndSignatureStartPos);
                    this.WriteLine(MessageImportance.High, "Data Length: ({0})", dataLength);
                    writer.Write(dataLength);

                    this.WriteLine(MessageImportance.High, "Plugin successfully packed at {0}", filePath);
                }
            }

            return true;
        }

        private static byte[] GetData(FileInfo file)
        {
            using (var fileStream = file.OpenRead())
            {
                var data = new byte[fileStream.Length];

                fileStream.Read(data, 0, data.Length);

                return data;
            }
        }

        private PluginDependency[] BuildDependencies()
        {
            if (this.PluginDependencies == null)
                return Array.Empty<PluginDependency>();

            var dependencies = new PluginDependency[this.PluginDependencies.Length];

            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependency = this.PluginDependencies[i];
                var id = dependency.GetMetadata("Id");
                var version = dependency.GetMetadata("Version");

                var requiredMetadata = dependency.GetMetadata("Required");
                var required = !string.IsNullOrEmpty(requiredMetadata) && bool.Parse(requiredMetadata);

                if (string.IsNullOrEmpty(id))
                {
                    this.Log.LogWarning("Id is required when defining a dependency.");
                    continue;
                }

                dependencies[i] = new PluginDependency(id, version ?? ">=1.0", required);
            }

            return dependencies;
        }

        private void Add(List<Entry> entries, FileInfo file)
        {
            var data = GetData(file);

            var currentLength = data.Length;

            this.Log.LogMessage( $"------ Packing {file.Name} ------");

            this.Log.LogMessage($"Current file length: {currentLength}.");

            if (currentLength > MinCompressionSize)
            {
                using (var ms = new MemoryStream(data.Length))
                using (var deflateSteam = new DeflateStream(ms, CompressionMode.Compress))
                {
                    deflateSteam.Write(data, 0, data.Length);

                    var compressedData = ms.ToArray();

                    if (compressedData.Length < currentLength * CompressionTradeoff)
                    {
                        data = compressedData;
                        this.Log.LogMessage($"Compressed length: {compressedData.Length}.");
                    }
                }
            }

            entries.Add(new Entry()
            {
                Data = data,
                Name = file.Name,
                Length = currentLength,
                CompressedLength = data.Length,
                FilePath = file.FullName
            });
        }

        private sealed class Entry
        {
            public string Name { get; set; }

            public int Length { get; set; }

            public int CompressedLength { get; set; }

            public byte[] Data { get; set; }

            public string FilePath { get; set; }
        }

    }
}

