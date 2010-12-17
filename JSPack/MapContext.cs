

namespace JSPack
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Represents a map context within which outputs are transformed.
    /// </summary>
    public sealed class MapContext
    {
        /// <summary>
        /// Initializes a new instance of the MapContext class.
        /// </summary>
        /// <param name="documentPath">The path of the map to initialze the context for.</param>
        /// <param name="document">The map's loaded XML document.</param>
        /// <param name="overrides">A set of overrides to use when setting up the context.</param>
        public MapContext(string documentPath, XmlDocument document, IDictionary<string, string> overrides)
        {
            if (String.IsNullOrEmpty(documentPath))
            {
                throw new ArgumentNullException("documentPath", "documentPath must contain a value.");
            }

            if (!Path.IsPathRooted(documentPath))
            {
                throw new ArgumentException("documentPath must be a fully qualified path.", "documentPath");
            }

            if (!File.Exists(documentPath))
            {
                throw new FileNotFoundException("The specified document path does not exist.", documentPath);
            }

            if (document == null)
            {
                throw new ArgumentNullException("document", "document cannot be null.");
            }

            this.Document = document;
            this.DocumentPath = documentPath;

            overrides = overrides ?? new Dictionary<string, string>();
            string directory = Path.GetDirectoryName(documentPath);

            this.SourceDirectory = ResolveDirectory("src", directory, overrides, document);
            this.TargetDirectory = ResolveDirectory("target", directory, overrides, document);
            this.Version = GetOverride("version", overrides);

            if (String.IsNullOrEmpty(this.Version) && document.DocumentElement.Attributes["version"] != null)
            {
                this.Version = document.DocumentElement.Attributes["version"].Value;
            }

            string actions = GetOverride("actions", overrides);

            if (String.IsNullOrEmpty(actions))
            {
                if (document.DocumentElement.Attributes["actions"] != null)
                {
                    actions = document.DocumentElement.Attributes["actions"].Value;
                }
                else
                {
                    actions = "false";
                }
            }

            this.EnableOutputActions = Convert.ToBoolean(actions, CultureInfo.InvariantCulture);
            this.DocumentInfo = new FileInfo(documentPath);
        }

        /// <summary>
        /// Gets a value indicating whether output actions are enabled.
        /// </summary>
        public bool EnableOutputActions { get; private set; }

        /// <summary>
        /// Gets a reference to the source map document.
        /// </summary>
        public XmlDocument Document { get; private set; }

        /// <summary>
        /// Gets this instance's source document's <see cref="FileInfo"/>.
        /// </summary>
        public FileInfo DocumentInfo { get; private set; }

        /// <summary>
        /// Gets the fully qualified path where the source map document is located.
        /// </summary>
        public string DocumentPath { get; private set; }

        /// <summary>
        /// Gets the fully qualified path to use as the source directory.
        /// </summary>
        public string SourceDirectory { get; private set; }

        /// <summary>
        /// Gets the fully qualified path to use as the target directory.
        /// </summary>
        public string TargetDirectory { get; private set; }
    
        /// <summary>
        /// Gets the version string to use for outputs, if applicable.
        /// </summary>
        public string Version { get; private set; }
    
        /// <summary>
        /// Gets the value of an override with the given key from the given overrides collection.
        /// </summary>
        /// <param name="key">The key to get the override value for.</param>
        /// <param name="overrides">A collection of overrides.</param>
        /// <returns>An override value, or <see cref="String.Empty"/> if none was found.</returns>
        public static string GetOverride(string key, IDictionary<string, string> overrides)
        {
            string result = null;

            if (overrides.ContainsKey(key))
            {
                result = overrides[key];
            }

            return (result ?? String.Empty).Trim();
        }

        /// <summary>
        /// Resolves the directory for the given argument key.
        /// </summary>
        /// <param name="key">The key to resolve the directory for.</param>
        /// <param name="baseDirectory">The base directory to resolve relative to.</param>
        /// <param name="overrides">A collection of overrides.</param>
        /// <param name="document">The source map document.</param>
        /// <returns>The key's fully qualified path.</returns>
        public static string ResolveDirectory(string key, string baseDirectory, IDictionary<string, string> overrides, XmlDocument document)
        {
            string path = GetOverride(key, overrides);

            if (String.IsNullOrEmpty(path) && document.DocumentElement.Attributes[key] != null)
            {
                path = document.DocumentElement.Attributes[key].Value;
            }

            if (String.IsNullOrEmpty(path))
            {
                path = @".\";
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(baseDirectory, path));
            }

            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }
    }
}
