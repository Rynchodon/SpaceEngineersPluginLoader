using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Rynchodon.PluginLoader
{
	internal class PluginData
	{

		private const string fileName = "Settings.json";
		/// <summary>
		/// Don't load this one!
		/// </summary>
		private static readonly PluginName loadArms = new PluginName("Rynchodon", "Load-ARMS");

		private readonly string _directory;

		/// <summary>
		/// Plugins that are configured to be downloaded.
		/// </summary>
		private readonly Dictionary<PluginName, PluginConfig> _gitHubConfig = new Dictionary<PluginName, PluginConfig>();
		/// <summary>
		/// Required plugins that are not in <see cref="_gitHubConfig"/>.
		/// </summary>
		private readonly HashSet<PluginName> _required = new HashSet<PluginName>();
		/// <summary>
		/// Plugins that have been downloaded.
		/// </summary>
		private readonly Dictionary<PluginName, Plugin> _downloaded = new Dictionary<PluginName, Plugin>();

		private bool _needsSave = false;

		public ICollection<PluginConfig> GitHubConfig
		{
			get { return _gitHubConfig.Values; }
			set
			{
				_gitHubConfig.Clear();
				foreach (PluginConfig config in value)
					AddConfig(config);
				_needsSave = true;
			}
		}

		public PluginData(string directory)
		{
			this._directory = directory;
		}

		private string GetFilePath()
		{
			return PathExtensions.Combine(_directory, fileName);
		}

		/// <summary>
		/// Add a configuration. Load-ARMS is not permitted.
		/// </summary>
		public void AddConfig(PluginConfig config)
		{
			if (config.name.Equals(loadArms))
			{
				string log = "ERROR: Cannot add " + config.name.repository + ", it is incompatible with " + Loader.SeplShort + ". Adding ARMS instead";
				Logger.WriteLine(log);
				Console.Error.WriteLine(log);
				config.name.repository = "ARMS";
			}
			_gitHubConfig[config.name] = config;
			_needsSave = true;
		}

		public void AddDownloaded(Plugin plugin)
		{
			_downloaded[plugin.name] = plugin;
			_needsSave = true;
		}

		/// <summary>
		/// Add a required plugin, if it is not already going to be downloaded.
		/// </summary>
		/// <param name="name">The required plugin.</param>
		/// <returns>True if the plugin was added. False if it is in config or already required.</returns>
		public bool Require(PluginName name)
		{
			_needsSave = true;
			return !_gitHubConfig.ContainsKey(name) && _required.Add(name);
		}

		public bool TryGetDownloaded(PluginName name, out Plugin plugin)
		{
			return _downloaded.TryGetValue(name, out plugin);
		}

		public void Load()
		{
			_gitHubConfig.Clear();
			_required.Clear();
			_downloaded.Clear();

			string filePath = GetFilePath();
			Settings set = default(Settings);

			if (File.Exists(filePath))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				fileInfo.IsReadOnly = false;
				try
				{
					Serialization.ReadJson(filePath, out set);
				}
				catch
				{
					Logger.WriteLine("ERROR: Failed to read settings file");
					throw;
				}
				finally
				{
					fileInfo.IsReadOnly = true;
				}
			}

			if (set.GitHubConfig != null)
			{
				Logger.WriteLine("Loading config");
				foreach (PluginConfig config in set.GitHubConfig)
					_gitHubConfig.Add(config.name, config);
			}
			else
			{
				Logger.WriteLine("Create new config");
				PluginConfig config = new PluginConfig(new PluginName("Rynchodon", Loader.SeplRepo), false);
				_gitHubConfig.Add(config.name, config);
			}

			if (set.Downloaded != null)
			{
				Logger.WriteLine("Loading downloads");
				foreach (Plugin plugin in set.Downloaded)
					if (plugin.MissingFile())
						Logger.WriteLine(plugin.name.fullName + " is missing a file, it must be downloaded again");
					else
						_downloaded.Add(plugin.name, plugin);
			}
		}

		public void Save(bool force = false)
		{
			if (!_needsSave)
				return;
			_needsSave = false;

			string filePath = GetFilePath();
			Settings set;
			set.Downloaded = _downloaded.Values.ToArray();
			set.GitHubConfig = _gitHubConfig.Values.ToArray();
			FileInfo fileInfo = new FileInfo(filePath);
			if (File.Exists(filePath))
				fileInfo.IsReadOnly = false;
			try
			{
				Serialization.WriteJson(filePath, set, true);
			}
			catch
			{
				Logger.WriteLine("ERROR: Failed to write settings file");
				throw;
			}
			finally
			{
				fileInfo.IsReadOnly = true;
			}
		}

		[DataContract]
		private struct Settings
		{
			[DataMember]
			public Plugin[] Downloaded;
			[DataMember]
			public PluginConfig[] GitHubConfig;
		}

	}
}
