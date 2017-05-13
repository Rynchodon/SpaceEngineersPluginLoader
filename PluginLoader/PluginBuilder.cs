using System.Runtime.Serialization;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Plugin info deserialized from file and ammended by the command line.
	/// </summary>
	[DataContract]
	public sealed class PluginBuilder
	{
		[DataContract]
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
			public string[] requires;

			public File(string source, string targetFolder, string[] requires)
			{
				this.source = source;
				this.targetFolder = targetFolder;
				this.requires = requires;
			}
		}

		[DataContract]
		public sealed class Release
		{
			[DataMember]
			public string target_commitish, name, body, zipFileName;
			[DataMember]
			public bool draft = true, prerelease;
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
		/// <summary>
		/// GitHub oAuthToken, needed to publish a file. Keep this out of version control!
		/// </summary>
		[IgnoreDataMember]
		public string oAuthToken;
	}
}
