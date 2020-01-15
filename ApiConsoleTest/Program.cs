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
                Console.WriteLine("Update available: " + v.NewVersion);
            };

            updater.DownloadingCompleted += (s, e) =>
            {
                Console.WriteLine("Installing update");
                try
                {
                    updater.InstallUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error!!!: " + ex.Message);
                }
            };
            updater.InstallingCompleted += async (s, e) =>
            {
                Console.WriteLine("Installed");
                await updater.RollbackAsync();
            };

            updater.CheckForUpdate();


            Console.WriteLine("end");
            Console.ReadKey();
        }
    }
}
