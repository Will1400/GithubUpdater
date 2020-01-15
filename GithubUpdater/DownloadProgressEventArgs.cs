using System;
using System.Collections.Generic;
using System.Text;

namespace GithubUpdater
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesReceived { get; set; }
        public long TotalBytesToReceive { get; set; }
        /// <summary>
        /// The current Progress in percent
        /// </summary>
        public int ProgressPercent { get; set; }

        public DownloadProgressEventArgs(int progressPercent, long bytesReceived, long totalBytesToReceive)
        {
            ProgressPercent = progressPercent;
            BytesReceived = bytesReceived;
            TotalBytesToReceive = totalBytesToReceive;
        }
    }
}
