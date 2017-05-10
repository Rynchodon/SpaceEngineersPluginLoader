using System;
using Sandbox.Game.Gui;
using VRage.Utils;
using Timers = System.Timers;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Creates a GUI screen to show download progress. This screen prevents the player from doing anything until it closes.
	/// </summary>
	class DownloadProgress : MyGuiScreenProgressAsync, IDisposable
	{
		internal class Stats
		{
			public int Current, Total;
		}

		private class TaskWrapper : IMyAsyncResult
		{
			private readonly ParallelTasks.Task _task;

			public bool IsCompleted { get { return _task.IsComplete; } }
			public ParallelTasks.Task Task { get { return _task; } }

			public TaskWrapper(ParallelTasks.Task task)
			{
				_task = task;
			}
		}

		public delegate int Progress();

		private static DownloadProgress _instance;

		private static IMyAsyncResult beginAction()
		{
			Logger.WriteLine("opening download screen");
			return _instance._task;
		}

		private static void endAction(IMyAsyncResult arg1, MyGuiScreenProgressAsync arg2)
		{
			Logger.WriteLine("closing download screen");
			arg2.CloseScreen();
			_instance.Dispose();
		}

		private Stats _stats;
		private TaskWrapper _task;
		private Timers.Timer _timer;

		public DownloadProgress(ParallelTasks.Task task, Stats stats)
			: base(MyStringId.NullOrEmpty, null, beginAction, endAction)
		{
			Logger.WriteLine("entered");

			FriendlyName = typeof(DownloadProgress).Name;

			_instance = this;
			_stats = stats;
			_task = new TaskWrapper(task);
			_timer = new Timers.Timer(100d);
			_timer.Elapsed += Update;
			_timer.Start();
		}

		public void Update(object sender, Timers.ElapsedEventArgs e)
		{
			ProgressTextString = Loader.SeplShort + ": Downloading plugin " + _stats.Current + " of " + _stats.Total;
		}

		public void Dispose()
		{
			_instance = null;
			_stats = null;
			_task = null;
			_timer.Dispose();
			_timer = null;
		}
	}
}
