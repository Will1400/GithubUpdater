using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace GithubUpdater
{
    public class Updater
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

        public Updater(string githubUsername, string githubRepositoryName)
        {
            GithubUsername = githubUsername;
            GithubRepositoryName = githubRepositoryName;
        }

        public Updater()
        {

        }

        async Task GetRepositoryAsync()
        {
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

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion));
                return true;
            }

            return false;
        }

        public void CheckForUpdate()
        {
            GetRepository();

            Version currentVersion = Version.ConvertToVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());
            Version newestVersion = Version.ConvertToVersion(repository.TagName);

            if (currentVersion < newestVersion)
            {
                UpdateAvailable?.Invoke(this, new VersionEventArgs(newestVersion));
            }
        }

        public void DownloadUpdate()
        {
            DownloadingUpdate?.Invoke(this, EventArgs.Empty);

            WebClient client = new WebClient();
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

        public void InstallUpdate()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);


            string backupPath = Path.GetTempPath() + "Backup";

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            Directory.Move(Assembly.GetEntryAssembly().Location, backupPath);
            if (isDownloadedAssetAFolder)
            {
                Directory.Move(downloadedAssetPath, AppDomain.CurrentDomain.BaseDirectory + "Test");
            }
            else
            {
                File.Move(downloadedAssetPath, AppDomain.CurrentDomain.BaseDirectory + Path.GetFileName(downloadedAssetPath));
            }

            InstallingComplete?.Invoke(this, EventArgs.Empty);
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
    }
}
