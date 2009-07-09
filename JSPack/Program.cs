using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using CommandLine.Utility;

namespace JSPack
{
    public class Program
    {
        #region Constants

        private const string USAGE = "Usage: ScriptBuilder /map:path_to_map [/src:path_to_source /target:path_to_target /version:version_number /minify:true|false]";
        private const string SCHEMA = "JSPack.Map.xsd";
        private static string YUI = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "yuicompressor-2.4.2.jar");

        #endregion

        public static void Main(params string[] args)
        {
            Arguments inputArgs = new Arguments(args);
            string error = String.Empty;
            bool success = true;

            Console.WriteLine();
            DateTime start = DateTime.Now;

            if (!String.IsNullOrEmpty(inputArgs["map"]))
            {
                string map = Path.GetFullPath(inputArgs["map"]);

                if (File.Exists(map))
                {
                    XmlDocument doc = new XmlDocument();

                    if (LoadAndValidateMap(doc, map, out error))
                    {
                        string src, target, version;
                        bool minify;

                        if (ParseArguments(new Arguments(args), map, doc, out src, out target, out version, out minify, out error))
                        {
                            if (Directory.Exists(src))
                            {
                                if (!Directory.Exists(target))
                                {
                                    Directory.CreateDirectory(target);
                                }

                                success = Build(doc, src, target, version, minify, out error);
                            }
                            else
                            {
                                error = String.Format("Source directory \"{0}\" does not exist.", src);
                                success = false;
                            }
                        }
                    }
                    else
                    {
                        success = false;
                    }
                }
                else
                {
                    error = String.Format("Map file \"{0}\" does not exist.", map);
                    success = false;
                }
            }
            else
            {
                error = USAGE;
                success = false;
            }

            if (success)
            {
                Console.WriteLine();
                Console.WriteLine(String.Format("Packing completed successfully in {0:N2} seconds.", DateTime.Now.Subtract(start).TotalSeconds));
            }
            else
            {
                Console.Error.WriteLine(error);
            }
        }

        /// <summary>
        /// Runs the build process.
        /// </summary>
        /// <param name="map">The map document defining the build.</param>
        /// <param name="src">The base source directory.</param>
        /// <param name="target">The base target directory.</param>
        /// <param name="version">The version number to use when versioning output files.</param>
        /// <param name="minify">A value indicating whether to minify output files.</param>
        /// <param name="reason">Contains the reason for failure if applicable.</param>
        /// <returns>True if the build process completed successfully, false otherwise.</returns>
        private static bool Build(XmlDocument map, string src, string target, string version, bool minify, out string reason)
        {
            bool success = true;
            List<string> temp = new List<string>();
            Dictionary<string, string> named = new Dictionary<string, string>();
            
            Process yui = new Process();
            yui.StartInfo.UseShellExecute = false;
            yui.StartInfo.FileName = "java.exe";
            yui.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            yui.StartInfo.RedirectStandardError = true;
            
            reason = String.Empty;

            foreach (XmlElement output in map.SelectNodes("jspack/output"))
            {
                string outputPath = ConcatenateOutput(temp, named, src, target, version, output);

                if (minify && Convert.ToBoolean(output.Attributes["minify"].Value))
                {
                    if (!MinifyOutput(yui, outputPath, out reason))
                    {
                        success = false;
                        break;
                    }
                }
            }

            foreach (string tempFile in temp)
            {
                File.Delete(tempFile);
            }

            return success;
        }

