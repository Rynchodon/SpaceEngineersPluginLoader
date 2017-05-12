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
		public static string PathBin64, PathDedicated64;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				string seDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

				PathBin64 = Path.Combine(seDirectory, "Bin64");
				PathDedicated64 = Path.Combine(seDirectory, "DedicatedServer64");

				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

				if (args != null && args.Length != 0)
				{
					if (args[0].Equals("--CreateTemplates", StringComparison.CurrentCultureIgnoreCase))
						LoadBuilder.CreateTemplates();
					else
						LoadBuilder.AddLocallyCompiled(args);
					return;
				}

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Manager());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Console.ReadKey();
				throw;
			}
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

			if (args.Name == AssemblyName.GetAssemblyName(assemblyPath).FullName)
			{
				assembly = Assembly.LoadFrom(assemblyPath);
				return true;
			}

			assembly = null;
			return false;
		}

	}
}
