using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommandLine.Utility;

namespace JSPack
{
    /// <summary>
    /// Represents arguments parsed from the map and/or command line.
    /// </summary>
    public class MapArguments
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Command line arguments to use when parsing the arguments.</param>
        /// <param name="loader">The loader containing the map to use when parsing the arguments.</param>
        public MapArguments(Arguments args, MapLoader loader)
        {
            ParseMap(args, loader);
        }

        /// <summary>
        /// Gets a value indicating whether this instance's arguments are valid.
        /// </summary>
        public bool ArgumentsAreValid { get; private set; }

        /// <summary>
        /// Gets the reason this instance's arguments are invalid, if applicable.
        /// </summary>
        public string ArgumentsInvalidReason { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to perform the actions set on outputs in the map.
        /// </summary>
        public bool OutputActions { get; private set; }

        /// <summary>
        /// Gets the fully qualified path to the map's input sources.
        /// </summary>
        public string SourcePath { get; private set; }

        /// <summary>
        /// Gets the fully qualified path to the map's output targets.
        /// </summary>
        public string TargetPath { get; private set; }

        /// <summary>
        /// Gets the version value to use when creating output filenames.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Parses a map and arguments set.
        /// </summary>
        /// <param name="args">The command line arguments to use while parsing.</param>
        /// <param name="loader">The loader containing the map to use while parsing.</param>
        private void ParseMap(Arguments args, MapLoader loader)
        {
            ArgumentsAreValid = true;
            ArgumentsInvalidReason = "The script map arguments are invalid.\n";

            string mapDir = Path.GetDirectoryName(loader.Path);

            SourcePath = args["src"] ?? String.Empty;

            if (String.IsNullOrEmpty(SourcePath) && loader.Map.DocumentElement.Attributes["src"] != null)
            {
                SourcePath = loader.Map.DocumentElement.Attributes["src"].Value;
            }

            if (String.IsNullOrEmpty(SourcePath))
            {
                SourcePath = @".\";
            }

            if (!Path.IsPathRooted(SourcePath))
            {
                SourcePath = Path.GetFullPath(Path.Combine(mapDir, SourcePath));
            }

            TargetPath = args["target"] ?? String.Empty;

            if (String.IsNullOrEmpty(TargetPath) && loader.Map.DocumentElement.Attributes["target"] != null)
            {
                TargetPath = loader.Map.DocumentElement.Attributes["target"].Value;
            }

            if (String.IsNullOrEmpty(TargetPath))
            {
                TargetPath = @".\";
            }

            if (!Path.IsPathRooted(TargetPath))
            {
                TargetPath = Path.GetFullPath(Path.Combine(mapDir, TargetPath));
            }

            Version = args["version"] ?? String.Empty;

            if (String.IsNullOrEmpty(Version))
            {
                Version = loader.Map.DocumentElement.Attributes["version"].Value;
            }

            string actions = args["actions"] ?? String.Empty;

            if (String.IsNullOrEmpty(actions))
            {
                actions = loader.Map.DocumentElement.Attributes["actions"].Value;
            }

            OutputActions = true;

            try
            {
                OutputActions = Convert.ToBoolean(actions);
            }
            catch
            {
                ArgumentsInvalidReason += "Output actions must be either \"true\" or \"false\".\n";
                ArgumentsAreValid = false;
            }
        }
    }
}
