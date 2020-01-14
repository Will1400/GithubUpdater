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
        private WebClient client;

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

        public void InstallUpdate()
        {
            InstallingUpdate?.Invoke(this, EventArgs.Empty);


            if (!isDownloadedAssetAFolder)
            {
                File.Delete(Path.GetTempPath() + "IdkBackupOfSomething.randombackup");

                Console.WriteLine(Environment.CurrentDirectory + "\\" + repository.Assets[0].Name);
                File.Move(Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, Path.GetTempPath() + "IDKBackupOfSomething.randombackup");

                Console.WriteLine("Moving file");
                File.Move(downloadedAssetPath, Environment.CurrentDirectory + "\\" + repository.Assets[0].Name, true);

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
