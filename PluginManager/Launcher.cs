using Microsoft.Win32;
using System;
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

		static Launcher()
		{
			try
			{
				string seplDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				PathPluginLoader = Path.Combine(seplDirectory, "PluginLoader.dll");

				string seDirectory = Path.GetDirectoryName(seplDirectory);
				PathBin64 = Path.Combine(seDirectory, SeBinFolder);
				PathDedicated64 = Path.Combine(seDirectory, SeDedicatedFolder);

				if (!File.Exists(Path.Combine(PathBin64, SeClientExe)))
					PathBin64 = null;
				if (!File.Exists(Path.Combine(PathDedicated64, SeDedicatedExe)))
					PathDedicated64 = null;

				if (PathBin64 != null || PathDedicated64 != null)
				{
					Console.WriteLine("Located " + SeFolder + " relative to assembly directory");
					return;
				}

				string installLocation = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850", "InstallLocation", null);
				if (installLocation == null)
					installLocation = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850", "InstallLocation", null);
				if (installLocation == null)
				{
					MessageBox.Show(SeFolder + " could not be located");
					return;
				}

				Console.WriteLine("Using " + SeFolder + " path from registry");

				PathBin64 = Path.Combine(installLocation, SeBinFolder);
				PathDedicated64 = Path.Combine(installLocation, SeDedicatedFolder);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), SeplName);
				throw;
			}
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
				MessageBox.Show(ex.ToString(), SeplName);
				throw;
			}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly;
			if (TryResolveAssembly(PathBin64, args, out assembly) || TryResolveAssembly(PathDedicated64, args, out assembly))
				return assembly;
			Console.WriteLine("Could not locate " + args.Name);
			return null;
		}

		private static bool TryResolveAssembly(string directory, ResolveEventArgs args, out Assembly assembly)
		{
			if (directory == null)
			{
				// if SE is locally compiled or SEPL is installed in torch directory, this is normal
				Console.WriteLine("null directory");
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

			Console.WriteLine("located assembly " + name.Name + " in " + directory);
			assembly = Assembly.LoadFrom(assemblyPath);
			return true;

			// for checking version:
			//string fullName = AssemblyName.GetAssemblyName(assemblyPath).FullName;
			//if (args.Name == fullName)
			//{
			//	assembly = Assembly.LoadFrom(assemblyPath);
			//	return true;
			//}

			//Console.WriteLine("Rejecting partial match: " + fullName);

			//assembly = null;
			//return false;
		}

	}
}
