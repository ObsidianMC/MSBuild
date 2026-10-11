using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Org.BouncyCastle.Crypto;
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
    /// <summary>
    /// Packs a published plugin into a version 2 .obby archive: a ZIP holding obsidian.mod.json, the publish
    /// folder's top-level files, optional assets/ and data/ trees and an optional obsidian.sig signature.
    /// </summary>
    public sealed class Pack : Task
    {
        public const string MetadataEntryName = "obsidian.mod.json";
        public const string SignatureEntryName = "obsidian.sig";

        /// <summary>Entries smaller than this are stored instead of deflated.</summary>
        public const int MinCompressionSize = 1024;

        private static readonly string[] Sides = { "both", "server", "client" };
        private static readonly string[] DependencyTypes = { "required", "optional", "incompatible", "discouraged" };
        private static readonly string[] Orderings = { "none", "before", "after" };

        [Required]
        public string PluginPublishDir { get; set; }

        [Required]
        public string PluginAssembly { get; set; }

        [Required]
        public string PluginVersion { get; set; }

        [Required]
        public string PluginId { get; set; }

        public string PluginName { get; set; }

        public string PluginApiVersion { get; set; }

        /// <summary>Comma-separated author names.</summary>
        public string PluginAuthors { get; set; }

        public string PluginDescription { get; set; }

        public string ProjectUrl { get; set; }

        /// <summary>both (default), server or client.</summary>
        public string PluginSide { get; set; }

        /// <summary>
        /// Dependency items. The mod id comes from the Id metadata or the item spec; Version/VersionRange, Type,
        /// Ordering and Side map to the JSON fields. Required=true/false is accepted as type required/optional.
        /// </summary>
        public ITaskItem[] PluginDependencies { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// A ready-made obsidian.mod.json to pack as-is instead of generating one from the properties above.
        /// Used only when the file exists.
        /// </summary>
        public string PluginMetadataFile { get; set; }

        /// <summary>Folder packed under assets/ when it exists.</summary>
        public string PluginAssetsDir { get; set; }

        /// <summary>Folder packed under data/ when it exists.</summary>
        public string PluginDataDir { get; set; }

        /// <summary>
        /// RSA private key in PEM format ("PRIVATE KEY" or "RSA PRIVATE KEY"), given as a file path or the
        /// PEM text itself. The archive is signed only when this is set.
        /// </summary>
        public string PluginSigningKey { get; set; }

        [Output]
        public string PackedFile { get; set; }

        public override bool Execute()
        {
            var metadata = this.LoadMetadata();
            if (metadata == null)
                return false;

            AsymmetricKeyParameter signingKey = null;
            if (!string.IsNullOrEmpty(this.PluginSigningKey))
            {
                signingKey = this.LoadSigningKey();
                if (signingKey == null)
                    return false;
            }

            var entries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            entries[MetadataEntryName] = metadata;
            this.AddPublishFiles(entries);
            this.AddTree(entries, this.PluginAssetsDir, "assets");
            this.AddTree(entries, this.PluginDataDir, "data");

            var filePath = Path.Combine(this.PluginPublishDir, this.PluginAssembly + ".obby");
            if (File.Exists(filePath))
                File.Delete(filePath);

            this.Log.LogMessage(MessageImportance.High, "Packing {0} entries into {1}", entries.Count, filePath);

            using (var stream = new FileStream(filePath, FileMode.CreateNew))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (var entry in entries)
                {
                    this.Log.LogMessage(MessageImportance.Low, "Entry {0} ({1} bytes)", entry.Key, entry.Value.Length);
                    WriteEntry(archive, entry.Key, entry.Value);
                }

                if (signingKey != null)
                {
                    var signature = Sign(ComputeDigest(entries), signingKey);
                    WriteEntry(archive, SignatureEntryName, signature);
                    this.Log.LogMessage(MessageImportance.High, "Signed with a {0}-byte signature.", signature.Length);
                }
            }

            this.PackedFile = filePath;
            this.Log.LogMessage(MessageImportance.High, "Plugin packed at {0}", filePath);

            return true;
        }

        /// <summary>
        /// Computes the digest the signature covers: SHA-384 over every entry except obsidian.sig in ordinal
        /// path order, each contributing its UTF-8 path, a zero byte and the SHA-384 hash of its contents.
        /// This must match Obsidian's verifier byte for byte.
        /// </summary>
        public static byte[] ComputeDigest(IEnumerable<KeyValuePair<string, byte[]>> entries)
        {
            var sorted = entries
                .Where(entry => entry.Key != SignatureEntryName)
                .OrderBy(entry => entry.Key, StringComparer.Ordinal);

            using (var sha384 = SHA384.Create())
            using (var buffer = new MemoryStream())
            {
                foreach (var entry in sorted)
                {
                    var path = Encoding.UTF8.GetBytes(entry.Key);
                    buffer.Write(path, 0, path.Length);
                    buffer.WriteByte(0);

                    var hash = sha384.ComputeHash(entry.Value);
                    buffer.Write(hash, 0, hash.Length);
                }

                return sha384.ComputeHash(buffer.ToArray());
            }
        }

        /// <summary>
        /// Signs the digest with SHA384withRSA (PKCS #1 v1.5). The signer hashes the digest bytes again, which
        /// is what RSA.VerifyData(digest, ..., SHA384, Pkcs1) expects on the reading side.
        /// </summary>
        private static byte[] Sign(byte[] digest, AsymmetricKeyParameter key)
        {
            var signer = SignerUtilities.GetSigner("SHA384WITHRSA");
            signer.Init(true, key);
            signer.BlockUpdate(digest, 0, digest.Length);

            return signer.GenerateSignature();
        }

        private static void WriteEntry(ZipArchive archive, string path, byte[] data)
        {
            var level = data.Length < MinCompressionSize ? CompressionLevel.NoCompression : CompressionLevel.Optimal;
            var entry = archive.CreateEntry(path, level);

            using (var entryStream = entry.Open())
                entryStream.Write(data, 0, data.Length);
        }

        private void AddPublishFiles(SortedDictionary<string, byte[]> entries)
        {
            var packedFileName = this.PluginAssembly + ".obby";

            foreach (var file in Directory.GetFiles(this.PluginPublishDir))
            {
                var name = Path.GetFileName(file);

                if (name.StartsWith("testhost", StringComparison.Ordinal)
                    || name == packedFileName
                    || name == MetadataEntryName
                    || name == SignatureEntryName)
                {
                    continue;
                }

                entries[name] = File.ReadAllBytes(file);
            }
        }

        /// <summary>Adds every file under <paramref name="directory"/> as prefix/relative/path.</summary>
        private void AddTree(SortedDictionary<string, byte[]> entries, string directory, string prefix)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                entries[prefix + "/" + relative] = File.ReadAllBytes(file);
            }
        }

        /// <summary>Returns the obsidian.mod.json bytes, or null after logging an error.</summary>
        private byte[] LoadMetadata()
        {
            var candidates = new[]
            {
                this.PluginMetadataFile,
                Path.Combine(this.PluginPublishDir, MetadataEntryName)
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    this.Log.LogMessage(MessageImportance.High, "Using metadata from {0}", candidate);

                    return File.ReadAllBytes(candidate);
                }
            }

            var metadata = this.BuildMetadata();
            if (metadata == null)
                return null;

            return new UTF8Encoding(false).GetBytes(metadata.ToJson());
        }

        private ModMetadata BuildMetadata()
        {
            var side = NormalizeChoice(this.PluginSide, "both");
            if (!this.ValidateChoice("PluginSide", side, Sides))
                return null;

            var authors = (this.PluginAuthors ?? string.Empty)
                .Split(',')
                .Select(author => author.Trim())
                .Where(author => author.Length > 0)
                .ToArray();

            var dependencies = new List<PluginDependency>();
            foreach (var item in this.PluginDependencies ?? Array.Empty<ITaskItem>())
            {
                var dependency = this.BuildDependency(item);
                if (dependency == null)
                    return null;

                dependencies.Add(dependency);
            }

            return new ModMetadata
            {
                ModId = this.PluginId,
                DisplayName = EmptyToNull(this.PluginName),
                Version = this.PluginVersion,
                Assembly = this.PluginAssembly,
                ApiVersion = EmptyToNull(this.PluginApiVersion),
                Authors = authors,
                Description = EmptyToNull(this.PluginDescription),
                ProjectUrl = EmptyToNull(this.ProjectUrl),
                Side = side,
                Dependencies = dependencies.ToArray()
            };
        }

        private PluginDependency BuildDependency(ITaskItem item)
        {
            var modId = EmptyToNull(item.GetMetadata("Id")) ?? item.ItemSpec;
            if (string.IsNullOrEmpty(modId))
            {
                this.Log.LogError("A PluginDependency item needs a mod id in its Include or Id metadata.");

                return null;
            }

            var type = NormalizeChoice(item.GetMetadata("Type"), null);
            var required = item.GetMetadata("Required");
            if (type == null && !string.IsNullOrEmpty(required))
                type = bool.Parse(required) ? "required" : "optional";

            var versionRange = EmptyToNull(item.GetMetadata("VersionRange")) ?? EmptyToNull(item.GetMetadata("Version"));
            var ordering = NormalizeChoice(item.GetMetadata("Ordering"), null);
            var side = NormalizeChoice(item.GetMetadata("Side"), null);

            var valid = this.ValidateChoice(modId + " Type", type, DependencyTypes)
                && this.ValidateChoice(modId + " Ordering", ordering, Orderings)
                && this.ValidateChoice(modId + " Side", side, Sides);

            return valid ? new PluginDependency(modId, type, versionRange, ordering, side) : null;
        }

        private bool ValidateChoice(string name, string value, string[] allowed)
        {
            if (value == null || allowed.Contains(value))
                return true;

            this.Log.LogError("{0} must be one of {1}, not '{2}'.", name, string.Join(", ", allowed), value);

            return false;
        }

        private static string NormalizeChoice(string value, string fallback)
        {
            return EmptyToNull(value)?.Trim().ToLowerInvariant() ?? fallback;
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>Loads the RSA private key from PluginSigningKey, or returns null after logging an error.</summary>
        private AsymmetricKeyParameter LoadSigningKey()
        {
            var pem = this.PluginSigningKey;
            if (File.Exists(pem))
                pem = File.ReadAllText(pem);

            object key;
            try
            {
                using (var reader = new StringReader(pem))
                    key = new PemReader(reader).ReadObject();
            }
            catch (Exception exception)
            {
                this.Log.LogError("PluginSigningKey is not a readable PEM key: {0}", exception.Message);

                return null;
            }

            if (key is AsymmetricCipherKeyPair pair)
                return pair.Private;

            if (key is AsymmetricKeyParameter parameter && parameter.IsPrivate)
                return parameter;

            this.Log.LogError("PluginSigningKey must contain an RSA private key.");

            return null;
        }
    }
}
