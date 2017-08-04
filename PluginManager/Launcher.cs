using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Rynchodon.PluginManager
{
	/// <summary>
	/// Entry point for application.
	/// </summary>
	/// <remarks>
	/// Resolves assemblies so it is not permitted to reference any non-system assemblies. Must resolve assemblies without referencing any other class in its assembly.
	/// </remarks>
	internal static class Launcher
	{

		public const string SeplName = "SpaceEngineersPluginLoader";
		public const string SeFolder = "SpaceEngineers", SeBinFolder = "Bin64", SeDedicatedFolder = "DedicatedServer64",
			SeClientExe = "SpaceEngineers.exe", SeDedicatedExe = "SpaceEngineersDedicated.exe";
		public static readonly string PathPluginLoader, PathBin64, PathDedicated64;

		private static List<string> _consoleOut = new List<string>();

		static Launcher()
		{
			try
			{
				EraseCrashLog();

				string seplDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				PathPluginLoader = Path.Combine(seplDirectory, "PluginLoader.dll");

				string seDirectory = Path.GetDirectoryName(seplDirectory);
				PathBin64 = Path.Combine(seDirectory, SeBinFolder);
				PathDedicated64 = Path.Combine(seDirectory, SeDedicatedFolder);

				CheckForBin(ref PathBin64, false);
				CheckForBin(ref PathDedicated64, false);

				if (PathBin64 != null || PathDedicated64 != null)
				{
					WriteLine("Using path relative to assembly path for SE binaries");
					return;
				}

				string installLocation = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850", "InstallLocation", null);
				if (installLocation == null)
					installLocation = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850", "InstallLocation", null);
				if (installLocation == null)
					throw new Exception(SeFolder + " could not be located");

				WriteLine("Using " + SeFolder + " path from registry");

				PathBin64 = Path.Combine(installLocation, SeBinFolder);
				PathDedicated64 = Path.Combine(installLocation, SeDedicatedFolder);

				CheckForBin(ref PathBin64, true);
				CheckForBin(ref PathDedicated64, true);
			}
			catch (Exception ex)
			{
				CrashDump(ex);
				throw;
			}
			finally
			{
				if (PathBin64 != null)
					WriteLine("Path to " + SeBinFolder + ": " + PathBin64);
				if (PathDedicated64 != null)
					WriteLine("Path to " + SeDedicatedFolder + ": " + PathDedicated64);
			}
		}

		private static void CheckForBin(ref string path, bool fromRegistry)
		{
			if (Directory.Exists(path))
			{
				if (File.Exists(Path.Combine(path, SeClientExe)) || File.Exists(Path.Combine(path, SeDedicatedExe))) // SE might be locally compiled
					return;

				WriteLine((fromRegistry ? "ERROR: " : "WARNING: ") + path + " does not contain " + SeClientExe + " or " + SeDedicatedExe);
			}
			else if (fromRegistry)
				WriteLine("ERROR: " + path + " does not exist");

			path = null;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				if (args != null && args.Length != 0)
				{
					LoadBuilder.Load(args);
					return;
				}

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Manager());
			}
			catch (Exception ex)
			{
				CrashDump(ex);
				throw;
			}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly;
			if (TryResolveAssembly(PathBin64, args, out assembly) || TryResolveAssembly(PathDedicated64, args, out assembly))
				return assembly;
			WriteLine("Could not locate " + new AssemblyName(args.Name).Name);
			return null;
		}

		private static bool TryResolveAssembly(string directory, ResolveEventArgs args, out Assembly assembly)
		{
			if (directory == null)
			{
				// if SE is locally compiled or SEPL is installed in torch directory, this is normal
				WriteLine("null directory");
				assembly = null;
				return false;
			}

			AssemblyName name = new AssemblyName(args.Name);
			string assemblyPath = Path.Combine(directory, name.Name);

			string dll = assemblyPath + ".dll";
			if (File.Exists(dll))
				assemblyPath = dll;
			else
			{
				string exe = assemblyPath + ".exe";
				if (File.Exists(exe))
					assemblyPath = exe;
				else
				{
					assembly = null;
					return false;
				}
			}

			WriteLine("located assembly " + name.Name + " in " + directory);
			assembly = Assembly.LoadFrom(assemblyPath);
			return true;

			// for checking version:
			//string fullName = AssemblyName.GetAssemblyName(assemblyPath).FullName;
			//if (args.Name == fullName)
			//{
			//	assembly = Assembly.LoadFrom(assemblyPath);
			//	return true;
			//}

			//WriteLine("Rejecting partial match: " + fullName);

			//assembly = null;
			//return false;
		}

		private static void WriteLine(string line)
		{
			Console.WriteLine(line);
			_consoleOut.Add(line);
		}

		private static string GetCrashLogPath()
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "crash.log");
		}

		private static void EraseCrashLog()
		{
			string crashLog = GetCrashLogPath();
			if (File.Exists(crashLog))
				File.Delete(crashLog);
		}

		private static void CrashDump(Exception ex)
		{
			string crashLog = GetCrashLogPath();
			using (StreamWriter writer = new StreamWriter(crashLog))
			{
				foreach (string line in _consoleOut)
					writer.WriteLine(line);
				writer.WriteLine(ex.ToString());
			}

			MessageBox.Show(SeplName + " crashed, see\n" + crashLog, SeplName);
		}

	}
}
