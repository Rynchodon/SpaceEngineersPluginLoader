using System;
using System.Runtime.InteropServices;

namespace Rynchodon.PluginManager
{
	internal static class Kernel32Wrapper
	{

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

		[DllImport("kernel32.dll")]
		public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, uint dwFreeType);

		[DllImport("kernel32.dll")]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize);

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll")]
		public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, IntPtr dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFLags, IntPtr lpThreadId);

		[DllImport("kernel32.dll")]
		public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

	}
}
