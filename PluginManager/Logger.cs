using System;
using System.Runtime.CompilerServices;

namespace Rynchodon.PluginManager
{
	internal static class Logger
	{

		public static void WriteLine(string line = null, bool skipMemeberName = false, [CallerMemberName] string memberName = null)
		{
			if (line != null && !skipMemeberName)
				line = DateTime.Now + ": " + memberName + ": " + line;
			Console.WriteLine(line);
		}

	}
}
