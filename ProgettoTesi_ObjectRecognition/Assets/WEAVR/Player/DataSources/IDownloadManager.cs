using System;
using System.Threading.Tasks;
using System.Threading;
using TXT.WEAVR.Communication;
using System.Collections.Generic;

namespace TXT.WEAVR.Player.DataSources
{

    public struct DownloadState
    {
        public static readonly DownloadState Invalid = new DownloadState()
        {
            status = Status.NotRegistered,
            progress = 0,
        };

        public enum Status
        {
            NotRegistered,
            NotStarted,
            InProgress,
            Cancelled,
            Faulted,
            Done,
        }

        public Status status;
        public float progress;
        public string error;
        public string destinationPath;

        public override bool Equals(object obj)
        {
            return obj is DownloadState state && status == state.status && destinationPath == state.destinationPath;
        }

        public override int GetHashCode()
        {
            return destinationPath?.GetHashCode() ?? base.GetHashCode();
        }

        public static bool operator ==(DownloadState left, DownloadState right)
        {
            return left.status == right.status && left.destinationPath == right.destinationPath;
        }

        public static bool operator !=(DownloadState left, DownloadState right)
        {
            return left.status != right.status || left.destinationPath != right.destinationPath;
        }
    }

    public interface IDownloadManager
    {
        IEnumerable<string> GetAllDownloads();
        DownloadState GetDownloadState(string downloadId, string filepath);
        IEnumerable<DownloadState> GetDownloadStates(string downloadId);
        void RegisterForProgress(string downloadId, Action<float> progressSetter);
        void UnregisterFromProgress(string downloadId, Action<float> progressSetter);
        Task FinishDownloadAsync(string downloadId, Action<float> progressSetter = null);
        Task StopDownload(string downloadId);
        Task StopDownload(string downloadId, string filepath);
        Task DownloadFileAsync(string downloadId, Request request, string filePath, Action<float> progressSetter = null, long fileSize = 0);
        void ClearTemporaryFiles();
        bool IsDownloadInProgress(string downloadId);
    }

    public interface IDownloadClient
    {
        IDownloadManager DownloadManager { get; set; }
    }
}