        /// <summary>
        /// Concatenates all of the imports and inputs in an output definition to an output file.
        /// </summary>
        /// <param name="temp">A collection of temporary files to add to if necessary.</param>
        /// <param name="named">A collection of named outputs to add to and/or use if necessary.</param>
        /// <param name="src">The source directory defined by the current map context.</param>
        /// <param name="target">The target directory defined by the current map context.</param>
        /// <param name="version">The version number defined by the current map context.</param>
        /// <param name="output">The output node to concatenate imports and inputs for.</param>
        /// <returns>The path of the concatenated output file.</returns>
        private static string ConcatenateOutput(List<string> temp, Dictionary<string, string> named, string src, string target, string version, XmlElement output)
        {
            string outputPath = GetOutputPath(target, version, output);
            Console.WriteLine(String.Format("Concatenating to {0}", Path.GetFileName(outputPath)));

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (Convert.ToBoolean(output.Attributes["temporary"].Value))
                    {
                        temp.Add(outputPath);
                    }

                    if (!String.IsNullOrEmpty(output.Attributes["name"].Value))
                    {
                        named.Add(output.Attributes["name"].Value, outputPath);
                    }

                    foreach (XmlElement import in output.SelectNodes("import"))
                    {
                        sw.WriteLine(File.ReadAllText(named[import.Attributes["name"].Value]));
                    }

                    foreach (XmlElement input in output.SelectNodes("input"))
                    {
                        sw.WriteLine(File.ReadAllText(GetInputPath(src, input)));
                    }
                }
            }

            return outputPath;
        }

        /// <summary>
        /// Minifies a previously concatenated output file using YUI.
        /// </summary>
        /// <param name="yui">The YUI <see cref="System.Diagnostics.Process"/> to use for minification.</param>
        /// <param name="path">The path of the output file to minify.</param>
        /// <param name="reason">Contains the reason for failure if applicable.</param>
        /// <returns>True if the minification was successful, false otherwise.</returns>
        private static bool MinifyOutput(Process yui, string path, out string reason)
        {
            Console.WriteLine(String.Format("Minifying {0}", Path.GetFileName(path)));

            bool success = true;
            reason = String.Empty;

            string tempPath = String.Concat(path, ".tmp", Path.GetExtension(path));

            yui.StartInfo.Arguments = String.Format("-jar \"{0}\" -o \"{1}\" \"{2}\"",
                YUI,
                tempPath,
                path);

            if (yui.Start())
            {
                yui.WaitForExit();

                if (yui.ExitCode == 0)
                {
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
                else
                {
                    reason = yui.StandardError.ReadToEnd();
                    success = false;
                }
            }
            else
            {
                reason = "You must have Java version >= 1.4 installed to use minification.";
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Gets the fully qualified input path defined for an input node.
        /// </summary>
        /// <param name="src">The source directory defined by the current map context.</param>
        /// <param name="input">The input node to get the relative path part from.</param>
        /// <returns>A fully qualified input path.</returns>
        private static string GetInputPath(string src, XmlElement input)
        {
            string path = input.Attributes["path"].Value;

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(src, path);
            }

            return path;
        }

        /// <summary>
        /// Gets the fully qualified output path defined for an output node.
        /// </summary>
        /// <param name="target">The target directory defined by the current map context.</param>
        /// <param name="version">The version number defined by the current map context.</param>
        /// <param name="output">The output node to get the relative path part from.</param>
        /// <returns>A fully qualified output path.</returns>
        private static string GetOutputPath(string target, string version, XmlElement output)
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

        /// <summary>
        /// Fills the given <see cref="System.Xml.XmlDocument"/> with the
        /// file found at <paramref name="path"/> and validates its schema.
        /// </summary>
        /// <param name="doc">The <see cref="System.Xml.XmlDocument"/> to fill.</param>
        /// <param name="path">The path to the XML file containing the map to load.</param>
        /// <param name="reason">Contains the reason for failure if applicable.</param>
        /// <returns>True if the map was loaded and validated successfully, false otherwise.</returns>
        private static bool LoadAndValidateMap(XmlDocument doc, string path, out string reason)
        {
            bool valid = true;
            reason = String.Empty;

            using (Stream schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SCHEMA))
            {
                using (XmlReader schemaReader = new XmlTextReader(schemaStream))
                {
                    XmlSchemaSet schemas = new XmlSchemaSet();
                    schemas.Add(String.Empty, schemaReader);

                    doc.Schemas = schemas;

                    try
                    {
                        doc.Load(path);
                    }
                    catch (Exception ex)
                    {
                        reason = ex.Message + " ";
                        valid = false;
                    }

                    string validateReason = String.Empty;

                    doc.Validate(new ValidationEventHandler(delegate(object sender, ValidationEventArgs e)
                    {
                        validateReason = e.Message + " ";
                        valid = false;
                    }));

                    reason += validateReason;
                }
            }

            return valid;
        }

        /// <summary>
        /// Parses and defaults the command line and map arguments.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <param name="map">The map to use when defaulting command line arguments.</param>
        /// <param name="src">Contains the source directory upon completion.</param>
        /// <param name="target">Contains the target directory upon completion.</param>
        /// <param name="version">Contains the version number upon completion.</param>
        /// <param name="minify">Contains a value indicating whether minification is enabled upon completion.</param>
        /// <param name="reason">Contains the reason for failure if applicable.</param>
        /// <returns>True if the arguments were parsed successfully, false otherwise.</returns>
        private static bool ParseArguments(Arguments args, string mapPath, XmlDocument map, out string src, out string target, out string version, out bool minify, out string reason)
        {
            bool success = true;
            string mapDirectory = Path.GetDirectoryName(mapPath);
            reason = String.Empty;

            src = args["src"] ?? String.Empty;

            if (String.IsNullOrEmpty(src) && map.DocumentElement.Attributes["src"] != null)
            {
                src = map.DocumentElement.Attributes["src"].Value;
            }

            if (String.IsNullOrEmpty(src))
            {
                src = @".\";
            }

            if (!Path.IsPathRooted(src))
            {
                src = Path.GetFullPath(Path.Combine(mapDirectory, src));
            }

            target = args["target"] ?? String.Empty;

            if (String.IsNullOrEmpty(target) && map.DocumentElement.Attributes["target"] != null)
            {
                target = map.DocumentElement.Attributes["target"].Value;
            }

            if (String.IsNullOrEmpty(target))
            {
                target = @".\";
            }

            if (!Path.IsPathRooted(target))
            {
                target = Path.GetFullPath(Path.Combine(mapDirectory, target));
            }

            version = args["version"] ?? String.Empty;

            if (String.IsNullOrEmpty(version))
            {
                version = map.DocumentElement.Attributes["version"].Value;
            }

            string min = args["minify"] ?? String.Empty;

            if (String.IsNullOrEmpty(min))
            {
                min = map.DocumentElement.Attributes["minify"].Value;
            }

            minify = true;

            try
            {
                minify = Convert.ToBoolean(min);
            }
            catch
            {
                reason += "Minify must be either \"true\" or \"false\". ";
                success = false;
            }

            return success;
        }
    }
}