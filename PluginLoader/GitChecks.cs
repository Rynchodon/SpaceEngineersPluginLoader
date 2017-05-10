using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Test for git being ready for publishing.
	/// </summary>
	public class GitChecks
	{

		/// <summary>
		/// Tests for being on the default branch, the branch being up-to-date, and the branch not being dirty.
		/// </summary>
		/// <param name="directory">Any directory that is part of the repository</param>
		/// <param name="pathToGit">Path to git.exe</param>
		/// <returns>True if all the tests were passed or the user skipped them.</returns>
		public static bool Check(string directory, string pathToGit)
		{
			return (new GitChecks(directory, pathToGit)).Check();
		}

		private readonly string _pathToGit, _repoDirectory;

		private GitChecks(string directory, string pathToGit)
		{
			this._pathToGit = pathToGit;

			foreach (string path in PathExtensions.PathsToRoot(directory))
			{
				string gitDirectory = PathExtensions.Combine(path, ".git");
				if (Directory.Exists(gitDirectory))
				{
					_repoDirectory = path;
					break;
				}
			}
		}

		private bool Check()
		{
			if (_pathToGit == null)
			{
				Logger.WriteLine("Git checks not enabled");
				return true;
			}

			if (_repoDirectory == null)
			{
				Logger.WriteLine("Cannot check git, not in repository folder");
				return true;
			}

			string _masterBranch = GetMasterBranch();

			Func<bool>[] tests = new Func<bool>[] {
				() => GetStatus().Contains("On branch " + _masterBranch),
				() => GetStatus().Contains("up-to-date"),
				() => string.IsNullOrWhiteSpace(GetStatus(true)) };

			string[] failMessage = new string[] {
				"Not on " + _masterBranch + " branch",
				"Branch not up to date",
				"The branch is dirty" };

			for (int i = 0; i < tests.Length; ++i)
				while (!tests[i].Invoke())
				{
					DialogResult result = MessageBox.Show(failMessage[i], "Git not ready", MessageBoxButtons.AbortRetryIgnore);
					Logger.WriteLine(failMessage[i] + " - " + result);
					switch (result)
					{
						case DialogResult.Abort:
							return false;
						case DialogResult.Retry:
							break;
						case DialogResult.Ignore:
							return true;
						default:
							throw new Exception("Bad result: " + result);
					}
				}

			return true;
		}

		private string GetMasterBranch()
		{
			Process gitBranch = new Process();
			gitBranch.StartInfo.FileName = _pathToGit;
			gitBranch.StartInfo.RedirectStandardOutput = true;
			gitBranch.StartInfo.UseShellExecute = false;

			gitBranch.StartInfo.Arguments = "-C \"" + _repoDirectory + "\" branch --list";
			gitBranch.Start();

			string output = gitBranch.StandardOutput.ReadToEnd();
			gitBranch.WaitForExit();

			string[] branches = output.Split(new char[] { '\n', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string branch in branches)
				if (branch[0] == '*')
					return branch.Substring(2);

			throw new Exception("failed to identify master branch");
		}

		private string GetStatus(bool shortSwitch = false)
		{
			Process gitStatus = new Process();
			gitStatus.StartInfo.FileName = _pathToGit;
			gitStatus.StartInfo.RedirectStandardOutput = true;
			gitStatus.StartInfo.UseShellExecute = false;

			string args = "-C \"" + _repoDirectory + "\" status";
			if (shortSwitch)
				args += " -s";
			gitStatus.StartInfo.Arguments = args;
			gitStatus.Start();

			string output = gitStatus.StandardOutput.ReadToEnd();
			gitStatus.WaitForExit();
			return output;
		}

	}
}
