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
    /// <summary>
    /// Loads and validates (for schema only) JSPack map files.
    /// </summary>
    public class MapLoader
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="path">The path of the map to load and validate.</param>
        public MapLoader(string path)
        {
            Path = System.IO.Path.GetFullPath(path);

            string reason;
            Map = new XmlDocument();
            MapIsValid = LoadAndValidateMap(Map, Path, out reason);
            MapIsInvalidReason = reason;
        }

        /// <summary>
        /// Gets the map that was loaded for this instance.
        /// </summary>
        public XmlDocument Map { get; private set; }

        /// <summary>
        /// Gets the reason the map is invalid, if applicable.
        /// </summary>
        public string MapIsInvalidReason { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance's map is valid.
        /// </summary>
        public bool MapIsValid { get; private set; }

        /// <summary>
        /// Gets the full path to this instance's map file.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Loads and validates a JSPack map file.
        /// </summary>
        /// <param name="map">The XML document to fill.</param>
        /// <param name="path">The path to the map file being loaded.</param>
        /// <param name="reason">The reason for failure upon completion.</param>
        /// <returns>A value indicating whether the map file is valid.</returns>
        private static bool LoadAndValidateMap(XmlDocument map, string path, out string reason)
        {
            bool valid = true;
            reason = "The script map is invalid.\n";

            using (Stream schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JSPack.Map.xsd"))
            {
                using (XmlReader schemaReader = new XmlTextReader(schemaStream))
                {
                    XmlSchemaSet schemas = new XmlSchemaSet();
                    schemas.Add(String.Empty, schemaReader);

                    map.Schemas = schemas;

                    try
                    {
                        map.Load(path);
                    }
                    catch (Exception ex)
                    {
                        reason = ex.Message + "\n";
                        valid = false;
                    }

                    string validateReason = String.Empty;

                    map.Validate(new ValidationEventHandler(delegate(object sender, ValidationEventArgs e)
                    {
                        validateReason = e.Message + "\n";
                        valid = false;
                    }));

                    reason += validateReason;
                }
            }

            return valid;
        }
    }
}
