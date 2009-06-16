using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace JSPack
{
    public class Program
    {
        #region Constants

        private const string USAGE = "Usage: ScriptBuilder /map:path_to_map [/src:path_to_source /target:path_to_target /version:version_number /minify:true|false]";
        private const string SCHEMA = "JSPack.Map.xsd";

        #endregion

        public static void Main(params string[] args)
        {
            CommandLine.Utility.Arguments inputArgs = new CommandLine.Utility.Arguments(args);

            if (!String.IsNullOrEmpty(inputArgs["map"]))
            {
                string map = Path.GetFullPath(inputArgs["map"]);

                if (File.Exists(map))
                {
                    XmlDocument doc = new XmlDocument();

                    if (LoadAndValidateMap(doc, map))
                    {
                        string sourceDirectory = inputArgs["src"] ?? String.Empty;

                        if (String.IsNullOrEmpty(sourceDirectory))
                        {
                            sourceDirectory = Path.GetFullPath(doc.DocumentElement.Attributes["sourceDirectory"].Value);
                        }

                        string targetDirectory = inputArgs["target"] ?? String.Empty;

                        if (String.IsNullOrEmpty(targetDirectory))
                        {
                            targetDirectory = Path.GetFullPath(doc.DocumentElement.Attributes["targetDirectory"].Value);
                        }

                        string version = inputArgs["version"] ?? String.Empty;

                        if (String.IsNullOrEmpty(version))
                        {
                            version = doc.DocumentElement.Attributes["version"].Value;
                        }

                        string minify = inputArgs["minify"] ?? String.Empty;

                        if (String.IsNullOrEmpty(minify))
                        {
                            minify = doc.DocumentElement.Attributes["minify"].Value;
                        }

                        bool doMinify = true;

                        try
                        {
                            doMinify = Convert.ToBoolean(minify);
                        }
                        catch
                        {
                            Console.WriteLine("Minify must be either \"true\" or \"false\".");
                            return;
                        }

                        string clean = inputArgs["clean"] ?? String.Empty;

                        if (String.IsNullOrEmpty(clean))
                        {
                            clean = doc.DocumentElement.Attributes["clean"].Value;
                        }

                        bool doClean = false;

                        try
                        {
                            doClean = Convert.ToBoolean(clean);
                        }
                        catch
                        {
                            Console.WriteLine("Clean must be either \"true\" or \"false\".");
                            return;
                        }

                        if (Directory.Exists(sourceDirectory))
                        {
                            if (doClean && Directory.Exists(targetDirectory))
                            {
                                Directory.Delete(targetDirectory, true);
                            }

                            if (!Directory.Exists(targetDirectory))
                            {
                                Directory.CreateDirectory(targetDirectory);
                            }

                            Build(doc, sourceDirectory, targetDirectory, version, doMinify);
                        }
                        else
                        {
                            Console.WriteLine(String.Format("Source directory \"{0}\" does not exist.", sourceDirectory));
                        }
                    }
                }
                else
                {
                    Console.WriteLine(String.Format("Map file \"{0}\" does not exist.", map));
                }
            }
            else
            {
                Console.WriteLine(USAGE);
            }
        }

        private static void Build(XmlDocument map, string src, string target, string version, bool minify)
        {
            List<string> temp = new List<string>();
            Dictionary<string, string> named = new Dictionary<string, string>();

            foreach (XmlElement output in map.SelectNodes("scripts/output"))
            {
                string outputPath = GetOutputPath(target, version, output);

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
            }

            foreach (string tempFile in temp)
            {
                File.Delete(tempFile);
            }
        }

        private static string GetInputPath(string src, XmlElement input)
        {
            return Path.Combine(src, input.Attributes["path"].Value);
        }

        private static string GetOutputPath(string target, string version, XmlElement output)
        {
            string path = Path.Combine(target, output.Attributes["path"].Value);

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

        private static bool LoadAndValidateMap(XmlDocument doc, string map)
        {
            bool valid = true;

            using (Stream schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SCHEMA))
            {
                using (XmlReader schemaReader = new XmlTextReader(schemaStream))
                {
                    XmlSchemaSet schemas = new XmlSchemaSet();
                    schemas.Add(String.Empty, schemaReader);

                    doc.Schemas = schemas;
                    doc.Load(map);

                    doc.Validate(new ValidationEventHandler(delegate(object sender, ValidationEventArgs e)
                    {
                        Console.WriteLine(e.Message);
                        valid = false;
                    }));
                }
            }

            return valid;
        }
    }
}
