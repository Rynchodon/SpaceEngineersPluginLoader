using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Test for git being ready for publishing.
	/// </summary>
	internal sealed class GitChecks
	{

		private enum CheckResult : byte
		{
			None, Passed, Failed, Ignored
		}

		/// <summary>
		/// Tests for being on the default branch, the branch being up-to-date, and the branch not being dirty.
		/// </summary>
		/// <param name="directory">Any directory that is part of the repository</param>
		/// <param name="pathToGit">Path to git.exe</param>
		/// <returns>True if all the tests were passed or the user skipped them.</returns>
		public static bool Check(PluginBuilder builder, string pathToGit)
		{
			return (new GitChecks(builder, pathToGit)).Check();
		}

		private readonly PluginBuilder _builder;
		private readonly string _pathToGit, _repoDirectory;

		private GitChecks(PluginBuilder builder, string pathToGit)
		{
			this._builder = builder;
			this._pathToGit = pathToGit;

			foreach (string path in PathExtensions.PathsToRoot(builder.files.First().source))
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

			string masterBranch = GetMasterBranch();

			CheckResult result = Check(() => string.IsNullOrWhiteSpace(GetStatus(true)), "The branch is dirty");
			switch (result)
			{
				case CheckResult.Passed:
					Logger.WriteLine("Branch is clean");
					break;
				case CheckResult.Ignored:
					Logger.WriteLine("WARNING: commitish will be incorrect");
					return true;
				case CheckResult.Failed:
					return false;
				default:
					throw new Exception("Bad case: " + result);
			}

			result = Check(() => GetStatus().Contains("up-to-date"), "Branch not up to date");
			switch (result)
			{
				case CheckResult.Passed:
					Logger.WriteLine("Branch is up to date");
					break;
				case CheckResult.Ignored:
					Logger.WriteLine("WARNING: commitish will be incorrect");
					return true;
				case CheckResult.Failed:
					return false;
				default:
					throw new Exception("Bad case: " + result);
			}

			result = Check(() => GetStatus().Contains("On branch " + masterBranch), "Not on " + masterBranch + " branch\nIgnore will update commit target");
			switch (result)
			{
				case CheckResult.Passed:
					Logger.WriteLine("Branch is " + masterBranch);
					break;
				case CheckResult.Ignored:
					UpdateCommitish();
					return true;
				case CheckResult.Failed:
					return false;
				default:
					throw new Exception("Bad case: " + result);
			}

			return true;
		}

		private CheckResult Check(Func<bool> test, string failMessage)
		{
			if (test.Invoke())
				return CheckResult.Passed;

			DialogResult result = MessageBox.Show(failMessage, "Git: not ready", MessageBoxButtons.AbortRetryIgnore);
			Logger.WriteLine(failMessage + " - " + result);
			switch (result)
			{
				case DialogResult.Abort:
					return CheckResult.Failed;
				case DialogResult.Retry:
					return Check(test, failMessage);
				case DialogResult.Ignore:
					return CheckResult.Ignored;
				default:
					throw new Exception("Bad result: " + result);
			}
		}

		/// <summary>
		/// Get the target commit from the head of the current branch.
		/// </summary>
		private void UpdateCommitish()
		{
			string headPath = PathExtensions.Combine(_repoDirectory, ".git", "HEAD");
			string head;
			using (StreamReader reader = new StreamReader(headPath))
				head = reader.ReadLine();

			const string refstring = "ref: ";
			if (head.StartsWith(refstring))
			{
				headPath = PathExtensions.Combine(_repoDirectory, ".git", head.Substring(refstring.Length));
				using (StreamReader reader = new StreamReader(headPath))
					head = reader.ReadLine();
			}

			Logger.WriteLine("Setting target to commit to \"" + head + '"');
			_builder.release.target_commitish = head;
		}

		private string GetMasterBranch()
		{
			Process gitBranch = new Process();
			gitBranch.StartInfo.FileName = _pathToGit;
			gitBranch.StartInfo.RedirectStandardOutput = true;
			gitBranch.StartInfo.UseShellExecute = false;

			PluginName name = new PluginName(_builder.author, _builder.repository);
			gitBranch.StartInfo.Arguments = "-C \"" + _repoDirectory + "\" ls-remote";
			gitBranch.Start();

			string output = gitBranch.StandardOutput.ReadToEnd();
			gitBranch.WaitForExit();

			string[] newline = new string[] { Environment.NewLine, "\n", "\r" };
			string[] references = output.Split(newline, StringSplitOptions.RemoveEmptyEntries);
			char[] space = new char[] { ' ', '\t' };

			// grab HEAD commit
			string headCommit = null;
			foreach (string reference in references)
				if (reference.Contains("HEAD"))
					headCommit = reference.Split(space, StringSplitOptions.RemoveEmptyEntries)[0];

			if (headCommit == null)
				throw new Exception("Failed to get HEAD");

			Logger.WriteLine("HEAD commit: \'" + headCommit + '"');

			// find matching commit
			foreach (string reference in references)
				if (reference.StartsWith(headCommit))
				{
					string refHeadMaster = reference.Split(space, StringSplitOptions.RemoveEmptyEntries)[1];
					const string refHead = "refs/heads/";
					if (refHeadMaster.StartsWith(refHead))
					{
						string defaultBranchName = refHeadMaster.Substring(refHead.Length);
						Logger.WriteLine("Default branch name: \"" + defaultBranchName + '"');
						return defaultBranchName;
					}
				}

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
