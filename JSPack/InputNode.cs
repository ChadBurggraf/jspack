

namespace JSPack
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents an input node.
    /// </summary>
    public sealed class InputNode : Node
    {
        /// <summary>
        /// Initializes a new instance of the InputNode class.
        /// </summary>
        /// <param name="context">The map context to initialize the node with.</param>
        /// <param name="path">The path of the input file.</param>
        public InputNode(MapContext context, string path)
            : base(context)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "path must contain a value.");
            }

            this.Path = path;
            this.ResolvedPath = ResolvePath(context, path);

            if (!File.Exists(this.ResolvedPath))
            {
                throw new FileNotFoundException("The specified input path does not exist.", this.ResolvedPath);
            }

            this.PathInfo = new FileInfo(this.ResolvedPath);
            this.IsDirty = this.PathInfo.LastWriteTimeUtc > context.DocumentInfo.LastWriteTimeUtc;
        }

        /// <summary>
        /// Gets the original path this instance was constructed with.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets this instance's source file's <see cref="FileInfo"/>.
        /// </summary>
        public FileInfo PathInfo { get; private set; }

        /// <summary>
        /// Gets the resolved path this instance refers to.
        /// </summary>
        public string ResolvedPath { get; private set; }

        /// <summary>
        /// Resolves an input file path.
        /// </summary>
        /// <param name="context">The map context to resolve the path with.</param>
        /// <param name="path">The path to resolve.</param>
        /// <returns>The resolved path.</returns>
        public static string ResolvePath(MapContext context, string path)
        {
            if (!System.IO.Path.IsPathRooted(path))
            {
                path = System.IO.Path.Combine(context.SourceDirectory, path);
            }

            return path;
        }
    }
}
