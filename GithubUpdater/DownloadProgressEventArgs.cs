﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GithubUpdater
{
    public class DownloadProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the bytes received of the download.
        /// </summary>
        public long BytesReceived { get; set; }
        /// <summary>
        /// Gets the total size of the download.
        /// </summary>
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
