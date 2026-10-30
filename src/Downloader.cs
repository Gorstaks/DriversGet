using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace DriversGet
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public int Percent;
        public long BytesReceived;
        public long TotalBytes;
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success;
        public string FilePath;
        public string Error;
    }

    // Wraps WebClient to download directly over HTTP(S) without needing a
    // browser installed. Reports progress and completion on the UI thread
    // when a SynchronizingObject is supplied.
    public class Downloader
    {
        private WebClient _client;
        private string _targetPath;

        public event EventHandler<DownloadProgressEventArgs> Progress;
        public event EventHandler<DownloadCompletedEventArgs> Completed;

        public bool IsBusy
        {
            get { return _client != null; }
        }

        public void Start(string url, string folder, string suggestedFileName)
        {
            if (IsBusy)
                throw new InvalidOperationException("A download is already in progress.");

            DriverSources.EnsureTls();

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = SafeFileName(url, suggestedFileName);
            _targetPath = Path.Combine(folder, fileName);

            _client = new WebClient();
            _client.Headers.Add(HttpRequestHeader.UserAgent,
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0 Safari/537.36 DriversGet/1.0");

            _client.DownloadProgressChanged += OnProgress;
            _client.DownloadFileCompleted += OnCompleted;

            try
            {
                _client.DownloadFileAsync(new Uri(url), _targetPath);
            }
            catch (Exception ex)
            {
                CleanupClient();
                RaiseCompleted(false, null, ex.Message);
            }
        }

        public void Cancel()
        {
            if (_client != null)
            {
                try { _client.CancelAsync(); }
                catch { }
            }
        }

        private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            EventHandler<DownloadProgressEventArgs> h = Progress;
            if (h != null)
            {
                h(this, new DownloadProgressEventArgs
                {
                    Percent = e.ProgressPercentage,
                    BytesReceived = e.BytesReceived,
                    TotalBytes = e.TotalBytesToReceive
                });
            }
        }

        private void OnCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string path = _targetPath;
            CleanupClient();

            if (e.Cancelled)
            {
                TryDelete(path);
                RaiseCompleted(false, null, "Download cancelled.");
                return;
            }
            if (e.Error != null)
            {
                TryDelete(path);
                RaiseCompleted(false, null, e.Error.Message);
                return;
            }
            RaiseCompleted(true, path, null);
        }

        private void RaiseCompleted(bool success, string path, string error)
        {
            EventHandler<DownloadCompletedEventArgs> h = Completed;
            if (h != null)
            {
                h(this, new DownloadCompletedEventArgs
                {
                    Success = success,
                    FilePath = path,
                    Error = error
                });
            }
        }

        private void CleanupClient()
        {
            if (_client != null)
            {
                try
                {
                    _client.DownloadProgressChanged -= OnProgress;
                    _client.DownloadFileCompleted -= OnCompleted;
                    _client.Dispose();
                }
                catch { }
                _client = null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }

        private static string SafeFileName(string url, string suggested)
        {
            string name = suggested;
            if (string.IsNullOrEmpty(name))
            {
                try
                {
                    Uri u = new Uri(url);
                    name = Path.GetFileName(u.LocalPath);
                }
                catch { }
            }
            if (string.IsNullOrEmpty(name))
                name = "driver_download.exe";

            // Strip anything not filename-safe.
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
