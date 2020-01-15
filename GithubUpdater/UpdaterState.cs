using System;
using System.Collections.Generic;
using System.Text;

namespace GithubUpdater
{
    public enum UpdaterState
    {
        Idle,
        GettingRepository,
        CheckingForUpdate,
        Downloading,
        Installing,
        RollingBack
    }
}
