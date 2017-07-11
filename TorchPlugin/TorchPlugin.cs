using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using VRage.Plugins;

namespace Rynchodon.Torch
{
	[Plugin("SpaceEngineersPluginLoader", "0.0", "65C6D899-E27A-4D0B-89D7-D042815CC4F7")]
	public sealed class TorchPlugin : TorchPluginBase
	{
		private IPlugin _loader;

		public TorchPlugin()
		{
			try
			{
				string pathToSepl = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Rynchodon\\Space Engineers Plugin Loader", "DirRemoveUninstall", null);
				if (pathToSepl == null || !Directory.Exists(pathToSepl))
				{
					Trace.Fail("SpaceEngineersPluginLoader has not been installed");
					return;
				}

				string seplLoaderPath = Path.Combine(pathToSepl, "PluginLoader.dll");
				if (!File.Exists(seplLoaderPath))
				{
					Trace.Fail("PluginLoader.dll was not found, SEPL will have to be reinstalled");
					return;
				}

				Assembly seplLoaderAssembly = Assembly.LoadFrom(seplLoaderPath);
				if (seplLoaderAssembly == null)
				{
					Trace.Fail("PluginLoader.dll is corrupt, SEPL will have to be reinstalled");
					return;
				}

				Type seplLoader = seplLoaderAssembly.GetType("Rynchodon.PluginLoader.Loader");
				if (seplLoader == null)
				{
					Trace.Fail("Failed to load type: Rynchodon.PluginLoader.Loader");
					return;
				}
				_loader = (IPlugin)Activator.CreateInstance(seplLoader);
			}
			catch (Exception ex)
			{
				Trace.Fail(ex.Message, ex.ToString());
			}
		}

		public override void Dispose()
		{
			_loader?.Dispose();
			_loader = null;
		}

		public override void Init(ITorchBase torchBase)
		{
			_loader?.Init(null);
		}

		public override void Update()
		{
			_loader?.Update();
		}
	}
}
