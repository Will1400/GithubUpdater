using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubUpdater
{
    public class Updater : IDisposable
    {
        public event EventHandler<VersionEventArgs> UpdateAvailable;
        public event EventHandler DownloadingUpdate;
        public event EventHandler DownloadingComplete;
        public event EventHandler InstallingUpdate;
        public event EventHandler InstallingComplete;

        public string GithubUsername;
        public string GithubRepositoryName;

        private const string baseUri = "https://api.github.com/repos/";
        private Repository repository;
        private bool isDownloadedAssetAFolder;
        private string downloadedAssetPath;
        private WebClient client;

        public Updater(string githubUsername, string githubRepositoryName)
        {
            GithubUsername = githubUsername;
            GithubRepositoryName = githubRepositoryName;
        }

        public Updater() { }

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

        public void DownloadUpdate()
        {
            DownloadingUpdate?.Invoke(this, EventArgs.Empty);

            if (client == null)
                client = new WebClient();

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            client.DownloadFile(repository.Assets[0].BrowserDownloadUrl, destination);
            downloadedAssetPath = destination;

            if (Path.GetExtension(destination) == ".zip")
            {
                ExtractZipFile(destination, destination.Replace(".zip", ""));
                downloadedAssetPath = destination.Replace(".zip", "");
                isDownloadedAssetAFolder = true;
            }
            else
            {
                isDownloadedAssetAFolder = false;
            }

            DownloadingComplete?.Invoke(this, EventArgs.Empty);
        }

        public async Task DownloadUpdateAsync()
        {
            DownloadingUpdate?.Invoke(this, EventArgs.Empty);

            if (client == null)
                client = new WebClient();
            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            string destination = Path.GetTempPath() + repository.Assets[0].Name;
            await client.DownloadFileTaskAsync(repository.Assets[0].BrowserDownloadUrl, destination);
            downloadedAssetPath = destination;

            if (Path.GetExtension(destination) == ".zip")
            {
                await ExtractZipFileAsync(destination, destination.Replace(".zip", ""));
                downloadedAssetPath = destination.Replace(".zip", "");
                isDownloadedAssetAFolder = true;
            }
            else
            {
                isDownloadedAssetAFolder = false;
            }

            DownloadingComplete?.Invoke(this, EventArgs.Empty);
        }

        public void InstallUpdate()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            if (!isDownloadedAssetAFolder)
            {
                File.Delete(Path.GetTempPath() + "GithubUpdaterBackup.backup");

                // Move current exe to backup.
                File.Move(Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, Path.GetTempPath() + "GithubUpdaterBackup.backup");

                // Move downloaded exe to the correct folder.
                File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);
            }

            InstallingComplete?.Invoke(this, EventArgs.Empty);
        }


        public async Task InstallUpdateAsync()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);

            if (repository == null)
                throw new NullReferenceException("Could not retrieve Repository");

            await Task.Run(() =>
            {
                if (!isDownloadedAssetAFolder)
                {
                    File.Delete(Path.GetTempPath() + "GithubUpdaterBackup.backup");

                    // Move current exe to backup.
                    File.Move(Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, Path.GetTempPath() + "GithubUpdaterBackup.backup");

                    // Move downloaded exe to the correct folder.
                    File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);
                }
            });

            InstallingComplete?.Invoke(this, EventArgs.Empty);
        }

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

        void ExtractZipFile(string zipLocation, string destinationPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(destinationPath);

            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
            ZipFile.ExtractToDirectory(zipLocation, destinationPath);
        }

        async Task ExtractZipFileAsync(string zipLocation, string destinationPath)
        {
            await Task.Run(() =>
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(destinationPath);

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }
                ZipFile.ExtractToDirectory(zipLocation, destinationPath);
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
