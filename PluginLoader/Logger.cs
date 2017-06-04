using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Rynchodon.PluginLoader
{
	public static class Logger
	{

		[Flags]
		public enum LogTo { None = 0, File = 1, StandardOut = 2, StandardError = 4 }

		private static readonly StreamWriter _streamWriter;

		static Logger()
		{
			//string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			//while (!directory.EndsWith("SpaceEngineers"))
			//	directory = Path.GetDirectoryName(directory);

			string logDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
			Directory.CreateDirectory(logDirectory);

			string logFileName = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".log";
			string logFile = Path.Combine(logDirectory, logFileName);
			_streamWriter = new StreamWriter(logFile);

			_streamWriter.WriteLine("Log opened on " + DateTime.Now.ToString("yyyy-MM-dd"));
			DeleteOldLogs(logDirectory);
		}

		private static void DeleteOldLogs(string logDirectory)
		{
			List<string> files = new List<string>(Directory.EnumerateFiles(logDirectory));
			files.Sort();

			int deletions = files.Count - 10;
			for (int index = 0; index < deletions; ++index)
				try
				{
					File.Delete(files[index]);
					WriteLine("Deleted log: " + files[index]);
				}
				catch (IOException)
				{
					WriteLine("Failed to delete log: " + files[index]);
				}
		}

		public static void WriteLine(string line = null, [CallerFilePath] string callerPath = null, [CallerMemberName] string memberName = null, [CallerLineNumber]int lineNumber = -1, LogTo logTo = LogTo.File)
		{
			callerPath = Path.GetFileNameWithoutExtension(callerPath);
			if (line == null)
				line = string.Empty;
			else
			{
				line = line.Replace('{', '[').Replace('}', ']');
				if (line.Contains(Environment.NewLine))
				{
					string[] splitByNewLine = line.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
					foreach (string nl in splitByNewLine)
						WriteLine(nl, callerPath, memberName, lineNumber);
					return;
				}
			}

			string toLog = '{' + DateTime.Now.ToString("HH:mm:ss.fff") + "}{" + callerPath + "}{" + memberName + "}{" + lineNumber + "}{" + line + '}';

			if ((logTo & LogTo.File) != 0)
			{
				_streamWriter.WriteLine('{' + DateTime.Now.ToString("HH:mm:ss.fff") + "}{" + callerPath + "}{" + memberName + "}{" + lineNumber + "}{" + line + '}');
				_streamWriter.Flush();
			}
			if ((logTo & LogTo.StandardOut) != 0)
				Console.Out.WriteLine(logTo);
			if ((logTo & LogTo.StandardError) != 0)
				Console.Error.WriteLine(logTo);
		}

	}
}
