using System;
using System.Collections.Generic;
using System.Text;

namespace GithubUpdater
{
    public class VersionEventArgs : EventArgs
    {
        public VersionEventArgs(Version newVersion, Version currentVersion)
        {
            NewVersion = newVersion;
            CurrentVersion = currentVersion;
        }

        public Version NewVersion { get; private set; }

        public Version CurrentVersion { get; private set; }
    }
}
