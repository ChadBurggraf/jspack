

namespace JSPack
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for all node types in a map.
    /// </summary>
    public abstract class Node
    {
        private IList<Node> dependants, dependencies;

        /// <summary>
        /// Initializes a new instance of the Node class.
        /// </summary>
        /// <param name="context">The map context to initialize the node with.</param>
        protected Node(MapContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context", "context cannot be null.");
            }

            this.Context = context;
            this.dependants = new List<Node>();
            this.dependencies = new List<Node>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the node is dirty.
        /// </summary>
        public bool IsDirty { get; protected set; }

        /// <summary>
        /// Gets the map context this instance is a part of.
        /// </summary>
        protected MapContext Context { get; private set; }

        /// <summary>
        /// Adds a dependency to the node. Also adds this instance
        /// to the given dependency's list of dependants, and updates
        /// this instance's <see cref="IsDirty"/> value as required.
        /// </summary>
        /// <param name="dependency"></param>
        public void AddDependency(Node dependency)
        {
            this.dependencies.Add(dependency);
            dependency.dependants.Add(this);
            this.IsDirty = this.IsDirty || dependency.IsDirty;
        }
    }
}
