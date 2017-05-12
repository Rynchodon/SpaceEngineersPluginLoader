using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rynchodon.PluginLoader
{
	/// <summary>
	/// Connects to GitHub and sends and receives release information.
	/// </summary>
	public class GitHubClient
	{

		public readonly PluginName name;
		private readonly string _oAuthToken, _userAgent;

		private Task<Release[]> _releaseDownload;
		private bool _releaseDownloadFailed;
		private Release[] _releases;

		/// <summary>True if oAuthToken was provided or retrieved from environment variable, false otherwise.</summary>
		public bool HasOAuthToken { get { return _oAuthToken != null; } }

		/// <summary>
		/// Create a GitHubClient and start downloading release information.
		/// </summary>
		/// <param name="plugin">Name of the plugin</param>
		/// <param name="oAuthToken">Authentication token for GitHub, not required for updating.</param>
		/// <param name="userAgent">Name of the application</param>
		public GitHubClient(PluginName plugin, string oAuthToken = null, string userAgent = "Rynchodon:" + Loader.SeplShort)
		{
			this.name = plugin;
			this._oAuthToken = oAuthToken ?? Environment.GetEnvironmentVariable("oAuthToken");
			this._userAgent = userAgent;

			_releaseDownload = new Task<Release[]>(DownloadReleases);
			_releaseDownload.Start();
		}

		/// <summary>
		/// Publish a new release.
		/// </summary>
		/// <param name="create">Release information.</param>
		/// <param name="assetsPaths">Assets to be included, do not include folders. It is recommended that you compress the files and pass the zip file path to this method.</param>
		public bool PublishRelease(CreateRelease create, params string[] assetsPaths)
		{
			if (_oAuthToken == null)
				throw new NullReferenceException("Cannot publish if authentication token is null");

			if (assetsPaths == null || assetsPaths.Length == 0)
				throw new ArgumentException("No Assets, cannot publish");
			foreach (string path in assetsPaths)
				if (!File.Exists(path))
					throw new ArgumentException("File does not exist: " + path);

			string fail;
			if (!CanCreateRelease(create.version, out fail))
			{
				Logger.WriteLine(fail);
				return false;
			}

			// release needs to be draft while it is being created, in case of failure
			bool draft = create.draft;
			create.draft = true;

			HttpWebRequest request = WebRequest.CreateHttp(name.releases_site);
			request.UserAgent = _userAgent;
			request.Method = "POST";
			request.Headers.Add("Authorization", "token " + _oAuthToken);

			using (Stream requestStream = request.GetRequestStream())
				create.WriteCreateJson(requestStream);
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Release));
			Release release;
			using (WebResponse response = request.GetResponse())
			using (Stream responseStream = response.GetResponseStream())
				release = (Release)serializer.ReadObject(responseStream);

			Logger.WriteLine("Release id: " + release.id);

			try
			{
				foreach (string asset in assetsPaths)
				{
					string fileName = Path.GetFileName(asset);
					request = WebRequest.CreateHttp(name.uploads_site + '/' + release.id + "/assets?name=" + fileName);
					request.UserAgent = _userAgent;
					request.Method = "POST";
					request.ContentType = "application/" + Path.GetExtension(fileName).Substring(1);
					request.Headers.Add("Authorization", "token " + _oAuthToken);

					using (Stream upStream = request.GetRequestStream())
					using (FileStream fileRead = new FileStream(asset, FileMode.Open))
						fileRead.CopyTo(upStream);

					Logger.WriteLine("Posting: " + fileName);
					request.GetResponse().Dispose();
				}
			}
			catch (WebException ex)
			{
				Logger.WriteLine("Failed to post asset(s)\n" + ex);
				DeleteRelease(release);
				throw;
			}

			if (!draft)
			{
				release.draft = draft;
				EditRelease(ref release);
			}

			_releases = null; // needs to be updated

			Logger.WriteLine("Release published");
			return true;
		}

		/// <summary>
		/// Checks if a release with a specified version can be created.
		/// </summary>
		/// <param name="version">A version that might be in use.</param>
		/// <param name="reason">In the event of failure, the reason for the failure.</param>
		public bool CanCreateRelease(Version version, out string reason)
		{
			Release[] releases = GetReleases();
			if (releases == null)
			{
				reason = "Failed to download releases";
				return false;
			}

			foreach (Release release in releases)
				if (release.version.CompareTo(version) == 0)
				{
					reason = "Release exists: " + version;
					return false;
				}

			reason = null;
			return true;
		}

		/// <summary>
		/// Edit information about a release.
		/// </summary>
		/// <param name="edit">The new information for the release.</param>
		public void EditRelease(ref Release edit)
		{
			if (_oAuthToken == null)
				throw new NullReferenceException("Cannot edit if authentication token is null");

			HttpWebRequest request = WebRequest.CreateHttp(name.releases_site + '/' + edit.id);
			request.UserAgent = _userAgent;
			request.Method = "PATCH";
			request.Headers.Add("Authorization", "token " + _oAuthToken);

			using (Stream requestStream = request.GetRequestStream())
				edit.WriteCreateJson(requestStream);
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Release));
			using (WebResponse response = request.GetResponse())
			using (Stream responseStream = response.GetResponseStream())
				edit = (Release)serializer.ReadObject(responseStream);

			Logger.WriteLine("Release edited: " + edit.tag_name);

			_releases = null; // needs to be updated
		}

		/// <summary>
		/// Delete a release from GitHub
		/// </summary>
		/// <param name="delete">The release to delete</param>
		public void DeleteRelease(Release delete)
		{
			if (_oAuthToken == null)
				throw new NullReferenceException("Cannot delete if authentication token is null");

			HttpWebRequest request = WebRequest.CreateHttp(name.releases_site + '/' + delete.id);
			request.UserAgent = _userAgent;
			request.Method = "DELETE";
			request.Headers.Add("Authorization", "token " + _oAuthToken);

			using (Stream requestStream = request.GetRequestStream())
				request.GetResponse().Dispose();

			Logger.WriteLine("Release deleted: " + delete.tag_name);

			_releases = null; // needs to be updated
		}

		/// <summary>
		/// Get all the releases, the value is cached and only updated after PublishRelease, EditRelease, or DeleteRelease.
		/// The array should not be modified.
		/// </summary>
		/// <returns>An array representing all the GitHub releases for this plugin.</returns>
		public Release[] GetReleases()
		{
			if (_releases == null)
			{
				if (_releaseDownload == null)
				{
					if (_releaseDownloadFailed)
						return null;
					_releaseDownload = new Task<Release[]>(DownloadReleases);
					_releaseDownload.Start();
				}
				try
				{
					_releaseDownload.Wait();
					_releases = _releaseDownload.Result;
				}
				catch (AggregateException aex)
				{
					_releaseDownloadFailed = true;
					Logger.WriteLine("Failed to download releases:\n" + aex);
				}
				_releaseDownload.Dispose();
				_releaseDownload = null;
			}
			return _releases;
		}

		/// <summary>
		/// Download all releases for this plugin from GitHub.
		/// </summary>
		/// <returns>All the releases for this plugin.</returns>
		private Release[] DownloadReleases()
		{
			HttpWebRequest request = WebRequest.CreateHttp(name.releases_site);
			request.UserAgent = _userAgent;
			if (_oAuthToken != null)
				request.Headers.Add("Authorization", "token " + _oAuthToken);

			using (WebResponse response = request.GetResponse())
			{
				DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Release[]));
				using (Stream responseStream = response.GetResponseStream())
					return (Release[])serializer.ReadObject(responseStream);
			}
		}

		internal void Publish(Plugin plugin, PluginBuilder.Release releaseBuilder)
		{
			if (!HasOAuthToken)
				throw new ArgumentException("Need oAuthToken");

			CreateRelease release = new CreateRelease(plugin.version, true);
			release.target_commitish = releaseBuilder.target_commitish;
			release.name = releaseBuilder.name;
			release.body = releaseBuilder.body;
			release.prerelease = releaseBuilder.prerelease;

			string zipFileName;
			if (releaseBuilder.zipFileName != null)
				zipFileName = Path.GetFileName(releaseBuilder.zipFileName);
			else
				zipFileName = plugin.name.repository;

			if (!zipFileName.EndsWith(".zip"))
				zipFileName = zipFileName + ".zip";

			string zipFilePath = PathExtensions.Combine(plugin.directory, zipFileName);
			try
			{
				plugin.Zip(zipFilePath);
				if (PublishRelease(release, zipFilePath))
					Console.WriteLine("Release published");
				else
					Console.WriteLine("Publish failed, see log for details");
			}
			finally
			{
				if (File.Exists(zipFilePath))
					File.Delete(zipFilePath);
			}
		}

		/// <summary>
		/// Download an update for a plugin.
		/// </summary>
		/// <returns>True iff the plugin was updated.</returns>
		internal bool Update(Plugin plugin)
		{
			Release[] releases = GetReleases();
			if (releases == null)
				// already complained about it in depth
				return false;

			int seVersion = Loader.GetCurrentSEVersion();

			Release mostRecent = null;
			foreach (Release rel in releases)
			{
				if (rel.draft)
					continue;
				if (rel.prerelease && !plugin.config.downloadPrerelease)
					continue;

				// skip if release was compiled with a newer version of SE
				if (seVersion < rel.version.SeVersion)
					continue;

				if (mostRecent == null || mostRecent.version.CompareTo(rel.version) < 0)
					mostRecent = rel;
			}

			if (mostRecent == null)
			{
				Logger.WriteLine("ERROR: No available releases");
				return false;
			}

			Logger.WriteLine("Latest release version: " + mostRecent.version + ", Current version: " + plugin.version);

			int relative = mostRecent.version.CompareTo(plugin.version);
			if (relative == 0)
			{
				Logger.WriteLine("Up-to-date: " + plugin.version);
				return false;
			}
			if (relative < 0) // current version is newer than latest release
			{
				if (plugin.locallyCompiled)
				{
					Logger.WriteLine("Keeping locally compiled version: " + plugin.version);
					return false;
				}
				Logger.WriteLine("Roll back version: " + plugin.version + " to " + mostRecent.version);
			}

			if (mostRecent.assets == null || mostRecent.assets.Length == 0)
			{
				Logger.WriteLine("ERROR: Release has no assets");
				return false;
			}

			// warn if a locally compiled version is going to be replaced by a downloaded version
			if (plugin.locallyCompiled && MessageBox.Show("Plugin: " + plugin.name.fullName + "\nLocally compiled version: " + plugin.version + "\nLatest release version: " + mostRecent.version + "\n\nOverwrite locally compiled plugin?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
			{
				Logger.WriteLine("Not overwriting locally compiled plugin");
				return false;
			}

			plugin.EraseAllFiles();

			Logger.WriteLine("Downloading version: " + mostRecent.version);

			Directory.CreateDirectory(plugin.directory);

			foreach (Release.Asset asset in mostRecent.assets)
			{
				Logger.WriteLine("Downloading asset: " + asset.name);
				HttpWebRequest request = WebRequest.CreateHttp(asset.browser_download_url);
				request.Accept = "application/octet-stream";
				request.UserAgent = _userAgent;

				WebResponse response = request.GetResponse();
				Stream responseStream = response.GetResponseStream();
				string assetDestination = PathExtensions.Combine(plugin.directory, asset.name);

				if (asset.name.EndsWith(".zip"))
					try
					{
						using (FileStream zipFile = new FileStream(assetDestination, FileMode.CreateNew))
							responseStream.CopyTo(zipFile);

						Logger.WriteLine("Unpacking: " + asset.name);
						using (ZipArchive archive = ZipFile.OpenRead(assetDestination))
							foreach (ZipArchiveEntry entry in archive.Entries)
							{
								string entryDestination = PathExtensions.Combine(plugin.directory, entry.FullName);
								Directory.CreateDirectory(Path.GetDirectoryName(entryDestination));

								if (File.Exists(entryDestination))
								{
									Logger.WriteLine("ERROR: File exists: " + entryDestination);
									return false;
								}

								entry.ExtractToFile(entryDestination);
								plugin.AddFile(entryDestination);
							}
					}
					finally
					{
						if (File.Exists(assetDestination))
							File.Delete(assetDestination);
					}
				else
				{
					if (File.Exists(assetDestination))
					{
						Logger.WriteLine("ERROR: File exists: " + assetDestination);
						return false;
					}

					using (FileStream file = new FileStream(assetDestination, FileMode.CreateNew))
						responseStream.CopyTo(file);
					plugin.AddFile(assetDestination);
				}

				responseStream.Dispose();
				response.Dispose();
			}

			if (!plugin.LoadManifest())
				return false;

			plugin.version = mostRecent.version;
			plugin.locallyCompiled = false;

			return true;
		}

	}
}
