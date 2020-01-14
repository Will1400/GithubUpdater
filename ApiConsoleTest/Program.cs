using GithubUpdater;
using System;

namespace ApiConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Updater updater = new Updater("domialex", "sidekick");
            Updater updater = new Updater("will1400", "ReleaseApiTest");

            updater.UpdateAvailable += (s, v) =>
            {
                updater.DownloadUpdate();
                Console.WriteLine("Update available");
            };

            updater.DownloadingComplete += (s, e) =>
            {
                updater.InstallUpdate();
                Console.WriteLine("Installing update");
            };

            updater.CheckForUpdate();


            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
