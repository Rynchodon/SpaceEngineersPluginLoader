using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using VRage.Plugins;

namespace Rynchodon.PluginLoader
{
	[DataContract]
	internal sealed class Plugin
	{

		/// <summary>Directory for plugin's files.</summary>
		[DataMember]
		public string directory;
		[DataMember]
		public PluginConfig config;

		[DataMember]
		public bool locallyCompiled;
		[DataMember]
		public PluginName[] requiredPlugins;
		[DataMember]
		public Version version;

		/// <summary>
		/// Public so it can be serialized, if access is needed a member should be added.
		/// </summary>
		[DataMember]
		public Dictionary<string, string[]> files = new Dictionary<string, string[]>();

		/// <summary>
		/// Key is the relative path to a file. Value is the file's requirements.
		/// </summary>
		[IgnoreDataMember]
		private Dictionary<string, string[]> _files
		{
			get
			{
				if (files == null)
					files = new Dictionary<string, string[]>();
				return files;
			}
		}

		[IgnoreDataMember]
		public PluginName name
		{
			get { return config.name; }
		}

		public Plugin(string seplDirectory, PluginConfig config)
		{
			this.config = config;
			this.directory = PathExtensions.Combine(seplDirectory, "plugin", name.fullName);
		}

		private string GetFullPath(string relativePath)
		{
			return PathExtensions.Combine(directory, relativePath);
		}

		private string GetRelativeFilePath(string filePath)
		{
			string fullPath = Path.GetFullPath(Path.Combine(directory, filePath)); // need filePath if it is rooted
			if (fullPath.StartsWith(directory))
				return fullPath.Substring(directory.Length + 1);
			throw new ArgumentException(filePath + " is not in plugin's directory");
		}

		public void AddFile(string filePath, string[] requires = null)
		{
			string relativeFilePath = GetRelativeFilePath(filePath);
			if (requires == null)
			{
				_files.Add(relativeFilePath, null);
				return;
			}

			if (!filePath.EndsWith(".dll"))
				throw new Exception(GetFullPath(relativeFilePath) + " has requirements but is not a dll");

			string[] relativeRequires = new string[requires.Length];
			for (int index = requires.Length - 1; index >= 0; --index)
				relativeRequires[index] = GetRelativeFilePath(requires[index]);
			_files.Add(relativeFilePath, relativeRequires);
		}

		public void EraseAllFiles()
		{
			foreach (string relativePath in _files.Keys)
			{
				string fullPath = GetFullPath(relativePath);
				if (File.Exists(fullPath))
				{
					Logger.WriteLine("Deleting " + fullPath);
					File.Delete(fullPath);
				}
			}
			_files.Clear();
		}

