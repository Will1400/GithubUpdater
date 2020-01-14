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
                Console.WriteLine("Update available: " + v.Version);
            };

            updater.DownloadingComplete += (s, e) =>
            {
                Console.WriteLine("Installing update");
                try
                {
                    updater.InstallUpdate();
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!!!: " + ex.Message);
                }
            };

            updater.InstallingComplete += (s, e) => { Console.WriteLine("Installed"); };

            updater.CheckForUpdate();


            Console.WriteLine("end");
            Console.ReadKey();
        }
    }
}
