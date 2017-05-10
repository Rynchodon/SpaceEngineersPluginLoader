using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Rynchodon.PluginLoader
{
	[DataContract]
	public struct Version : IComparable<Version>
	{
		[DataMember]
		public int Major, Minor, Build, Revision;
		/// <summary>Space Engineers version the file was compiled with. If the value is 0, the file is considered compatible with all Space Engineers versions.</summary>
		[DataMember]
		public int SeVersion;

		public Version(FileVersionInfo info, int seVersion)
		{
			Major = Math.Max(info.FileMajorPart, info.ProductMajorPart);
			Minor = Math.Max(info.FileMinorPart, info.ProductMinorPart);
			Build = Math.Max(info.FileBuildPart, info.ProductBuildPart);
			Revision = Math.Max(info.FilePrivatePart, info.ProductPrivatePart);

			this.SeVersion = seVersion;
		}

		/// <summary>
		/// Construct a version from a string.
		/// </summary>
		/// <param name="versionString">The string to create the version from.</param>
		public Version(string versionString)
		{
			Match match = Regex.Match(versionString, @"(\d+)\.(\d+)\.?(\d*)\.?(\d*)");

			if (!match.Success)
				throw new ArgumentException("Could not parse: " + versionString);

			string group = match.Groups[1].Value;
			this.Major = string.IsNullOrWhiteSpace(group) ? 0 : int.Parse(group);
			group = match.Groups[2].Value;
			this.Minor = string.IsNullOrWhiteSpace(group) ? 0 : int.Parse(group);
			group = match.Groups[3].Value;
			this.Build = string.IsNullOrWhiteSpace(group) ? 0 : int.Parse(group);
			group = match.Groups[4].Value;
			this.Revision = string.IsNullOrWhiteSpace(group) ? 0 : int.Parse(group);

			match = Regex.Match(versionString, @"-SE(\d+)");
			if (!match.Success)
				SeVersion = 0;
			else
			{
				group = match.Groups[1].Value;
				SeVersion = string.IsNullOrWhiteSpace(group) ? 0 : int.Parse(group);
			}
		}

		/// <summary>
		/// Compare a version to another to determine if it is more preferable. Versions are first compared by SE version, then by major, minor, build, and revision numbers.
		/// </summary>
		/// <param name="other">The version to compare this version against.</param>
		/// <returns>A positive value, zero, or a negative value indicating that this version is preferred over, is equal to, or defers to other.</returns>
		public int CompareTo(Version other)
		{
			int diff;
			diff = this.SeVersion - other.SeVersion;
			if (diff != 0)
				return diff;
			diff = this.Major - other.Major;
			if (diff != 0)
				return diff;
			diff = this.Minor - other.Minor;
			if (diff != 0)
				return diff;
			diff = this.Build - other.Build;
			if (diff != 0)
				return diff;
			diff = this.Revision - other.Revision;
			if (diff != 0)
				return diff;

			return 0;
		}

		public override string ToString()
		{
			string value = "v" + Major + "." + Minor + "." + Build + "." + Revision;
			if (SeVersion > 0)
				value += "-SE" + SeVersion;
			return value;
		}

	}
}
