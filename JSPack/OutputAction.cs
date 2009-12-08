using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace JSPack
{
    /// <summary>
    /// Performs an executable action on an output.
    /// </summary>
    public class OutputAction
    {
        private Process process;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The action's name in the map.</param>
        public OutputAction(string name) : this(name, null, null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The action's name in the map.</param>
        /// <param name="executableName">The name of the executable the action will call.</param>
        public OutputAction(string name, string executableName) : this(name, executableName, null) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The action's name in the map.</param>
        /// <param name="executableName">The name of the executable the action will call.</param>
        /// <param name="arguments">A string of arguments to pass to the executable.</param>
        public OutputAction(string name, string executableName, string arguments)
        {
            Name = name;
            ExecutableName = executableName;
            Arguments = arguments;

            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
        }

        /// <summary>
        /// Gets or sets a string of arguments to pass to the executable;
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets or sets the name of the executable the action will call.
        /// </summary>
        public string ExecutableName { get; set; }

        /// <summary>
        /// Gets or sets the action's name in the map.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Executes the action for the given output path.
        /// </summary>
        /// <param name="input">A stream to read the action's standard input from.</param>
        /// <param name="output">A stream to write the action's standard output to.</param>
        /// <param name="reason">Contains the reason for failure on completion.</param>
        /// <returns>True if the execution succeeds, false otherwise.</returns>
        public bool Execute(Stream input, Stream output, out string reason)
        {
            const int bufferSize = 4096;
            bool success = false;
            reason = String.Empty;

            if (!String.IsNullOrEmpty(ExecutableName))
            {
                process.StartInfo.FileName = ExecutableName;
                process.StartInfo.Arguments = Arguments ?? String.Empty;

                if (process.Start())
                {
                    byte[] buffer = new byte[bufferSize];
                    int count = 0;

                    while (0 < (count = input.Read(buffer, 0, buffer.Length)))
                    {
                        process.StandardInput.BaseStream.Write(buffer, 0, count);
                        buffer = new byte[bufferSize];
                    }

                    process.StandardInput.Close();
                    buffer = new byte[bufferSize];

                    while(0 < (count = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length))) 
                    {
                        output.Write(buffer, 0, count);
                        buffer = new byte[bufferSize];
                    }

                    process.StandardOutput.Close();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        success = true;
                    }
                    else
                    {
                        reason = process.StandardError.ReadToEnd();
                    }
                }
                else
                {
                    reason = String.Concat("Failed to start \"", ExecutableName, "\".");
                }
            }
            else
            {
                reason = "There was no executable defined for execution.";
            }

            return success;
        }
    }
}
