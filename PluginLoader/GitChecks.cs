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

		private const string IncorrectCommitTarget = "WARNING: Commit target does not match HEAD";
		private const string PromptCaption = "Git check failed";
		
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

			string masterBranch = GitMasterBranch();

			CheckResult result = Check(() => string.IsNullOrWhiteSpace(GitStatus(true)), "The branch is dirty");
			switch (result)
			{
				case CheckResult.Passed:
					Logger.WriteLine("Branch is clean");
					break;
				case CheckResult.Ignored:
					Logger.WriteLine(IncorrectCommitTarget);
					return true;
				case CheckResult.Failed:
					return false;
				default:
					throw new Exception("Bad case: " + result);
			}

			result = Check(() => GitStatus().Contains("up-to-date"), "Branch not up to date");
			switch (result)
			{
				case CheckResult.Passed:
					Logger.WriteLine("Branch is up to date");
					break;
				case CheckResult.Ignored:
					Logger.WriteLine(IncorrectCommitTarget);
					return true;
				case CheckResult.Failed:
					return false;
				default:
					throw new Exception("Bad case: " + result);
			}

			string target_commit = GitRevParse(_builder.release.target_commitish ?? masterBranch);
			string head_commit = GitRevParse("HEAD");

			if (target_commit == head_commit)
			{
				Logger.WriteLine("Target commit approved");
				return true;
			}

			if (GitStatus().Contains("On branch " + masterBranch))
			{
				DialogResult changeTargetToMaster = MessageBox.Show("On branch " + masterBranch + " but commit target is " + target_commit + ".\nChange commit target to master?", PromptCaption, MessageBoxButtons.YesNoCancel);
				Logger.WriteLine("Change commit to master" + " - " + changeTargetToMaster);
				switch (changeTargetToMaster)
				{
					case DialogResult.Yes:
						string head = LocalHead();
						Logger.WriteLine("Setting target to commit to \"" + head + '"');
						_builder.release.target_commitish = head;
						return true;
					case DialogResult.No:
						Logger.WriteLine(IncorrectCommitTarget);
						return true;
					case DialogResult.Cancel:
						return false;
					default:
						throw new Exception("Bad case: " + changeTargetToMaster);
				}
			}

			DialogResult preReleaseFromOther = MessageBox.Show("Not on " + masterBranch + " branch.\nCreate a pre-release from the current branch?", PromptCaption, MessageBoxButtons.YesNoCancel);
			Logger.WriteLine("Create pre-release" + " - " + preReleaseFromOther);
			switch (preReleaseFromOther)
			{
				case DialogResult.Yes:
					string head = LocalHead();
					Logger.WriteLine("Setting target to commit to \"" + head + '"');
					_builder.release.prerelease = true;
					_builder.release.target_commitish = head;
					return true;
				case DialogResult.No:
					Logger.WriteLine(IncorrectCommitTarget);
					return true;
				case DialogResult.Cancel:
					return false;
				default:
					throw new Exception("Bad case: " + preReleaseFromOther);
			}
		}

		private CheckResult Check(Func<bool> test, string failMessage)
		{
			if (test.Invoke())
				return CheckResult.Passed;

			DialogResult result = MessageBox.Show(failMessage, PromptCaption, MessageBoxButtons.AbortRetryIgnore);
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
		/// Get the commit hash of local head.
		/// </summary>
		/// <returns>The commit hash of local head.</returns>
		private string LocalHead()
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

			return head;
		}

		private string RunGitCommand(string command)
		{
			Process git = new Process();
			git.StartInfo.FileName = _pathToGit;
			git.StartInfo.RedirectStandardOutput = true;
			git.StartInfo.UseShellExecute = false;
			git.StartInfo.Arguments = "-C \"" + _repoDirectory + "\" " + command;
			git.Start();

			string output = git.StandardOutput.ReadToEnd();
			git.WaitForExit();
			return output;
		}

		private string GitMasterBranch()
		{
			string output = RunGitCommand("ls-remote");

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

		private string GitRevParse(string s)
		{
			return RunGitCommand("rev-parse " + s);
		}

		private string GitStatus(bool shortSwitch = false)
		{
			string args = "status";
			if (shortSwitch)
				args += " -s";

			return RunGitCommand(args);
		}

	}
}
