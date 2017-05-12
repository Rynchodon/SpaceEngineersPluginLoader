using Rynchodon.PluginLoader;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Rynchodon.PluginManager
{
	internal static class DllInjector
	{

		const string dll = "PluginLoader.dll", processNameSE = "SpaceEngineers", processNameSED = "SpaceEngineersDedicated";

		public static void Run(string bin64, string launchFrom)
		{
			Logger.WriteLine("loaded");

			string dllPath = PathExtensions.Combine(bin64, dll);

			if (!File.Exists(dllPath))
				Logger.WriteLine("dll not found: " + dll);

			string dedicatedLauncher = PathExtensions.Combine(launchFrom, "SpaceEngineersDedicated.exe");
			bool isDedicatedServer = File.Exists(dedicatedLauncher);

			Logger.WriteLine("looking for process");

			Process process = GetGameProcess(isDedicatedServer);

			if (process == null)
				Logger.WriteLine("Process not found");
			else
				Logger.WriteLine("Found SE process: " + process.ProcessName);

			if (process == null)
			{
				if (isDedicatedServer)
				{
					Logger.WriteLine("Starting dedicated server");
					Process.Start(dedicatedLauncher);
				}
				else
				{
					string pathToSteam = null;
					foreach (string upPath in PathExtensions.PathsToRoot(launchFrom))
					{
						string steamExe = PathExtensions.Combine(upPath, "Steam.exe");
						if (File.Exists(steamExe))
							pathToSteam = steamExe;
					}

					if (pathToSteam != null)
					{
						Logger.WriteLine("Starting Space Engineers");
						Process.Start(pathToSteam, "-applaunch 244850");
					}
					else
					{
						string launcher = PathExtensions.Combine(launchFrom, "SpaceEngineers.exe");
						if (File.Exists(launcher))
						{
							Logger.WriteLine("Alternate launch of Space Engineers");
							Process.Start("steam://rungameid/244850");
							process = WaitForGameStart(false, false);
							while (!process.WaitForExit(100))
							{
								process.Refresh();
								if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
								{
									Logger.WriteLine("Window has title");
									break;
								}
							}
						}
						else
						{
							Logger.WriteLine("Game not found");
							return;
						}
					}
				}

				Logger.WriteLine("Game launched");
			}

			process = WaitForGameStart(isDedicatedServer);
			if (process != null)
				Inject(process, dllPath);
		}

		private static Process GetGameProcess(bool isDedicatedServer)
		{
			Process[] processes = Process.GetProcessesByName(isDedicatedServer ? processNameSED : processNameSE);

			Process newest = null;
			foreach (Process process in processes)
				if (newest == null || process.StartTime > newest.StartTime)
					newest = process;

			return newest;
		}

		private static Process WaitForGameStart(bool isDedicatedServer, bool needTitle = true, int seconds = 600)
		{
			Process process = null;

			// wait for process

			int waitCount = seconds * 10;
			for (int c = 0; c < waitCount; c++)
			{
				Thread.Sleep(100);
				process = GetGameProcess(isDedicatedServer);

				if (process != null)
					break;
			}

			if (process == null)
			{
				Logger.WriteLine("Game did not start");
				return null;
			}

			Logger.WriteLine("Found process: " + process.ProcessName);

			if (!needTitle)
				return process;

			// wait for title

			for (int c2 = 0; c2 < 6000; c2++)
			{
				Thread.Sleep(100);

				if (process.HasExited)
				{
					Logger.WriteLine("Game terminated before it finished loading");
					return null;
				}

				process.Refresh();
				if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
				{
					if ((process.MainWindowTitle.Contains(" - Select Instance of Dedicated server") || process.MainWindowTitle.Contains(" - Dedicated server configurator"))) // these are hard-coded
					{
						Logger.WriteLine("Configurator is running");
						process.WaitForExit();
						return WaitForGameStart(isDedicatedServer, seconds: 10);
					}
					Logger.WriteLine("Found process: " + process.ProcessName);
					return process;
				}
			}

			Logger.WriteLine("Game did not start");
			return null;
		}

		private static bool Inject(Process process, string dllPath)
		{
			IntPtr hProcess = Kernel32Wrapper.OpenProcess(0x2 | 0x8 | 0x10 | 0x20 | 0x400, true, (uint)process.Id);

			if (hProcess == IntPtr.Zero)
			{
				Logger.WriteLine("Failed to get process handle");
				return false;
			}

			IntPtr lpAddress = IntPtr.Zero, hThread = IntPtr.Zero;

			try
			{
				IntPtr lpStartAddress = Kernel32Wrapper.GetProcAddress(Kernel32Wrapper.GetModuleHandle("kernel32.dll"), "LoadLibraryA");

				if (lpStartAddress == IntPtr.Zero)
				{
					Logger.WriteLine("Failed to get address for load library");
					return false;
				}

				lpAddress = Kernel32Wrapper.VirtualAllocEx(hProcess, IntPtr.Zero, (IntPtr)dllPath.Length, 0x1000 | 0x2000, 0x40);

				if (lpAddress == IntPtr.Zero)
				{
					Logger.WriteLine("Failed to allocate memory");
					return false;
				}

				if (!Kernel32Wrapper.WriteProcessMemory(hProcess, lpAddress, Encoding.ASCII.GetBytes(dllPath), (IntPtr)dllPath.Length))
				{
					Logger.WriteLine("Failed to write dll name");
					return false;
				}

				hThread = Kernel32Wrapper.CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, lpStartAddress, lpAddress, 0, IntPtr.Zero);

				if (hThread == IntPtr.Zero)
				{
					Logger.WriteLine("Failed to create loading thread");
					return false;
				}

				if (Kernel32Wrapper.WaitForSingleObject(hThread, 60000) != 0)
				{
					Logger.WriteLine("Loading thread timed out");
					return false;
				}

				Logger.WriteLine("Dll injected");

				Kernel32Wrapper.LoadLibrary(dllPath);

				lpStartAddress = Kernel32Wrapper.GetProcAddress(Kernel32Wrapper.GetModuleHandle(dllPath), "RunInSEProcess");

				if (lpStartAddress == IntPtr.Zero)
				{
					Logger.WriteLine("Failed to get RunInSEProcess address");
					return false;
				}

				Kernel32Wrapper.CloseHandle(hThread);

				hThread = Kernel32Wrapper.CreateRemoteThread(hProcess, IntPtr.Zero, IntPtr.Zero, lpStartAddress, IntPtr.Zero, 0, IntPtr.Zero);

				if (hThread == IntPtr.Zero)
				{
					Logger.WriteLine("Failed to create run thread");
					return false;
				}

				while (Kernel32Wrapper.WaitForSingleObject(hThread, 1000) != 0)
				{
					process.Refresh();
					if (process.HasExited)
					{
						Logger.WriteLine("Game terminated before " + dll + " could be loaded");
						return false;
					}
				}

				Logger.WriteLine("Loaded " + dll);
			}
			finally
			{
				if (hThread != IntPtr.Zero)
					Kernel32Wrapper.CloseHandle(hThread);

				if (lpAddress != IntPtr.Zero)
					Kernel32Wrapper.VirtualFreeEx(hProcess, lpAddress, IntPtr.Zero, 0x8000);

				Kernel32Wrapper.CloseHandle(hProcess);
			}

			return true;
		}

	}
}
