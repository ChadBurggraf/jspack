

namespace JSPack.Test
{
    using System;
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Map context tests.
    /// </summary>
    [TestClass]
    public sealed class MapContextTests
    {
        /// <summary>
        /// Map context construction tests.
        /// </summary>
        [TestMethod]
        public void MapContextConstruct()
        {
            string path = Path.GetFullPath("map.xml");
            XmlDocument document = new XmlDocument();
            document.Load(path);

            MapContext context = new MapContext(path, document, null);
            Assert.AreEqual(true, context.EnableOutputActions);
            Assert.AreEqual(path, context.DocumentPath);
            Assert.AreEqual(Environment.CurrentDirectory, context.SourceDirectory);
            Assert.AreEqual(Environment.CurrentDirectory, context.TargetDirectory);
            Assert.AreEqual("1.2.3.4", context.Version);
        }
    }
}
