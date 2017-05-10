using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Rynchodon.PluginLoader
{
	public static class Logger
	{

		public delegate void LineWriter(string line, string callerPath, string memberName, int lineNumber);

		internal static string logFile;

		private static LineWriter _lineWriter = WriteLineToStream;
		private static StreamWriter _writer;

		public static void RedirectLogging(Stream loggingStream)
		{
			_lineWriter = WriteLineToStream;
			_writer = new StreamWriter(loggingStream);
		}

		public static void RedirectLogging(LineWriter lineWriter)
		{
			_lineWriter = lineWriter;
		}

		internal static void WriteLine(string line, [CallerFilePath] string callerPath = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
		{
			_lineWriter(line, callerPath, memberName, lineNumber);
		}

		private static void WriteLineToStream(string line, string callerPath = null, string memberName = null, int lineNumber = 0)
		{
			if (logFile == null)
				throw new NullReferenceException("logFile");

			if (_writer == null)
				_writer = new StreamWriter(File.Open(logFile, FileMode.Create));

			callerPath = Path.GetFileName(callerPath);

			_writer.WriteLine(DateTime.Now.ToString() + ":" + callerPath + ":" + memberName + ":" + lineNumber + ":" + line);
			_writer.Flush();
		}

	}
}
