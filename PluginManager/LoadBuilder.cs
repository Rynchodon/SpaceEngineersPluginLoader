using Rynchodon.PluginLoader;
using System;
using System.IO;
using System.Reflection;

namespace Rynchodon.PluginManager
{
	/// <summary>
	/// Loads builder file and ammends with command line.
	/// </summary>
	internal static class LoadBuilder
	{

		public static void Load(string[] args)
		{
			try
			{
				if (args[0].Equals("--CreateTemplates", StringComparison.CurrentCultureIgnoreCase))
					CreateTemplates();
				else
					AddLocallyCompiled(args);
			}
			catch (Exception ex)
			{
				Logger.WriteLine(ex.ToString(), logTo: Logger.LogTo.File | Logger.LogTo.StandardError);
				throw;
			}
		}

		private static void CreateTemplates()
		{
			PluginBuilder template = new PluginBuilder()
			{
				author = "Author",
				repository = "Repo",
				files = new PluginBuilder.File[]
				{
					new PluginBuilder.File("\\Path\\To\\LoadFirst.dll", null, null),
					new PluginBuilder.File("\\Path\\To\\LoadSecond.dll", null, new string[] { "LoadFirst.dll" })
				},
				release = new PluginBuilder.Release(),
				requires = new PluginName[] { new PluginName("OtherAuthor", "OtherRepo") }
			};
			Logger.WriteLine("Creating templates at " + Path.GetFullPath("."), logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
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
				Logger.WriteLine("File path to " + typeof(PluginBuilder).Name + " file not found", logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				return;
			}

			PluginBuilder builder;
			if (!TryDeserialize(builderFilePath, out builder))
				return;

			if (builder == null)
				// I don't really expect this
				throw new NullReferenceException("no idea what happened");

			if (builder.oAuthToken != null)
				Logger.WriteLine("WARNING: OAuth Token specified in builder file. Be sure to keep your OAuth Token secret!", logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);

			if (!AmmendPluginBuilder(args, builderFilePath, builder))
				return;

			if (builder.files == null || builder.files.Length == 0)
			{
				Logger.WriteLine("No files to include in plugin", logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				return;
			}

			if (!ResolveFilePath(builder, builderFilePath))
				return;

			Logger.WriteLine("Command line accepted.", logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
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
						Logger.WriteLine("Invalid argument: " + a, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
						continue;
					}
					FieldInfo field = typeof(PluginBuilder).GetField(split[0]);
					if (field == null)
					{
						success = false;
						Logger.WriteLine("Invalid argument: " + a + ", " + split[0] + " is not a field", logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
						continue;
					}
					object o;
					try { o = Convert.ChangeType(split[1], field.FieldType); }
					catch
					{
						success = false;
						Logger.WriteLine("Invalid argument: " + a + ", cannot convert " + split[1] + " to " + field.FieldType, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
						continue;
					}
					field.SetValue(builder, o);
					Logger.WriteLine("Set " + a, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
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
						Logger.WriteLine("Could not locate the file: " + Path.GetFileName(file.source), logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
						Logger.WriteLine("\tNot at " + Path.GetFullPath(file.source), logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
						Logger.WriteLine("\tNot at " + Path.GetFullPath(fromBuildDir), logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
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
				Logger.WriteLine("Failed to deserialize as json: " + filePath, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine("json exception: " + jsonExcept, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				return false;
			}
			else if (extension == ".xml")
			{
				if (TryDeserialize(filePath, false, out builder, out xmlExcept))
					return true;
				Logger.WriteLine("Failed to deserialize as xml: " + filePath, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine("xml exception: " + xmlExcept, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				return false;
			}
			else
			{
				if (TryDeserialize(filePath, true, out builder, out jsonExcept))
					return true;
				if (TryDeserialize(filePath, false, out builder, out xmlExcept))
					return true;
				Logger.WriteLine("Failed to deserialize as json or xml: " + filePath, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine("json exception: " + jsonExcept, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine("xml exception: " + xmlExcept, logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
				Logger.WriteLine(logTo: Logger.LogTo.File | Logger.LogTo.StandardOut);
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