		public bool MissingFile()
		{
			if (_files == null)
				throw new NullReferenceException("_files");
			foreach (string relativePath in _files.Keys)
				if (!File.Exists(GetFullPath(relativePath)))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked before uploading to include a file with all the requirements.
		/// </summary>
		private void CreateManifest()
		{
			_files[Manifest.fileName] = null;
			string manifestPath = PathExtensions.Combine(directory, Manifest.fileName);
			Serialization.WriteJson(manifestPath, new Manifest(_files.Keys.ToArray(), _files.Values.ToArray(), requiredPlugins), true);
		}

		public void Zip(string filePath)
		{
			CreateManifest();
			using (FileStream zipFile = new FileStream(filePath, FileMode.CreateNew))
			using (ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
				foreach (var file in _files)
					archive.CreateEntryFromFile(GetFullPath(file.Key), file.Key);
		}

		/// <summary>
		/// Invoked after all files are downloaded to add data from manifest and ensure there are no orphan or missing files.
		/// </summary>
		/// <returns>True if manifest was loaded without error or manifest does not exist. False otherwise.</returns>
		public bool LoadManifest()
		{
			string manifestPath = PathExtensions.Combine(directory, Manifest.fileName);
			if (!File.Exists(manifestPath))
			{
				Logger.WriteLine("WARNING: " + name.fullName + " has no manifest.");
				return true;
			}
			Manifest manifest; Serialization.ReadJson(manifestPath, out manifest);
			requiredPlugins = manifest.requiredPlugins;

			if (manifest.files == null || manifest.files.Length == 0)
			{
				Logger.WriteLine("ERROR: Manifest for " + name.fullName + " has no files.");
				return false;
			}

			bool error = false;

			HashSet<string> orphans = new HashSet<string>(_files.Keys);
			foreach (var f in manifest.FilesAndRequirements())
				if (!orphans.Remove(f.Key))
				{
					Logger.WriteLine("ERROR: " + f.Key + " listed on manifest for " + name.fullName + " is missing");
					error = true;
				}

			foreach (string o in orphans)
			{
				Logger.WriteLine("ERROR: " + o + " is not listed on manifest for " + name.fullName);
				error = true;
			}

			if (error)
				return false;

			Logger.WriteLine(name.fullName + " manifest loaded");
			_files.Clear();
			foreach (var f in manifest.FilesAndRequirements())
				_files.Add(f.Key, f.Value);
			return true;
		}

		public bool LoadDll(ICollection<IPlugin> pluginInterface)
		{
			if (_files.Count == 0)
			{
				Logger.WriteLine("ERROR: Failed to load " + name.fullName + ", plugin has no files");
				return false;
			}

			bool success = true;
			HashSet<string> loaded = new HashSet<string>();
			foreach (var file in _files)
				 success = LoadDll(file.Key, file.Value, pluginInterface, loaded) && success;
			return success;
		}

		private bool LoadDll(string relativePath, string[] requiredFiles, ICollection<IPlugin> pluginInterface, HashSet<string> loaded, int depth = 0)
		{
			if (!relativePath.EndsWith(".dll") || loaded.Contains(relativePath))
				return true;

			string fullPath = GetFullPath(relativePath);
			if (!File.Exists(fullPath))
			{
				LogFailedToLoadFile(fullPath, "file does not exist");
				return false;
			}

			if (depth > 100)
			{
				LogFailedToLoadFile(fullPath, "recursive requirements");
				return false;
			}

			if (requiredFiles != null)
				foreach (string fileName in requiredFiles)
				{
					string[] nestedRequirements;
					if (!_files.TryGetValue(fileName, out nestedRequirements))
					{
						LogFailedToLoadFile(fullPath, "missing required file: " + GetFullPath(fileName));
						return false;
					}
					if (!LoadDll(fileName, nestedRequirements, pluginInterface, loaded, depth + 1))
					{
						LogFailedToLoadFile(fullPath, "failed to load required file: " + GetFullPath(fileName));
						return false;
					}
				}

			if (!loaded.Add(relativePath))
				throw new Exception(fullPath + " already loaded");

			Logger.WriteLine("Loading plugins from " + fullPath);

			Assembly assembly = Assembly.LoadFrom(fullPath);
			if (assembly == null)
			{
				LogFailedToLoadFile(fullPath, "could not load assembly");
				return false;
			}
			foreach (Type t in assembly.ExportedTypes)
				if (typeof(IPlugin).IsAssignableFrom(t))
				{
					try { pluginInterface.Add((IPlugin)Activator.CreateInstance(t)); }
					catch (Exception ex)
					{
						LogFailedToLoadFile(fullPath, "exception while creating instance of " + t.FullName + ":\n" + ex);
						return false;
					}
					Logger.WriteLine("Loaded \"" + t.FullName + '"');
				}

			return true;
		}

		private void LogFailedToLoadFile(string fullPath, string reason)
		{
			Logger.WriteLine("ERROR: Failed to load " + fullPath + ", " + reason);
		}

		[DataContract]
		private struct Manifest
		{
			/// <summary>Name of file to include in release assets.</summary>
			public const string fileName = "Manifest.json";

			[DataMember]
			public string[] files;
			[DataMember]
			public string[][] fileRequirements;
			[DataMember]
			public PluginName[] requiredPlugins;

			public Manifest(string[] files, string[][] fileRequirements, PluginName[] requiredPlugins)
			{
				if (files.Length != fileRequirements.Length)
					throw new ArgumentException("length of files does not match length of fileRequirements");

				this.fileRequirements = fileRequirements;
				this.files = files;
				this.requiredPlugins = requiredPlugins;
			}

			public IEnumerable<KeyValuePair<string, string[]>> FilesAndRequirements()
			{
				if (files.Length != fileRequirements.Length)
					throw new ArgumentException("length of files does not match length of fileRequirements");

				for (int index = 0; index < files.Length; ++index)
					yield return new KeyValuePair<string, string[]>(files[index], fileRequirements[index]);
			}
		}
	}
}
