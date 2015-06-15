﻿// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Collections.Generic;
using System.Linq;
using SystemIO = System.IO;

namespace Fluent.IO {
    public sealed class Path : PathBase<Path> {
        /// <summary>
        /// Creates an empty Path object.
        /// </summary>
        public Path() {
        }

        /// <summary>
        /// Creates a collection of paths from a list of path strings.
        /// </summary>
        /// <param name="paths">The list of path strings.</param>
        public Path(params string[] paths) : base(paths) {
        }

        /// <summary>
        /// Creates a collection of paths from a list of path strings.
        /// </summary>
        /// <param name="paths">The list of path strings.</param>
        public Path(params Path[] paths) : base(paths.ToList()) {
        }

        /// <summary>
        /// Creates a collection of paths from a list of path strings.
        /// </summary>
        /// <param name="paths">The list of path strings.</param>
        public Path(IEnumerable<string> paths) : base(paths) {
        }

        /// <summary>
        /// Creates a collection of paths from a list of paths.
        /// </summary>
        /// <param name="paths">The list of paths.</param>
        public Path(IEnumerable<Path> paths) : base(paths) {
        }

        /// <summary>
        /// Creates a collection of paths from a list of path strings and a previous list of path strings.
        /// </summary>
        /// <param name="path">A path string.</param>
        /// <param name="previousPaths">The list of path strings in the previous set.</param>
        public Path(string path, Path previousPaths) : base(path, previousPaths) {
        }

        /// <summary>
        /// Creates a collection of paths from a list of path strings and a previous list of path strings.
        /// </summary>
        /// <param name="paths">The list of path strings in the set.</param>
        /// <param name="previousPaths">The list of path strings in the previous set.</param>
        public Path(IEnumerable<string> paths, Path previousPaths) : base(paths, previousPaths) {
        }

        public static explicit operator string(Path path) {
            return path.FirstPath();
        }

        public static explicit operator Path(string path) {
            return new Path(path);
        }
    }
}
