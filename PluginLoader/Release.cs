using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Information about a GitHub account.
	/// </summary>
	[DataContract]
	public class Account
	{
		[DataMember]
		public string login, avatar_url, gravatar_id, url, html_url, followers_url, following_url, gists_url, starred_url, subscriptions_url, organizations_url, repos_url, events_url, received_events_url, type;
		[DataMember]
		public long id;
		[DataMember]
		public bool site_admin;
	}

	/// <summary>
	/// Data that is needed to create a GitHub release.
	/// </summary>
	[DataContract]
	public class CreateRelease
	{

		[DataMember]
		public string target_commitish, name, body;
		[DataMember]
		public bool draft, prerelease;

		[IgnoreDataMember]
		private string _tag_name;

		/// <summary>Needs to be in a Version compatible format.</summary>
		[DataMember]
		public string tag_name
		{
			get { return _tag_name; }
			set
			{
				version = new Version(value);
				_tag_name = value;
			}
		}

		[IgnoreDataMember]
		public Version version { get; private set; }

		public CreateRelease() { }

		public CreateRelease(Version version, PluginBuilder.Release builder)
		{
			this.target_commitish = builder.target_commitish;
			this.name = builder.name;
			this.body = builder.GetBody();
			this.draft = builder.draft;
			this.prerelease = builder.prerelease;
			this.version = version;
			this.tag_name = version.ToString();
		}

		/// <summary>
		/// Special JSON writer for CreateRelease because GitHub won't take null for an answer.
		/// </summary>
		/// <param name="targetStream">The stream to write to.</param>
		internal void WriteCreateJson(Stream targetStream)
		{
			StringBuilder builder = new StringBuilder();
			List<string> fieldStrings = new List<string>();
			foreach (FieldInfo field in typeof(CreateRelease).GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				if (!field.HasAttribute<DataMemberAttribute>())
					continue;

				object fieldValue = field.GetValue(this);
				if (fieldValue == null)
					continue;

				WriteCreateJson(builder, fieldStrings, field, field.FieldType, fieldValue);
			}
			foreach (PropertyInfo property in typeof(CreateRelease).GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (!property.HasAttribute<DataMemberAttribute>())
					continue;

				object propertyValue = property.GetValue(this);
				if (propertyValue == null)
					continue;

				WriteCreateJson(builder, fieldStrings, property, property.PropertyType, propertyValue);
			}

			using (StreamWriter writer = new StreamWriter(targetStream))
			{
				writer.Write('{');
				writer.Write(string.Join(",", fieldStrings));
				writer.Write('}');
			}
		}

		private static void WriteCreateJson(StringBuilder builder, List<string> fieldStrings, MemberInfo member, Type memberType, object fieldValue)
		{
			builder.Clear();
			builder.Append('"');
			builder.Append(member.Name);
			builder.Append("\":");
			if (memberType == typeof(string))
			{
				builder.Append('"');
				builder.Append((string)fieldValue);
				builder.Append('"');
			}
			else
				builder.Append(fieldValue.ToString().ToLower());

			fieldStrings.Add(builder.ToString());
		}
	}

	/// <summary>
	/// All the information about a GitHub release.
	/// </summary>
	[DataContract]
	public class Release : CreateRelease
	{
		[DataContract]
		public class Asset
		{
			[DataMember]
			public string url, browser_download_url, name, label, state, content_type, created_at, updated_at;
			[DataMember]
			public long id, size, download_count;
			public Account uploader;
		}

		[DataMember]
		public string url, html_url, assets_url, upload_url, tarball_url, zipball_url, created_at, published_at;
		[DataMember]
		public long id;
		[DataMember]
		public Account author;
		[DataMember]
		public Asset[] assets;
	}
}
