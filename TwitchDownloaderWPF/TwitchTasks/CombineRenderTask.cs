using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TwitchDownloaderCore;
using TwitchDownloaderCore.Options;
using TwitchDownloaderWPF.TwitchTasks;

namespace TwitchDownloader.TwitchTasks
{
    class CombineRenderTask : ITwitchTask
    {
        public TaskData Info { get; set; } = new TaskData();
        public int Progress { get; set; }
        public TwitchTaskStatus Status { get; private set; } = TwitchTaskStatus.Ready;
        public CancellationTokenSource TokenSource { get; set; } = new CancellationTokenSource();
        public VodDownloadTask DependantTask1 { get; set; }
        public ChatRenderTask DependantTask2 { get; set; }

        public ChatRenderOptions DownloadOptions { get; set; }
        public VideoDownloadOptions VDownloadOptions { get; set; }
        public string TaskType { get; } = "Combine Render";
        public TwitchTaskException Exception { get; private set; } = new();
        public ITwitchTask DependantTask { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Cancel()
        {
            TokenSource.Cancel();

            if (Status == TwitchTaskStatus.Running)
            {
                ChangeStatus(TwitchTaskStatus.Stopping);
                return;
            }

            ChangeStatus(TwitchTaskStatus.Cancelled);
        }

        public bool CanRun()
        {
            if (DependantTask1 == null || DependantTask2 == null)
            {
                if (Status == TwitchTaskStatus.Ready)
                {
                    return true;
                }
            }
            else if (Status == TwitchTaskStatus.Waiting)
            {
                if (DependantTask1.Status == TwitchTaskStatus.Finished && DependantTask2.Status == TwitchTaskStatus.Finished)
                {
                    return true;
                }
                if (DependantTask1.Status is TwitchTaskStatus.Failed or TwitchTaskStatus.Cancelled)
                {
                    ChangeStatus(TwitchTaskStatus.Cancelled);
                    return false;
                }
                if (DependantTask2.Status is TwitchTaskStatus.Failed or TwitchTaskStatus.Cancelled)
                {
                    ChangeStatus(TwitchTaskStatus.Cancelled);
                    return false;
                }
            }
            return false;
        }

        public void ChangeStatus(TwitchTaskStatus newStatus)
        {
            Status = newStatus;
            OnPropertyChanged(nameof(Status));
        }

        public async Task RunAsync()
        {
            Progress<ProgressReport> progress = new Progress<ProgressReport>();
            progress.ProgressChanged += Progress_ProgressChanged;
            ChangeStatus(TwitchTaskStatus.Running);
            try
            {
                //await renderer.RenderVideoAsync(progress, TokenSource.Token);
                var process = new System.Diagnostics.Process
                {
                    StartInfo =
                        {
                        FileName = DependantTask1.DownloadOptions.FfmpegPath,
                        Arguments = $"-i \"{DependantTask2.DownloadOptions.OutputFile}\" -i \"{DependantTask1.DownloadOptions.Filename}\" -filter_complex hstack=inputs=2 -c:v libx264 \"{System.IO.Path.ChangeExtension(DependantTask1.DownloadOptions.Filename, "-Combined.mp4")}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                        }

                };

                process.Start();
                process.WaitForExit();

                if (TokenSource.IsCancellationRequested)
                {
                    ChangeStatus(TwitchTaskStatus.Cancelled);
                }
                else
                {
                    ChangeStatus(TwitchTaskStatus.Finished);
                    Progress = 100;
                    OnPropertyChanged(nameof(Progress));
                }
            }
            catch (OperationCanceledException)
            {
                ChangeStatus(TwitchTaskStatus.Cancelled);
            }
            catch (Exception ex)
            {
                ChangeStatus(TwitchTaskStatus.Failed);
                Exception = new TwitchTaskException(ex);
                OnPropertyChanged(nameof(Exception));
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();



        }

        private void Progress_ProgressChanged(object sender, ProgressReport e)
        {
            if (e.ReportType == ReportType.Percent)
            {
                int percent = (int)e.Data;
                if (percent > Progress)
                {
                    Progress = percent;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
