using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Rynchodon.PluginLoader
{
	[DataContract]
	public struct PluginConfig
	{
		[DataMember]
		public PluginName name;
		[DataMember]
		public bool downloadPrerelease;
		[DataMember]
		public bool enabled;

		public PluginConfig(PluginName name, bool downloadPrerelease, bool enabled = true)
		{
			this.name = name;
			this.downloadPrerelease = downloadPrerelease;
			this.enabled = enabled;
		}
	}

	[DataContract(Namespace = "")]
	public struct PluginName : IComparable<PluginName>, IEquatable<PluginName>
	{
		[DataMember]
		public string author, repository;

		[IgnoreDataMember]
		public string fullName
		{
			get { return author + '.' + repository; }
		}

		[IgnoreDataMember]
		public string releases_site
		{
			get { return @"https://api.github.com/repos/" + author + "/" + repository + "/releases"; }
		}

		[IgnoreDataMember]
		public string uploads_site
		{
			get { return @"https://uploads.github.com/repos/" + author + "/" + repository + "/releases"; }
		}

		public PluginName(string author, string repository)
		{
			this.author = author;
			this.repository = repository;
		}

		public int CompareTo(PluginName other)
		{
			int authorCompare = author.CompareTo(other.author);
			if (authorCompare != 0)
				return authorCompare;
			return repository.CompareTo(other.repository);
		}

		public bool Equals(PluginName other)
		{
			return this.author == other.author && this.repository == other.repository;
		}

		public override int GetHashCode()
		{
			return fullName.GetHashCode();
		}

		public override string ToString()
		{
			return fullName;
		}
	}
}
