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
        public event EventHandler<DownloadProgressEventArgs> DownloadingProgressed;
        /// <summary>
        /// Called when a download is complete
        /// </summary>
        public event EventHandler DownloadingCompleted;
        /// <summary>
        /// Called when installing a update has started
        /// </summary>
        public event EventHandler InstallationStarted;
        /// <summary>
        /// Called when the installation failed
        /// </summary>
        public event EventHandler<ExceptionEventArgs<Exception>> InstallationFailed;
        /// <summary>
        /// Called when a installation is completed
        /// </summary>
        public event EventHandler InstallationCompleted;

        /// <summary>
        /// The github username of the repository owner.
        /// </summary>
        public string GithubUsername;
        /// <summary>
        /// The github repository name.
        /// </summary>
        public string GithubRepositoryName;

        /// <summary>
        /// The current state of the Updater
        /// </summary>
        public UpdaterState State { get; private set; }

        private const string baseUri = "https://api.github.com/repos/";
        private Repository repository;
        private string downloadedAssetPath;
        private WebClient client;
        private string originalInstallPath;

        private string backupFileName = "GithubUpdaterBackup.backup";

        public Updater(string githubUsername, string githubRepositoryName)
        {
            GithubUsername = githubUsername;
            GithubRepositoryName = githubRepositoryName;

            originalInstallPath = Process.GetCurrentProcess().MainModule.FileName;
        }

        public Updater(string githubUsername, string githubRepositoryName, bool rollBackOnFail) : this(githubUsername, githubRepositoryName)
        {
            if (rollBackOnFail)
                InstallationFailed += (s, e) => { Rollback(); };
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
        /// <exception cref="NullReferenceException">Thrown when the Repository is null</exception> 
        /// <exception cref="FormatException">Thrown when the version was in a invalid format</exception>
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
        ///  <exception cref="NullReferenceException">Thrown when the Repository is null</exception>
        ///  <exception cref="FormatException">Thrown when the version was in a invalid format</exception>
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
        ///  <exception cref="NullReferenceException">Thrown when the Repository is null</exception>
        ///  <exception cref="FileLoadException">Thrown when the asset file is a .zip</exception>
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
        ///  <exception cref="NullReferenceException">Thrown when the Repository is null</exception>
        ///  <exception cref="FileLoadException">Thrown when the asset file is a .zip</exception>
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
        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs args)
        {
            DownloadingProgressed?.Invoke(this, new DownloadProgressEventArgs(args.ProgressPercentage, args.BytesReceived, args.TotalBytesToReceive));
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        ///  <exception cref="NullReferenceException">Thrown when the Repository is null</exception>
        public void InstallUpdate()
        {
            InstallationStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Installing;

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            try
            {
                string tempPath = Path.GetTempPath() + backupFileName;
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                // Move current exe to backup.
                File.Move(originalInstallPath, tempPath);

                // Move downloaded exe to the correct folder.
                File.Move(downloadedAssetPath, originalInstallPath, true);
            }
            catch (Exception ex)
            {
                InstallationFailed?.Invoke(this, new ExceptionEventArgs<Exception>(ex, ex.Message));
                return;
            }

            State = UpdaterState.Idle;
            InstallationCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Makes a backup of the current EXE, then overwrites it with the new EXE.
        /// </summary>
        /// <returns>Awaitable Task</returns>
        ///  <exception cref="NullReferenceException">Thrown when the Repository is null</exception>
        public async Task InstallUpdateAsync()
        {
            InstallationStarted?.Invoke(this, EventArgs.Empty);
            State = UpdaterState.Installing;

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            try
            {
                string tempPath = Path.GetTempPath() + backupFileName;
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                await Task.Delay(100);

                // Move current exe to backup.
                File.Move(originalInstallPath, tempPath);

                await Task.Delay(100);

                // Move downloaded exe to the correct folder.
                File.Move(downloadedAssetPath, originalInstallPath);
            }
            catch (Exception ex)
            {
                InstallationFailed?.Invoke(this, new ExceptionEventArgs<Exception>(ex, ex.Message));
                return;
            }

            State = UpdaterState.Idle;
            InstallationCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Replaces the current EXE with a backup
        /// </summary>
        /// <returns>Awaitable Task</returns>
        ///  <exception cref="FileNotFoundException">Thrown when the backup file could not be found</exception>
        public void Rollback()
        {
            try
            {
                if (File.Exists(Path.GetTempPath() + backupFileName))
                {
                    State = UpdaterState.RollingBack;

                    // Move downloaded exe to the correct folder.
                    File.Move(Path.GetTempPath() + backupFileName, originalInstallPath, true);

                    State = UpdaterState.Idle;
                }
                else
                {
                    throw new FileNotFoundException("Backup file not found");
                }
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            repository = null;
            client.Dispose();

            //// Remove all listeners to events
            //foreach (Delegate item in UpdateAvailable.GetInvocationList())
            //{
            //    UpdateAvailable -= (EventHandler<VersionEventArgs>)item;
            //}
            //UpdateAvailable = null;
            //foreach (Delegate item in DownloadingStarted.GetInvocationList())
            //{
            //    DownloadingStarted -= (EventHandler)item;
            //}
            //DownloadingStarted = null;
            //foreach (Delegate item in DownloadingProgressed.GetInvocationList())
            //{
            //    DownloadingProgressed -= (EventHandler<DownloadProgressEventArgs>)item;
            //}
            //DownloadingProgressed = null;
            //foreach (Delegate item in DownloadingCompleted.GetInvocationList())
            //{
            //    DownloadingCompleted -= (EventHandler)item;
            //}
            //DownloadingCompleted = null;
            //foreach (Delegate item in InstallationStarted.GetInvocationList())
            //{
            //    InstallationStarted -= (EventHandler)item;
            //}
            //InstallationStarted = null;
            //foreach (Delegate item in InstallationFailed.GetInvocationList())
            //{
            //    InstallationFailed -= (EventHandler<ExceptionEventArgs<Exception>>)item;
            //}
            //InstallationFailed = null;
            //foreach (Delegate item in InstallationCompleted.GetInvocationList())
            //{
            //    InstallationCompleted -= (EventHandler)item;
            //}
            //InstallationCompleted = null;
        }
    }
}
