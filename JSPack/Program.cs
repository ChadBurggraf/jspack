using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using CommandLine.Utility;

namespace JSPack
{
    public class Program
    {
        #region Constants

        private const string CHANGES = "Changes detected in source directory, re-packaging.";
        private const string TIME = "Packing completed in {0:N2} seconds at {1:F}.\n";
        private const string USAGE = "Usage: jspack /map:path_to_map [/src:source_dir /target:target_dir /version:version_number /actions:true|false]\n";
        private const string WATCHING = "Watching for changes. Press Ctl+C to quit.\n";
        
        #endregion

        public static void Main(params string[] args)
        {
            Console.WriteLine();
            Arguments inputArgs = new Arguments(args);

            if (!String.IsNullOrEmpty(inputArgs["map"]))
            {
                MapLoader loader = new MapLoader(inputArgs["map"]);

                if (loader.MapIsValid)
                {
                    MapArguments mapArgs = new MapArguments(inputArgs, loader);

                    if (mapArgs.ArgumentsAreValid)
                    {
                        DateTime start = DateTime.Now;
                        Packer = new Packer(loader, mapArgs);
                        Packer.Pack(Console.Out, Console.Error);
                        Console.WriteLine(TIME, DateTime.Now.Subtract(start).TotalSeconds, DateTime.Now);

                        if (!String.IsNullOrEmpty(inputArgs["watch"]) && Convert.ToBoolean(inputArgs["watch"]))
                        {
                            Console.WriteLine(WATCHING);
                            LastChange = DateTime.Now;

                            FileSystemWatcher watcher = new FileSystemWatcher(mapArgs.SourcePath);
                            watcher.IncludeSubdirectories = true;
                            watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                            watcher.EnableRaisingEvents = true;

                            while (true)
                            {
                                Console.ReadKey();
                            }
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(mapArgs.ArgumentsInvalidReason);
                    }
                }
                else
                {
                    Console.Error.WriteLine(loader.MapIsInvalidReason);
                }
            }
            else
            {
                Console.Error.WriteLine(USAGE);
            }
        }

        /// <summary>
        /// Gets or sets the date the watcher last observed a change in the watch path.
        /// </summary>
        private static DateTime LastChange { get; set; }

        /// <summary>
        /// Gets or sets the packer we're using to run the packing process.
        /// </summary>
        private static Packer Packer { get; set; }

        /// <summary>
        /// Raises the watcher's Changed event.
        /// </summary>
        private static void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath);

            if (ext.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".js", StringComparison.OrdinalIgnoreCase))
            {
                // Depending on the editing application we might get a bunch of these fired
                // in a row. Users probably aren't hitting save more than once per second.
                if (DateTime.Now.Subtract(LastChange).TotalSeconds > 1)
                {
                    // Wait so hopefully the editor's file handles
                    // are all released.
                    Action action = () =>
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine(CHANGES);
                        DateTime start = DateTime.Now;
                        Packer.Pack(Console.Out, Console.Error);
                        Console.WriteLine(TIME, DateTime.Now.Subtract(start).TotalSeconds, DateTime.Now);
                        Console.WriteLine(WATCHING);
                    };

                    action.BeginInvoke(null, null);
                }

                LastChange = DateTime.Now;
            }
        }
    }
}