using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using RGiesecke.DllExport;
using Sandbox;
using Sandbox.Engine.Platform;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game;
using VRage.Plugins;
using System.Linq;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Main entry point.
	/// </summary>
	public class Loader : IPlugin
	{

		public const string Dll = "PluginLoader.dll", Exe = "PluginManager.exe", SeplRepo = "SpaceEngineersPluginLoader", SeplShort = "SEPL";

		/// <summary>If both inject and -plugin are used, there will be two <see cref="Loader"/>. This is a reference to the first one created, the second will be suppressed.</summary>
		private static Loader _instance;

		// Steam generates a popup with this method.
		#region Launch SE with Args
		//private const string launcherArgs = "-plugin " + Dll;

		//public static void Main(string[] args)
		//{
		//	try { LaunchSpaceEngineers(); }
		//	catch (Exception ex)
		//	{
		//		Logger.WriteLine(ex.ToString());
		//		Console.WriteLine(ex.ToString());
		//		Thread.Sleep(60000);
		//	}
		//}

		//private static void LaunchSpaceEngineers()
		//{
		//	string myDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		//	string launcher = myDirectory + "\\SpaceEngineers.exe";
		//	if (File.Exists(launcher))
		//	{
		//		Process.Start(launcher, launcherArgs).Dispose();
		//		return;
		//	}

		//	launcher = myDirectory + "\\SpaceEngineersDedicated.exe";
		//	if (File.Exists(launcher))
		//	{
		//		Process.Start(launcher, launcherArgs).Dispose();
		//		return;
		//	}

		//	throw new Exception("Not in Space Engineers folder");
		//}

		#endregion

		#region Injected Init

		/// <summary>
		/// Starting point when injected into SE.
		/// </summary>
		[DllExport]
		public static void RunInSEProcess()
		{
			for (int i = 0; i < 1000000; ++i)
			{
				if (MySandboxGame.Static != null)
				{
					if (_instance != null)
						return;
					MySandboxGame.Static.Invoke(() => typeof(MyPlugins).GetMethod("LoadPlugins", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { Assembly.GetExecutingAssembly() }));
					return;
				}
				Thread.Sleep(1);
			}

			throw new TimeoutException("Timed out waiting for instance of MySandboxGame");
		}

		#endregion

		/// <summary>
		/// Adds a locally compiled plugin and optionally publishes it.
		/// </summary>
		public static void AddLocallyCompiledPlugin(PluginBuilder builder)
		{
			if (_instance == null)
				new Loader(false);

			if (builder.version.CompareTo(default(Version)) <= 0)
			{
				foreach (string file in builder.files.Select(f => f.source))
				{
					Version fileVersion = new Version(FileVersionInfo.GetVersionInfo(file), builder.version.SeVersion);
					if (builder.version.CompareTo(fileVersion) < 0)
						builder.version = fileVersion;
				}
				Logger.WriteLine("Got plugin version from files: " + builder.version);
			}

			if (builder.allBuilds)
				builder.version.SeVersion = 0;
			else if (builder.version.SeVersion < 1)
				builder.version.SeVersion = GetCurrentSEVersion();

			_instance._task.Wait();
			Plugin plugin = _instance.AddLocallyCompiled(builder);

			if (builder.release && GitChecks.Check(builder.files.First().source, builder.pathToGitExe))
				(new GitHubClient(plugin.name, builder.oAuthToken)).Publish(plugin);
		}

		/// <summary>
		/// Get the current Space Engineers version from SpaceEngineersGame.
		/// </summary>
		/// <returns>The current version of Space Engineers.</returns>
		public static int GetCurrentSEVersion()
		{
			FieldInfo field = typeof(SpaceEngineersGame).GetField("SE_VERSION", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				throw new NullReferenceException("SpaceEngineersGame does not have field SE_VERSION, or it has unexpected binding");
			return (int)field.GetValue(null);
		}

		private string _directory;
		private PluginData _data;
		private ParallelTasks.Task _task;
		private DownloadProgress.Stats _downProgress = new DownloadProgress.Stats();
		private IPlugin[] _plugins;
		private bool _initialized, _startedRobocopy;

		public ICollection<PluginConfig> GitHubConfig
		{
			get
			{
				_task.Wait();
				return _data.GitHubConfig;
			}
			set
			{
				_task.Wait();
				_data.GitHubConfig = value;
				_data.Save();
			}
		}

		/// <summary>
		/// Creates an instance of <see cref="Loader"/> and starts the updating process.
		/// </summary>
		public Loader() : this(true) { }

		/// <summary>
		/// Creates an instance of <see cref="Loader"/> and, optionally, starts the updating process.
		/// </summary>
		/// <param name="start">Iff true, start the updating process.</param>
		public Loader(bool start)
		{
			if (_instance != null)
				return;

			_instance = this;
			_directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			if (!File.Exists(_directory + "\\SpaceEngineers.exe") && !File.Exists(_directory + "\\SpaceEngineersDedicated.exe"))
				throw new Exception("Not in Space Engineers folder");

			_directory = PathExtensions.Combine(Path.GetDirectoryName(_directory), SeplRepo);
			Directory.CreateDirectory(_directory);

			_data = new PluginData(_directory);

			Logger.logFile = PathExtensions.Combine(_directory, (Game.IsDedicated ? SeplShort + " Dedicated.log" : SeplShort + ".log"));
			Logger.WriteLine(SeplShort + " version: " + new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location), 0));

			if (start)
				_task = ParallelTasks.Parallel.StartBackground(Run);
			else
				_task = ParallelTasks.Parallel.StartBackground(_data.Load);
		}

		private void Run()
		{
			_data.Load();
			UpdatePlugin();
			_data.Save();
		}

		/// <summary>
		/// Update PluginLoader.dll and PluginManager.exe from download folder.
		/// </summary>
		void IDisposable.Dispose()
		{
			foreach (IPlugin plugin in _plugins)
				plugin.Dispose();
			Robocopy();
		}

		/// <summary>
		/// Update PluginLoader.dll and PluginManager.exe from download folder using robocopy.
		/// </summary>
		private void Robocopy()
		{
			if (_instance != this || _startedRobocopy)
				return;
			_startedRobocopy = true;

			PluginName seplName = new PluginName("Rynchodon", SeplRepo);
			string seplDownloadPath = PathExtensions.Combine(_directory, "plugin", seplName.fullName);
			string spaceEngineersPath = Path.GetDirectoryName(_directory);
			string bin64 = PathExtensions.Combine(spaceEngineersPath, "Bin64");
			string ded64 = PathExtensions.Combine(spaceEngineersPath, "DedicatedServer64");

			string license = PathExtensions.Combine(seplDownloadPath, "License.rtf");
			if (File.Exists(license))
				File.Copy(license, PathExtensions.Combine(_directory, "License.rtf"), true);

			Logger.WriteLine("starting robocopy");

			string first = '"' + seplDownloadPath + "\" \"";

			string toBin64 = first + bin64 + "\" " + Dll + " " + Exe + " /copyall /W:1 /xx";
			string toDed64 = first + ded64 + "\" " + Dll + " /copyall /W:1 /xx";

			Process robocopy = new Process();
			robocopy.StartInfo.FileName = "cmd.exe";
			robocopy.StartInfo.Arguments = "/C robocopy " + toBin64 + " & robocopy " + toDed64;
			robocopy.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			robocopy.Start();
		}

		/// <summary>
		/// Initialize this object.
		/// </summary>
		/// <param name="gameInstance">MySandboxGame.Static, so I don't know why it's a param</param>
		public void Init(object gameInstance = null)
		{
			if (_instance != this)
				return;
			_initialized = true;

			if (!_task.IsComplete && !Game.IsDedicated)
				MyGuiSandbox.AddScreen(new DownloadProgress(_task, _downProgress));
		}

		/// <summary>
		/// Load plugins, if updating has finished.
		/// </summary>
		void IPlugin.Update()
		{
			if (_instance != this)
				return;
			if (!_initialized)
				Init();

			if (_plugins != null)
				for (int i = _plugins.Length - 1; i >= 0; --i)
					_plugins[i].Update();
			else
				CheckTask();
		}

		private void CheckTask()
		{
			if (_task.IsComplete)
			{
				Logger.WriteLine("Finished task, loading plugins");
				_plugins = LoadPlugin();
				foreach (IPlugin plugin in _plugins)
					plugin.Init(MySandboxGame.Static);

				_data = default(PluginData);
				_task = default(ParallelTasks.Task);
				_downProgress = null;
			}
		}

		private void UpdatePlugin()
		{
			_downProgress.Total = 0;
			_downProgress.Current = 0;

			Logger.WriteLine("Updating plugins");
			UpdatePlugin(_data.EnabledGitHubConfig());
		}

		private void UpdatePlugin(IEnumerable<PluginConfig> pluginConfig)
		{
			_downProgress.Total += pluginConfig.Count();
			foreach (PluginConfig config in pluginConfig)
			{
				++_downProgress.Current;
				Plugin current = UpdatePlugin(config);
				if (current.requiredPlugins != null)
					UpdatePlugin(current.requiredPlugins.Select(name => new PluginConfig(name, false)));
			}
		}

		private Plugin UpdatePlugin(PluginConfig config)
		{
			Logger.WriteLine("plugin: " + config.name.fullName);

			GitHubClient client = new GitHubClient(config.name);

			Plugin current;
			if (_data.TryGetDownloaded(config.name, out current))
			{
				if (client.Update(current))
				{
					Logger.WriteLine("Updated");
					_data.AddDownloaded(current);
				}
			}
			else
			{
				current = new Plugin(_directory, config);
				if (client.Update(current))
				{
					Logger.WriteLine("New download");
					_data.AddDownloaded(current);
				}
			}

			return current;
		}

		private IPlugin[] LoadPlugin()
		{
			Plugin plugin;
			List<IPlugin> pluginInterface = new List<IPlugin>();
			HashSet<PluginName> loaded = new HashSet<PluginName>();

			foreach (PluginConfig config in _data.EnabledGitHubConfig())
				if (_data.TryGetDownloaded(config.name, out plugin))
					LoadPlugin(plugin, pluginInterface, loaded);

			return pluginInterface.ToArray();
		}

		private bool LoadPlugin(Plugin plugin, List<IPlugin> pluginInterface, HashSet<PluginName> loaded, int depth = 0)
		{
			if (plugin.name.author == "Rynchodon" && plugin.name.repository == SeplRepo)
				return true;

			if (loaded.Contains(plugin.name))
				return true;
			if (depth > 100)
			{
				Logger.WriteLine("ERROR Failed to load " + plugin.name.fullName + ", recursive requirements");
				return false;
			}

			if (plugin.requiredPlugins != null)
				foreach (PluginName name in plugin.requiredPlugins)
				{
					Plugin required;
					if (!_data.TryGetDownloaded(name, out required))
					{
						Logger.WriteLine("ERROR: Failed to load " + plugin.name.fullName + ", missing required plugin: " + name.fullName);
						return false;
					}
					if (!LoadPlugin(required, pluginInterface, loaded, depth + 1))
					{
						Logger.WriteLine("ERROR: Failed to load " + plugin.name.fullName + ", failed to load required plugin: " + name.fullName);
						return false;
					}
				}

			if (!loaded.Add(plugin.name))
				throw new Exception(plugin.name.fullName + " already loaded");

			return plugin.LoadDll(pluginInterface);
		}

		private Plugin AddLocallyCompiled(PluginBuilder builder)
		{
			PluginName name = new PluginName(builder.author, builder.repo);
			Plugin plugin;
			if (!_data.TryGetDownloaded(name, out plugin))
				plugin = new Plugin(_directory, new PluginConfig(name, true));
			else
				plugin.EraseAllFiles();

			plugin.version = builder.version;
			Logger.WriteLine("plugin: " + name.fullName + ", compiled version: " + plugin.version);

			Directory.CreateDirectory(plugin.directory);

			foreach (var fileSource in builder.files)
			{
				string fileDestination = fileSource.targetFolder == null ?
					PathExtensions.Combine(plugin.directory, Path.GetFileName(fileSource.source)) :
					PathExtensions.Combine(plugin.directory, fileSource.targetFolder, Path.GetFileName(fileSource.source));

				if (!Path.GetFullPath(fileDestination).StartsWith(plugin.directory))
					throw new Exception(Path.GetFullPath(fileDestination) + " is outside of plugin's directory");

				Logger.WriteLine("Copy: " + fileSource.source + " to " + fileDestination);
				Directory.CreateDirectory(Path.GetDirectoryName(fileDestination));
				File.Copy(fileSource.source, fileDestination, true);
				plugin.AddFile(fileDestination, fileSource.requires);
			}

			plugin.locallyCompiled = true;

			_data.AddConfig(plugin.config);
			_data.AddDownloaded(plugin);
			_data.Save();

			if (name.author == "Rynchodon" && name.repository == SeplRepo)
				Robocopy();

			return plugin;
		}

	}
}
