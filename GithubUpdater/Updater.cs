using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubUpdater
{
    public class Updater : IDisposable
    {
        /// <summary>
        /// Called when there is a update available
        /// </summary>
        public event EventHandler<VersionEventArgs> UpdateAvailable;
        /// <summary>
        /// Called at the beginning of a download.
        /// </summary>
        public event EventHandler DownloadingUpdate;
        /// <summary>
        /// Called when a download is complete
        /// </summary>
        public event EventHandler DownloadingComplete;
        /// <summary>
        /// Called when installing a update
        /// </summary>
        public event EventHandler InstallingUpdate;
        /// <summary>
        /// Called when a installation is complete
        /// </summary>
        public event EventHandler InstallingComplete;

        /// <summary>
        /// The github username of the repository owner.
        /// </summary>
        public string GithubUsername;
        /// <summary>
        /// The github repository name.
        /// </summary>
        public string GithubRepositoryName;

        private const string baseUri = "https://api.github.com/repos/";
        private Repository repository;
        private string downloadedAssetPath;
        private WebClient client;

        public Updater(string githubUsername, string githubRepositoryName)
        {
            GithubUsername = githubUsername;
            GithubRepositoryName = githubRepositoryName;
        }

        /// <summary>
        /// Gets the repository from github.
        /// </summary>
        async Task GetRepositoryAsync()
        {
            if (GithubUsername == null || GithubRepositoryName == null)
                return;

            Uri uri = new Uri(baseUri + $"{GithubUsername}/{GithubRepositoryName}/releases/latest");
            string json;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "GithubUpdater";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                json = await reader.ReadToEndAsync();
            }

            repository = Repository.FromJson(json);
        }

        /// <summary>
        /// Gets the repository from github.
        /// </summary>
        void GetRepository()
        {
            if (GithubUsername == null || GithubRepositoryName == null)
                return;

            Uri uri = new Uri(baseUri + $"{GithubUsername}/{GithubRepositoryName}/releases/latest");
            string json;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "GithubUpdater";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }

            repository = Repository.FromJson(json);
        }

        /// <summary>
        /// Gets the the repository, then checks if there is a new version available.
        /// </summary>
        /// <returns>True if there is a new version</returns>
        public async Task<bool> CheckForUpdateAsync()
        {
            await GetRepositoryAsync();

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the the repository, then checks if there is a new version available.
        /// </summary>
        /// <returns>True if there is a new version</returns>
        public bool CheckForUpdate()
        {
            GetRepository();

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Downloads the new EXE from github.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public void DownloadUpdate()
        {
            DownloadingUpdate?.Invoke(this, EventArgs.Empty);

            if (client == null)
                client = new WebClient();
            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");
            if (repository.Assets[0].Name.EndsWith(".zip"))
                throw new FileLoadException("The downloaded file is a zip file, which is not supported");

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            client.DownloadFile(repository.Assets[0].BrowserDownloadUrl, destination);
            downloadedAssetPath = destination;

            DownloadingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Downloads the new EXE from github.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public async Task DownloadUpdateAsync()
        {
            DownloadingUpdate?.Invoke(this, EventArgs.Empty);

            if (client == null)
                client = new WebClient();
            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");
            if (repository.Assets[0].Name.EndsWith(".zip"))
                throw new FileLoadException("The downloaded file is a zip file, which is not supported");

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            await client.DownloadFileTaskAsync(repository.Assets[0].BrowserDownloadUrl, destination);
            downloadedAssetPath = destination;


            DownloadingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        public void InstallUpdate()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

                File.Delete(Path.GetTempPath() + "GithubUpdaterBackup.backup");

                // Move current exe to backup.
                File.Move(Process.GetCurrentProcess().MainModule.FileName, Path.GetTempPath() + "GithubUpdaterBackup.backup");

                // Move downloaded exe to the correct folder.
                File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);

            InstallingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public async Task InstallUpdateAsync()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            await Task.Run(() =>
            {
                    // Move current exe to backup.
                    File.Move(Process.GetCurrentProcess().MainModule.FileName, Path.GetTempPath() + "GithubUpdaterBackup.backup", true);

                    // Move downloaded exe to the correct folder.
                    File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);
            });

            InstallingComplete?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Replaces the current EXE with a backup
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public async Task RollbackAsync()
        {
            await Task.Run(() =>
            {
                if (File.Exists(Path.GetTempPath() + "GithubUpdaterBackup.backup"))
                {
                    // Move downloaded exe to the correct folder.
                    File.Move(Path.GetTempPath() + "GithubUpdaterBackup.backup", Process.GetCurrentProcess().MainModule.FileName, true);
                }
                else
                {
                    throw new FileNotFoundException("Backup file not found");
                }
            });
        }

        public void Dispose()
        {
            repository = null;
            client.Dispose();
            
            // Remove all listeners to events
            foreach (Delegate item in UpdateAvailable.GetInvocationList())
            {
                UpdateAvailable -= (EventHandler<VersionEventArgs>)item;   
            }
            UpdateAvailable = null;
            foreach (Delegate item in DownloadingUpdate.GetInvocationList())
            {
                DownloadingUpdate -= (EventHandler)item;
            }
            DownloadingUpdate = null;
            foreach (Delegate item in DownloadingComplete.GetInvocationList())
            {
                DownloadingComplete -= (EventHandler)item;
            }
            DownloadingComplete = null;
            foreach (Delegate item in InstallingUpdate.GetInvocationList())
            {
                InstallingUpdate -= (EventHandler)item;
            }
            InstallingUpdate = null;
            foreach (Delegate item in InstallingComplete.GetInvocationList())
            {
                InstallingComplete -= (EventHandler)item;
            }
            InstallingComplete = null;
        }
    }
}
