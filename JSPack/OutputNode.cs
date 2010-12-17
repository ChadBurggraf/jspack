

namespace JSPack
{
    using System;

    public sealed class OutputNode : Node
    {
        public bool Actions { get; set; }

        

        public string Name { get; set; }

        public string Path { get; set; }

        public string ResolvedPath { get; private set; }

        public bool Temporary { get; set; }

        public bool Version { get; set; }
    }
}
