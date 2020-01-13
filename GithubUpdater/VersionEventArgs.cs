using System;
using System.Collections.Generic;
using System.Text;

namespace GithubUpdater
{
    public class VersionEventArgs : EventArgs
    {
        public VersionEventArgs(Version version)
        {
            Version = version;
        }

        public Version Version { get; set; }
    }
}
