using System;
using System.Collections.Generic;
using System.IO;

namespace Rynchodon.PluginLoader
{
	public static class PathExtensions
	{

		/// <summary>
		/// Combine paths without a special case for rooted paths.
		/// </summary>
		/// <param name="pathParts">Parts of the path to combine.</param>
		/// <returns>The combined paths.</returns>
		public static string Combine(params string[] pathParts)
		{
			if (pathParts == null)
				throw new ArgumentNullException("pathParts");

			if (pathParts.Length == 0)
				throw new ArgumentException("pathParts has no elements");

			int index = 0;
			string path = pathParts[index];
			if (path == null)
				throw new ArgumentNullException("pathParts[" + index + "]");

			for (index = 1; index < pathParts.Length; ++index)
			{
				string pp = pathParts[index];

				if (pp == null)
					throw new ArgumentNullException("pathParts[" + index + "]");

				char lastChar, firstChar;
				if ((lastChar = path[path.Length - 1]) == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar || (firstChar = pp[0]) == Path.DirectorySeparatorChar || firstChar == Path.AltDirectorySeparatorChar)
					path += pp;
				else
					path += Path.DirectorySeparatorChar + pp;
			}
			return path;
		}

		public static IEnumerable<string> PathsToRoot(string path)
		{
			if (!Path.IsPathRooted(path))
				throw new ArgumentException("Path is not rooted: " + path);
			string root = Path.GetPathRoot(path);
			while (path != root)
			{
				yield return path;
				path = Path.GetDirectoryName(path);
			}
		}

	}
}
