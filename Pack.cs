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
        //WHen should we start compressing?
        public int MinCompressionSize = 1024;

        [Required]
        public string PublishDir { get; set; }

        [Required]
        public string ApiVersion { get; set; }

        [Required]
        public string PluginName { get; set; }

        [Required]
        public string PluginVersion { get; set; }

        public string SigningKey { get; set; }

        private byte[] GetData(FileInfo file)
        {
            using (var fileStream = file.OpenRead())
            {
                var data = new byte[fileStream.Length];

                fileStream.Read(data, 0, data.Length);

                return data;
            }
        }

        private void Add(List<Entry> entries, FileInfo file)
        {
            var data = GetData(file);

            var currentLength = data.Length;

            if (currentLength > MinCompressionSize)
            {
                using (var ms = new MemoryStream(data.Length))
                using (var deflateSteam = new DeflateStream(ms, CompressionMode.Compress))
                {
                    deflateSteam.Write(data, 0, data.Length);

                    data = ms.ToArray();
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

        public override bool Execute()
        {
            this.Log.LogMessage(MessageImportance.High, "------ Starting Plugin Packer ------");
            var files = Directory.GetFiles(this.PublishDir)
                .Select(x => new FileInfo(x));

            this.Log.LogMessage(MessageImportance.High, "Gathering entries...");
            var entries = new List<Entry>();

            foreach (var file in files)
            {
                this.Add(entries, file);
                file.Delete();
            }

            this.Log.LogMessage(MessageImportance.High, "Entries gathered. Starting packing process..");

            var filePath = Path.Combine(this.PublishDir, $"{this.PluginName}.obby");
            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var fs = new FileStream(filePath, FileMode.CreateNew))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(Encoding.ASCII.GetBytes("OBBY"));
                writer.Write(this.ApiVersion);

                this.Log.LogMessage(MessageImportance.High, "Header and ApiVersion written.");

                var preHeaderStartPos = fs.Position;
                writer.Write(new byte[280]);

                var dataStartPos = fs.Position;

                this.Log.LogMessage(MessageImportance.High,
                    "{0}:{1}", this.PluginName, this.PluginVersion);
                writer.Write(this.PluginName);
                writer.Write(this.PluginVersion);


                this.Log.LogMessage(MessageImportance.High, "Writing entry headers. ({0})", entries.Count);
                writer.Write(entries.Count);
                foreach (var entry in entries)
                {
                    writer.Write(entry.Name);
                    writer.Write(entry.Length);
                    writer.Write(entry.CompressedLength);
                }

                this.Log.LogMessage(MessageImportance.High, "Writing entries. ({0})", entries.Count);
                foreach (var entry in entries)
                {
                    writer.Write(entry.Data);
                }

                // set the hash
                fs.Position = dataStartPos;
                var hash = SHA1.Create().ComputeHash(fs);

                this.Log.LogMessage(MessageImportance.High, "Packed Hash: ({0})",
                    BitConverter.ToString(hash).Replace("-", string.Empty));

                fs.Position = preHeaderStartPos;
                writer.Write(hash);

                if (!string.IsNullOrEmpty(this.SigningKey))
                {
                    //Write signature if there's a key
                    using (var rsa = AsymmetricAlgorithm.Create("RSA"))
                    {
                        rsa.FromXmlString(this.SigningKey);

                        var formatter = new RSAPKCS1SignatureFormatter(rsa);

                        formatter.SetHashAlgorithm("SHA1");
                        var sig = formatter.CreateSignature(hash);

                        writer.Write(sig);
                    }
                }
                else
                {
                    fs.Seek(256, SeekOrigin.Current);
                }

                // write data length after hash
                var dataLength = (int)(fs.Length - dataStartPos);
                this.Log.LogMessage(MessageImportance.High, "Data Length: ({0})", dataLength);
                writer.Write(dataLength);

                this.Log.LogMessage(MessageImportance.High, "Plugin successfully packed at {0}", filePath);
            }

            return true;
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
