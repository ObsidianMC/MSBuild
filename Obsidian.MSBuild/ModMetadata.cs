using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Obsidian.MSBuild
{
    /// <summary>
    /// The contents of obsidian.mod.json, the metadata entry of a version 2 .obby archive.
    /// ModId, Version and Assembly are required; the other fields are omitted from the JSON when null.
    /// </summary>
    public sealed class ModMetadata
    {
        public string ModId { get; set; }

        public string DisplayName { get; set; }

        public string Version { get; set; }

        public string Assembly { get; set; }

        public string ApiVersion { get; set; }

        public string[] Authors { get; set; } = Array.Empty<string>();

        public string Description { get; set; }

        public string ProjectUrl { get; set; }

        /// <summary>both, server or client.</summary>
        public string Side { get; set; }

        public PluginDependency[] Dependencies { get; set; } = Array.Empty<PluginDependency>();

        /// <summary>
        /// Serializes the metadata as the camelCase JSON document Obsidian reads.
        /// Written by hand because the task targets netstandard2.0 and must not pull in a JSON package.
        /// </summary>
        public string ToJson()
        {
            var members = new List<string>();

            AddMember(members, "modId", this.ModId);
            AddMember(members, "displayName", this.DisplayName);
            AddMember(members, "version", this.Version);
            AddMember(members, "assembly", this.Assembly);
            AddMember(members, "apiVersion", this.ApiVersion);
            members.Add("\"authors\": " + JsonArray(this.Authors.Select(Quote)));
            AddMember(members, "description", this.Description);
            AddMember(members, "projectUrl", this.ProjectUrl);
            AddMember(members, "side", this.Side);
            members.Add("\"dependencies\": " + JsonArray(this.Dependencies.Select(DependencyToJson)));

            return "{\n  " + string.Join(",\n  ", members) + "\n}\n";
        }

        private static string DependencyToJson(PluginDependency dependency)
        {
            var members = new List<string>();

            AddMember(members, "modId", dependency.ModId);
            AddMember(members, "type", dependency.Type);
            AddMember(members, "versionRange", dependency.VersionRange);
            AddMember(members, "ordering", dependency.Ordering);
            AddMember(members, "side", dependency.Side);

            return "{ " + string.Join(", ", members) + " }";
        }

        private static void AddMember(List<string> members, string name, string value)
        {
            if (value != null)
                members.Add(Quote(name) + ": " + Quote(value));
        }

        private static string JsonArray(IEnumerable<string> items)
        {
            return "[" + string.Join(", ", items) + "]";
        }

        /// <summary>Quotes a string as a JSON string literal, escaping everything RFC 8259 requires.</summary>
        private static string Quote(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');

            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            builder.Append(c);

                        break;
                }
            }

            builder.Append('"');

            return builder.ToString();
        }
    }
}
