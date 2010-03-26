using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace JSPack
{
    /// <summary>
    /// Performs packing.
    /// </summary>
    public class Packer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="loader">The loader containing the map file to pack.</param>
        /// <param name="arguments">The arguments defining the packing behavior.</param>
        public Packer(MapLoader loader, MapArguments arguments)
        {
            Loader = loader;
            Arguments = arguments;
        }

        /// <summary>
        /// Gets the arguments to use while packing.
        /// </summary>
        public MapArguments Arguments { get; private set; }

        /// <summary>
        /// Gets the loader containing the map file defining the pack.
        /// </summary>
        public MapLoader Loader { get; private set; }

        /// <summary>
        /// Runs packing on this instance's map.
        /// </summary>
        /// <param name="standardOutput">The stream to write standard output messages to.</param>
        /// <param name="standardError">The stream to write standard error messages to.</param>
        public void Pack(TextWriter standardOutput, TextWriter standardError)
        {
            string prevDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(this.Loader.Path));

            try
            {
                List<string> temp = new List<string>();
                Dictionary<string, string> named = new Dictionary<string, string>();
                XmlNodeList outputs = Loader.Map.SelectNodes("jspack/output");

                for (int i = 0; i < outputs.Count; i++)
                {
                    XmlElement output = outputs[i] as XmlElement;
                    string info = String.Format("Concatenating output {0} of {1}", i + 1, outputs.Count);
                    string outputName = output.Attributes["name"].Value;
                    string outputPath = output.Attributes["path"].Value;

                    if (!String.IsNullOrEmpty(outputName))
                    {
                        info += " (" + outputName + ").";
                    }
                    else if (!String.IsNullOrEmpty(outputPath))
                    {
                        info += " (" + outputPath + ").";
                    }
                    else
                    {
                        info += ".";
                    }

                    standardOutput.WriteLine(info);

                    bool success = true;
                    string path = ConcatentateOutput(named, Arguments.SourcePath, output);
                    temp.Add(path);

                    if (Arguments.OutputActions && Convert.ToBoolean(output.Attributes["actions"].Value))
                    {
                        foreach (OutputAction action in ResolveOutputActions(Loader, output))
                        {
                            info = String.Concat("Executing action ", action.Name);

                            if (!String.IsNullOrEmpty(outputName))
                            {
                                info += " on " + outputName + ".";
                            }
                            else if (!String.IsNullOrEmpty(outputPath))
                            {
                                info += " on " + outputPath + ".";
                            }
                            else
                            {
                                info += ".";
                            }

                            standardOutput.WriteLine(info);
                            string tempPath = Path.GetTempFileName();

                            using (FileStream inputStream = File.OpenRead(path))
                            {
                                using (FileStream outputStream = File.OpenWrite(tempPath))
                                {
                                    string reason;
                                    if (!action.Execute(inputStream, outputStream, out reason))
                                    {
                                        standardError.WriteLine(reason);
                                        success = false;
                                        break;
                                    }
                                }
                            }

                            File.Delete(path);
                            File.Move(tempPath, path);
                        }
                    }

                    if (success)
                    {
                        if (!Convert.ToBoolean(output.Attributes["temporary"].Value))
                        {
                            string op = ResolveOutputPath(Arguments.TargetPath, Arguments.Version, output);
                            string od = Path.GetDirectoryName(op);

                            if (!Directory.Exists(od))
                            {
                                Directory.CreateDirectory(od);
                            }

                            File.Copy(path, op, true);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (string tempPath in temp)
                {
                    File.Delete(tempPath);
                }

                standardOutput.WriteLine();
            }
            finally
            {
                Directory.SetCurrentDirectory(prevDir);
            }
        }

        /// <summary>
        /// Concatenates an output into a single file.
        /// </summary>
        /// <param name="named">A collection of named outputs to add to if applicable.</param>
        /// <param name="src">The source directory to read inputs from.</param>
        /// <param name="output">The output definition element.</param>
        /// <returns>The path of the created output file.</returns>
        private static string ConcatentateOutput(Dictionary<string, string> named, string src, XmlElement output)
        {
            string path = Path.GetTempFileName();

            using (FileStream fs = new FileStream(path, FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (!String.IsNullOrEmpty(output.Attributes["name"].Value))
                    {
                        named.Add(output.Attributes["name"].Value, path);
                    }

                    foreach (XmlElement import in output.SelectNodes("import"))
                    {
                        sw.WriteLine(File.ReadAllText(named[import.Attributes["name"].Value]));
                    }

                    foreach (XmlElement input in output.SelectNodes("input"))
                    {
                        sw.WriteLine(File.ReadAllText(ResolveInputPath(src, input)));
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// Resolves a fully qualified input path defined for an input node.
        /// </summary>
        /// <param name="src">The source directory defined by the current map context.</param>
        /// <param name="input">The input node to get the relative path part from.</param>
        /// <returns>A fully qualified input path.</returns>
        private static string ResolveInputPath(string src, XmlElement input)
        {
            string path = input.Attributes["path"].Value;

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(src, path);
            }

            return path;
        }

        /// <summary>
        /// Resolves the output actions for the given output.
        /// </summary>
        /// <param name="loader">The loader containing the map file definition.</param>
        /// <param name="output">The output to resolve actions for.</param>
        /// <returns>An output's actions collection.</returns>
        private static IEnumerable<OutputAction> ResolveOutputActions(MapLoader loader, XmlElement output)
        {
            Dictionary<string, OutputAction> actions = new Dictionary<string, OutputAction>();

            foreach (XmlElement element in loader.Map.SelectNodes("jspack/outputAction[@global='true']"))
            {
                string name = element.Attributes["name"].Value;
                actions[name] = new OutputAction(name, element.Attributes["executable"].Value, element.Attributes["arguments"].Value);
            }

            foreach (XmlElement element in output.SelectNodes("action"))
            {
                string name = element.Attributes["name"].Value;
                string args = element.Attributes["arguments"].Value;

                if (actions.ContainsKey(name))
                {
                    if (!String.IsNullOrEmpty(args))
                    {
                        actions[name].Arguments = args;
                    }
                }
                else
                {
                    XmlElement action = loader.Map.SelectSingleNode("jspack/outputAction[@name='" + name + "']") as XmlElement;

                    if (action != null)
                    {
                        if (String.IsNullOrEmpty(args))
                        {
                            args = action.Attributes["arguments"].Value;
                        }

                        actions[name] = new OutputAction(name, action.Attributes["executable"].Value, args);
                    }
                }
            }

            return (from kvp in actions
                    select kvp.Value).ToArray();
        }

        /// <summary>
        /// Resolves a fully qualified output path defined for an output node.
        /// </summary>
        /// <param name="target">The target directory defined by the current map context.</param>
        /// <param name="version">The version number defined by the current map context.</param>
        /// <param name="output">The output node to get the relative path part from.</param>
        /// <returns>A fully qualified output path.</returns>
        private static string ResolveOutputPath(string target, string version, XmlElement output)
        {
            string path = output.Attributes["path"].Value;

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(target, path);
            }

            if (!String.IsNullOrEmpty(version) && Convert.ToBoolean(output.Attributes["version"].Value))
            {
                string directory = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);

                path = Path.Combine(directory, String.Concat(
                    fileName,
                    "-",
                    version,
                    ext
                ));
            }

            return path;
        }
    }
}
