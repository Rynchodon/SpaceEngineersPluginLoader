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

		public static readonly string PathPluginLoader, PathBin64, PathDedicated64;

		static Launcher()
		{
			string seplDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string seDirectory = Path.GetDirectoryName(seplDirectory);

			PathPluginLoader = Path.Combine(seplDirectory, "PluginLoader.dll");
			PathBin64 = Path.Combine(seDirectory, "Bin64");
			PathDedicated64 = Path.Combine(seDirectory, "DedicatedServer64");
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
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

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly assembly;
			if (TryResolveAssembly(PathBin64, args, out assembly) || TryResolveAssembly(PathDedicated64, args, out assembly))
				return assembly;
			return null;
		}

		private static bool TryResolveAssembly(string directory, ResolveEventArgs args, out Assembly assembly)
		{
			if (directory == null)
			{
				Console.Error.WriteLine("null directory");
				assembly = null;
				return false;
			}

			AssemblyName name = new AssemblyName(args.Name);
			string assemblyPath = Path.Combine(directory, name.Name);

			Console.WriteLine("Looking for " + args.Name + " in " + directory);

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

			string fullName = AssemblyName.GetAssemblyName(assemblyPath).FullName;
			if (args.Name == fullName)
			{
				assembly = Assembly.LoadFrom(assemblyPath);
				return true;
			}

			Console.WriteLine("Rejecting partial match: " + fullName);

			assembly = null;
			return false;
		}

	}
}
