using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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

        public string PluginPublishDir { get; set; }

        [Required]
        public required string PluginName { get; set; }

        [Required]
        public required string PluginApiVersion { get; set; }

        [Required]
        public required string PluginAssembly { get; set; }

        [Required]
        public required string PluginVersion { get; set; }

        [Required]
        public required string PluginAuthors { get; set; }

        [Required]
        public required string PluginId { get; set; }

        public string? PluginDescription { get; set; }

        public ITaskItem[] PluginDependencies { get; set; } = [];

        /// <summary>
        /// Must be in XML format to be loaded and used properly. 
        /// Can be a file or the element directly.
        ///</summary>
        public string? PluginSigningKey { get; set; }

        [Output]
        public string PackedFile { get; set; }

        public override bool Execute()
        {
            var dependencies = this.BuildDependencies();
            var shouldSign = !string.IsNullOrEmpty(this.PluginSigningKey);

            //+5 to account for the boolean and data length
            var headerSize = (shouldSign ? SHA384.HashSizeInBits + SHA384.HashSizeInBytes : SHA384.HashSizeInBytes) + 5;

            this.Log.LogMessage("------ Starting Plugin Packer ------");
            var files = Directory.GetFiles(this.PluginPublishDir).Select(x => new FileInfo(x));

            this.Log.LogMessage("Gathering entries...");
            var entries = new List<Entry>();

            foreach (var file in files)
            {
                this.Add(entries, file);
            }

            this.Log.LogMessage("Entries gathered. Starting packing process..");

            var filePath = Path.Combine(this.PluginPublishDir, $"{this.PluginAssembly}.obby");
            if (File.Exists(filePath))
                File.Delete(filePath);

            this.PackedFile = filePath;

            using var fs = new FileStream(filePath, FileMode.CreateNew);
            using var writer = new BinaryWriter(fs);

            writer.Write(Encoding.ASCII.GetBytes("OBBY"));

            writer.Write(this.PluginApiVersion);

            writer.Write(this.PluginAssembly);
            writer.Write(this.PluginVersion);

            writer.Write(this.PluginName);
            writer.Write(this.PluginId);
            writer.Write(this.PluginAuthors);
            writer.Write(this.PluginDescription ?? "No description provided");

            var hashAndSignatureStartPos = fs.Position;
            writer.Write(new byte[headerSize]);

            this.Log.LogMessage("Writing entry headers. ({0})", entries.Count);
            writer.Write(entries.Count);

            foreach (var entry in entries)
            {
                this.Log.LogMessage(MessageImportance.Low, "Entry ({0})", entry.Name);
                writer.Write(entry.Name);
                writer.Write(entry.Length);
                writer.Write(entry.CompressedLength);
            }

            this.Log.LogMessage("Writing entries. ({0})", entries.Count);
            foreach (var entry in entries)
                writer.Write(entry.Data);

            // set the hash
            fs.Position = hashAndSignatureStartPos;
            byte[] hash;

            using (var sha384 = SHA384.Create())
                hash = sha384.ComputeHash(fs);

            this.Log.LogMessage("Packed Hash: ({0})", Convert.ToHexString(hash));

            writer.Write(hash);

            writer.Write(shouldSign);
            if (shouldSign)
            {
                if (File.Exists(this.PluginSigningKey))
                    this.PluginSigningKey = File.ReadAllText(this.PluginSigningKey);

                //Write signature if there's a key
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(this.PluginSigningKey);

                var formatter = new RSAPKCS1SignatureFormatter(ecdsa);

                formatter.SetHashAlgorithm("SHA384");
                var sig = formatter.CreateSignature(hash);

                this.Log.LogMessage("Signature length: {0}", sig.Length);
                writer.Write(sig);
            }
            else
            {
                this.Log.LogMessage("No signature.");
            }

            // write data length after hash
            var dataLength = (int)(fs.Length - hashAndSignatureStartPos);
            this.Log.LogMessage("Data Length: ({0})", dataLength);
            writer.Write(dataLength);

            this.Log.LogMessage("Plugin successfully packed at {0}", filePath);

            return true;
        }

        private static byte[] GetData(FileInfo file)
        {
            using var fileStream = file.OpenRead();
            var data = new byte[fileStream.Length];

            fileStream.ReadExactly(data, 0, data.Length);

            return data;
        }

        private PluginDependency[] BuildDependencies()
        {
            if (this.PluginDependencies == null)
                return [];

            var dependencies = new PluginDependency[this.PluginDependencies.Length];

            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependency = this.PluginDependencies[i];
                var id = dependency.GetMetadata("Id");
                var version = dependency.GetMetadata("Version");

                if (string.IsNullOrEmpty(id))
                {
                    this.Log.LogWarning("Id is required when defining a dependency.");
                    continue;
                }

                dependencies[i] = new PluginDependency(id, version ?? ">=1.0");
            }

            return dependencies;
        }

        private void Add(List<Entry> entries, FileInfo file)
        {
            var data = GetData(file);

            var currentLength = data.Length;

            this.Log.LogMessage(MessageImportance.High, $"------ Packing {file.Name} ------");

            this.Log.LogMessage(MessageImportance.High, $"Current file length: {currentLength}.");

            if (currentLength > MinCompressionSize)
            {
                using var ms = new MemoryStream(data.Length);
                using (var deflateSteam = new DeflateStream(ms, CompressionMode.Compress))
                    deflateSteam.Write(data, 0, data.Length);

                var compressedData = ms.ToArray();

                if (compressedData.Length < currentLength * CompressionTradeoff)
                {
                    data = compressedData;
                    this.Log.LogMessage(MessageImportance.High, $"Compressed length: {compressedData.Length}.");
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

        private readonly struct PluginDependency(string id, string version)
        {
            public string Id { get; } = id;

            public string Version { get; } = version;
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
