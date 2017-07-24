using System;
using System.Runtime.Serialization;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Plugin info deserialized from file and ammended by the command line.
	/// </summary>
	[DataContract (Namespace = "")]
	public sealed class PluginBuilder
	{
		[DataContract(Namespace = "")]
		public sealed class File
		{
			/// <summary>The current path to the file, it can be absolute, relative to CWD, or relative to builder file.</summary>
			[DataMember]
			public string source;
			/// <summary>The folder to put the file in. Null for top level.</summary>
			[DataMember]
			public string targetFolder;
			/// <summary>Names of files in this plugin that must be loaded before this one.</summary>
			[DataMember]
			[Obsolete("references are resolved automatically")]
			public string[] requires;

			public File(string source, string targetFolder, string[] requires)
			{
				this.source = source;
				this.targetFolder = targetFolder;
				this.requires = requires;
			}
		}

		[DataContract(Namespace = "")]
		public sealed class Release
		{
			[DataMember]
			public string target_commitish, name, body;
			/// <summary>Lines that are appended to body, separated by new line character.</summary>
			[DataMember]
			public string[] body_lines;
			[DataMember]
			public bool draft = true, prerelease;

			/// <summary>
			/// Concat body and body_lines, separated by new line.
			/// </summary>
			/// <returns>Concat body.</returns>
			public string GetBody()
			{
				const string newline = "\\n";

				if (body_lines == null)
					return body;

				if (body == null)
					return string.Join(newline, body_lines);

				return string.Join(newline, body, body_lines);
			}
		}

		/// <summary>Plugins that are required to be loaded before this one.</summary>
		[DataMember]
		public PluginName[] requires;
		[DataMember]
		public File[] files;
		[DataMember]
		public Release release;
		/// <summary>
		/// The author of the GitHub repository.
		/// </summary>
		[DataMember]
		public string author;
		/// <summary>
		/// The name of the GitHub repository.
		/// </summary>
		[DataMember]
		public string repository;
		/// <summary>
		/// Used by SEPL to decide which release of a plugin to download.
		/// </summary>
		[DataMember]
		public Version version;
		/// <summary>
		/// Publish the plugin to GitHub, you probably do not want to set this in builder file.
		/// </summary>
		[DataMember]
		public bool publish;
		[DataMember]
		public string zipFileName;
		/// <summary>
		/// GitHub oAuthToken, needed to publish a file. Keep this out of version control!
		/// </summary>
		[IgnoreDataMember]
		public string oAuthToken;
	}
}
