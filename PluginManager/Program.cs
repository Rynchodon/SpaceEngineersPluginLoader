using Rynchodon.PluginLoader;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Rynchodon.PluginManager
{
	public static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				if (args != null && args.Length != 0)
				{
					if (args[0].Equals("--CreateTemplates", StringComparison.CurrentCultureIgnoreCase))
						CreateTemplates();
					else
						AddLocallyCompiled(args);
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

		private static void CreateTemplates()
		{
			PluginBuilder template = new PluginBuilder()
			{
				author = "Author",
				repo = "Repo",
				files = new PluginBuilder.File[]
				{
					new PluginBuilder.File("\\Path\\To\\LoadFirst.dll", null, null),
					new PluginBuilder.File("\\Path\\To\\LoadSecond.dll", null, new string[] { "LoadFirst.dll" })
				},
				requires = new PluginName[] { new PluginName("OtherAuthor", "OtherRepo") }
			};
			WriteFile("template.json", true, template);
			WriteFile("template.xml", false, template);
		}

		private static void AddLocallyCompiled(string[] args)
		{
			// figure out which arg is the file
			string builderFilePath = null;
			foreach (string a in args)
				if (File.Exists(a))
				{
					builderFilePath = a;
					break;
				}

			if (builderFilePath == null)
			{
				Logger.WriteLine("File path to "+typeof(PluginBuilder).Name+" file not found");
				return;
			}

			PluginBuilder builder;
			if (!TryDeserialize(builderFilePath, out builder))
				return;

			if (builder == null)
				// I don't really expect this
				throw new NullReferenceException("no idea what happened");

			if (builder.oAuthToken != null)
				Logger.WriteLine("WARNING: OAuth Token specified in builder file. Be sure to keep your OAuth Token secret!");

			if (!AmmendPluginBuilder(args, builderFilePath, builder))
				return;

			if (builder.files == null || builder.files.Length == 0)
			{
				Logger.WriteLine("No files to include in plugin");
				return;
			}

			if (!ResolveFilePath(builder, builderFilePath))
				return;

			Logger.WriteLine("Command line accepted.");
			Loader.AddLocallyCompiledPlugin(builder);
		}

		/// <summary>
		/// Ammends builder with command line arguments of the form "field[=:]value".
		/// </summary>
		private static bool AmmendPluginBuilder(string[] args, string builderFilePath, PluginBuilder builder)
		{
			char[] separator = new char[] { '=', ':' };

			bool success = true;
			foreach (string a in args)
				if (a != builderFilePath)
				{
					string[] split = a.Split(separator, 2);
					if (split.Length != 2)
					{
						success = false;
						Logger.WriteLine("Invalid argument: " + a);
						continue;
					}
					FieldInfo field = typeof(PluginBuilder).GetField(split[0]);
					if (field == null)
					{
						success = false;
						Logger.WriteLine("Invalid argument: " + a + ", " + split[0] + " is not a field");
						continue;
					}
					object o;
					try { o = Convert.ChangeType(split[1], field.FieldType); }
					catch
					{
						success = false;
						Logger.WriteLine("Invalid argument: " + a + ", cannot convert " + split[1] + " to " + field.FieldType);
						continue;
					}
					field.SetValue(builder, o);
					Logger.WriteLine("Set " + a);
				}

			return success;
		}

		/// <summary>
		/// Converts file paths to absolute paths.
		/// </summary>
		private static bool ResolveFilePath(PluginBuilder builder, string builderFilePath)
		{
			string builderDirectory = Path.GetDirectoryName(builderFilePath);

			bool success = true;
			foreach (var file in builder.files)
			{
				if (File.Exists(file.source))
				{
					if (!Path.IsPathRooted(file.source))
						file.source = Path.GetFullPath(file.source);
				}
				else
				{
					string fromBuildDir = PathExtensions.Combine(builderDirectory, file.source);
					if (File.Exists(fromBuildDir))
						file.source = Path.GetFullPath(fromBuildDir);
					else
					{
						success = false;
						Logger.WriteLine("Could not locate the file: " + Path.GetFileName(file.source));
						Logger.WriteLine("\tNot at " + Path.GetFullPath(file.source));
						Logger.WriteLine("\tNot at " + Path.GetFullPath(fromBuildDir));
					}
				}
			}

			return success;
		}

		private static bool TryDeserialize(string filePath, out PluginBuilder builder)
		{
			string jsonExcept, xmlExcept;
			string extension = Path.GetExtension(filePath);

			if (extension == ".json")
			{
				if (TryDeserialize(filePath, true, out builder, out jsonExcept))
					return true;
				Logger.WriteLine("Failed to deserialize as json: " + filePath);
				Logger.WriteLine();
				Logger.WriteLine("json exception: " + jsonExcept);
				Logger.WriteLine();
				return false;
			}
			else if (extension == ".xml")
			{
				if (TryDeserialize(filePath, false, out builder, out xmlExcept))
					return true;
				Logger.WriteLine("Failed to deserialize as xml: " + filePath);
				Logger.WriteLine();
				Logger.WriteLine("xml exception: " + xmlExcept);
				Logger.WriteLine();
				return false;
			}
			else
			{
				if (TryDeserialize(filePath, true, out builder, out jsonExcept))
					return true;
				if (TryDeserialize(filePath, false, out builder, out xmlExcept))
					return true;
				Logger.WriteLine("Failed to deserialize as json or xml: " + filePath);
				Logger.WriteLine();
				Logger.WriteLine("json exception: " + jsonExcept);
				Logger.WriteLine();
				Logger.WriteLine("xml exception: " + xmlExcept);
				Logger.WriteLine();
				return false;
			}
		}

		private static bool TryDeserialize(string filePath, bool json, out PluginBuilder builder, out string except)
		{
			try
			{
				if (json)
					Serialization.ReadJson(filePath, out builder);
				else
					Serialization.ReadXml(filePath, out builder);

				except = null;
				return true;
			}
			catch (Exception ex)
			{
				builder = null;
				except = ex.ToString();
				return false;
			}
		}

		private static void WriteFile(string filePath, bool json, PluginBuilder builder)
		{
			if (json)
				Serialization.WriteJson(filePath, builder, true);
			else
				Serialization.WriteXml(filePath, builder, true);
		}

	}
}
