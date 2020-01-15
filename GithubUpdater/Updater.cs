using System;
using System.Diagnostics;
using System.IO;
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
        public event EventHandler DownloadingStarted;
        /// <summary>
        /// Called when the download progressed.
        /// </summary>
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressed;
        /// <summary>
        /// Called when a download is complete
        /// </summary>
        public event EventHandler DownloadingCompleted;
        /// <summary>
        /// Called when installing a update has started
        /// </summary>
        public event EventHandler InstallingUpdateStarted;
        /// <summary>
        /// Called when a installation is completed
        /// </summary>
        public event EventHandler InstallingCompleted;

        /// <summary>
        /// The github username of the repository owner.
        /// </summary>
        public string GithubUsername;
        /// <summary>
        /// The github repository name.
        /// </summary>
        public string GithubRepositoryName;

        public UpdaterState State { get; private set; }

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

            State = UpdaterState.GettingRepository;

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
            State = UpdaterState.Idle;
        }

        /// <summary>
        /// Gets the repository from github.
        /// </summary>
        void GetRepository()
        {
            if (GithubUsername == null || GithubRepositoryName == null)
                return;

            State = UpdaterState.GettingRepository;

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
            State = UpdaterState.Idle;
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

            State = UpdaterState.CheckingForUpdate;

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion, currentVersion));
                State = UpdaterState.Idle;
                return true;
            }

            State = UpdaterState.Idle;
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

            State = UpdaterState.CheckingForUpdate;

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion, currentVersion));
                State = UpdaterState.Idle;
                return true;
            }

            State = UpdaterState.Idle;
            return false;
        }

        /// <summary>
        /// Downloads the new EXE from github.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public void DownloadUpdate()
        {
            DownloadingStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Downloading;

            if (client == null)
                client = new WebClient();
            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");
            if (repository.Assets[0].Name.EndsWith(".zip"))
                throw new FileLoadException("The downloaded file is a zip file, which is not supported");

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            client.DownloadFile(repository.Assets[0].BrowserDownloadUrl, destination);
            downloadedAssetPath = destination;

            State = UpdaterState.Idle;
            DownloadingCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Downloads the new EXE from github.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public async Task DownloadUpdateAsync()
        {
            DownloadingStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Downloading;

            if (client == null)
                client = new WebClient();
            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");
            if (repository.Assets[0].Name.EndsWith(".zip"))
                throw new FileLoadException("The downloaded file is a zip file, which is not supported");

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            downloadedAssetPath = destination;

            client.DownloadProgressChanged += DownloadProgressChanged;
            await client.DownloadFileTaskAsync(repository.Assets[0].BrowserDownloadUrl, destination);

            State = UpdaterState.Idle;
            DownloadingCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Calls the DownloadProgressed event
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">Args to be passed to the event</param>
        void DownloadProgressChanged(object sender,DownloadProgressChangedEventArgs args)
        {
            DownloadProgressed?.Invoke(this, new DownloadProgressEventArgs(args.ProgressPercentage, args.BytesReceived, args.TotalBytesToReceive));
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        public void InstallUpdate()
        {
            InstallingUpdateStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Installing;

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            File.Delete(Path.GetTempPath() + "GithubUpdaterBackup.backup");

            // Move current exe to backup.
            File.Move(Process.GetCurrentProcess().MainModule.FileName, Path.GetTempPath() + "GithubUpdaterBackup.backup");

            // Move downloaded exe to the correct folder.
            File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);

            State = UpdaterState.Idle;
            InstallingCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        public async Task InstallUpdateAsync()
        {
            InstallingUpdateStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Installing;

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            await Task.Run(() =>
            {
                // Move current exe to backup.
                File.Move(Process.GetCurrentProcess().MainModule.FileName, Path.GetTempPath() + "GithubUpdaterBackup.backup", true);

                // Move downloaded exe to the correct folder.
                File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);
            });

            State = UpdaterState.Idle;
            InstallingCompleted?.Invoke(this, EventArgs.Empty);
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
                    State = UpdaterState.RollingBack;

                    // Move downloaded exe to the correct folder.
                    File.Move(Path.GetTempPath() + "GithubUpdaterBackup.backup", Process.GetCurrentProcess().MainModule.FileName, true);

                    State = UpdaterState.Idle;
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
            foreach (Delegate item in DownloadingStarted.GetInvocationList())
            {
                DownloadingStarted -= (EventHandler)item;
            }
            DownloadingStarted = null;
            foreach (Delegate item in DownloadProgressed.GetInvocationList())
            {
                DownloadProgressed -= (EventHandler<DownloadProgressEventArgs>)item;
            }
            DownloadProgressed = null;
            foreach (Delegate item in DownloadingCompleted.GetInvocationList())
            {
                DownloadingCompleted -= (EventHandler)item;
            }
            DownloadingCompleted = null;
            foreach (Delegate item in InstallingUpdateStarted.GetInvocationList())
            {
                InstallingUpdateStarted -= (EventHandler)item;
            }
            InstallingUpdateStarted = null;
            foreach (Delegate item in InstallingCompleted.GetInvocationList())
            {
                InstallingCompleted -= (EventHandler)item;
            }
            InstallingCompleted = null;
        }
    }
}
