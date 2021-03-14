using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TXT.WEAVR.Communication;
using System.Threading;
using System.IO;
using UnityEngine.Networking;
using System.Linq;

namespace TXT.WEAVR.Player.DataSources
{
    public class DownloadManager : IDownloadManager
    {
        private Dictionary<string, DownloadGroup> m_downloads = new Dictionary<string, DownloadGroup>();
        private Dictionary<string, HashSet<Action<float>>> m_progressCallbacks = new Dictionary<string, HashSet<Action<float>>>();
        private readonly string m_downloadFolder;

        public DownloadManager(string downloadFolderPath)
        {
            m_downloadFolder = downloadFolderPath;
            if (!Directory.Exists(m_downloadFolder))
            {
                Directory.CreateDirectory(m_downloadFolder);
            }
        }

        public async Task DownloadFileAsync(string downloadId, Request request, string filePath, Action<float> progressSetter = null, long fileSize = 0)
        {
            if (!m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                group = new DownloadGroup(downloadId, m_downloadFolder);
                if (m_progressCallbacks.TryGetValue(downloadId, out HashSet<Action<float>> callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        group.progresser += callback;
                    }
                    m_progressCallbacks.Remove(downloadId);
                }
                m_downloads[downloadId] = group;
            }
            else if (group.TryGetDownloadHandler(filePath, out DownloadHandler handler))
            {
                if (handler.state.status == DownloadState.Status.InProgress)
                {
                    await handler.Wait();
                }
                else
                {
                    await handler.Start();
                }
                return;
            }

            var existingHandler = m_downloads.Values.SelectMany(g => g.m_handlers).FirstOrDefault(h => h.destinationFilepath == filePath);
            if (existingHandler?.state.status == DownloadState.Status.InProgress)
            {
                existingHandler.progresser += group.SetProgress;
                group.m_handlers.Add(existingHandler);
                await existingHandler.Wait();
            }
            else
            {
                await group.Append(request, filePath, progressSetter, fileSize);
            }
        }

        public async Task StopDownload(string downloadId)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                await group.Stop();
            }
        }

        public async Task StopDownload(string downloadId, string filepath)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group)
                && group.TryGetDownloadHandler(filepath, out DownloadHandler handler))
            {
                await handler.Stop();
            }
        }

        public async Task FinishDownloadAsync(string downloadId, Action<float> progressSetter = null)
        {
            if (!m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                return;
            }

            if (progressSetter != null)
            {
                group.progresser -= progressSetter;
                group.progresser += progressSetter;
                group.RefreshProgress();
            }

            await group.Wait();
        }

        public DownloadState GetDownloadState(string downloadId, string filepath)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group)
                && group.TryGetDownloadHandler(filepath, out DownloadHandler handler))
            {
                return handler.state;
            }
            return DownloadState.Invalid;
        }

        public void RegisterForProgress(string downloadId, Action<float> progressSetter)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                group.progresser -= progressSetter;
                group.progresser += progressSetter;
                group.RefreshProgress();
            }
            else
            {
                if (!m_progressCallbacks.TryGetValue(downloadId, out HashSet<Action<float>> callbacks))
                {
                    callbacks = new HashSet<Action<float>>();
                    m_progressCallbacks[downloadId] = callbacks;
                }
                callbacks.Add(progressSetter);
            }
        }

        public void UnregisterFromProgress(string downloadId, Action<float> progressSetter)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                group.progresser -= progressSetter;
            }
            else if (m_progressCallbacks.TryGetValue(downloadId, out HashSet<Action<float>> callbacks))
            {
                callbacks.Remove(progressSetter);
            }
        }

        public void ClearTemporaryFiles()
        {
            foreach (var group in m_downloads.Values.ToArray())
            {
                group.ClearTemporaryFiles();
                if (group.m_handlers.Count == 0)
                {
                    m_downloads.Remove(group.id);
                }
            }
            if (m_downloads.Count == 0 && Directory.Exists(m_downloadFolder))
            {
                Directory.Delete(m_downloadFolder);
                Directory.CreateDirectory(m_downloadFolder);
            }
        }

        public IEnumerable<string> GetAllDownloads()
        {
            return m_downloads.Keys;
        }

        public IEnumerable<DownloadState> GetDownloadStates(string downloadId)
        {
            if (m_downloads.TryGetValue(downloadId, out DownloadGroup group))
            {
                return group.states;
            }
            return new DownloadState[0];
        }

        public bool IsDownloadInProgress(string downloadId)
        {
            return GetDownloadStates(downloadId).Any(p => p.status == DownloadState.Status.InProgress);
        }

        private class DownloadGroup : IDisposable
        {
            internal List<DownloadHandler> m_handlers;
            
            public string id;
            public string folderPath;
            public float progress;
            public IEnumerable<DownloadState> states => m_handlers?.Select(h => h.state);

            public DownloadState averageState
            {
                get
                {
                    if (m_handlers?.Count > 0)
                    {
                        DownloadState state = m_handlers[0].state;
                        for (int i = 1; i < m_handlers.Count; i++)
                        {
                            var handlerState = m_handlers[i].state;
                            if (handlerState.status != DownloadState.Status.Done
                                && handlerState.status != DownloadState.Status.InProgress
                                && handlerState.status != DownloadState.Status.NotStarted)
                            {
                                state.status = handlerState.status;
                                state.error = handlerState.error;
                                break;
                            }

                            if (handlerState.status == DownloadState.Status.InProgress)
                            {
                                state.status = DownloadState.Status.InProgress;
                            }
                            else if (state.status == DownloadState.Status.NotStarted && handlerState.status == DownloadState.Status.Done)
                            {
                                state.status = DownloadState.Status.Done;
                            }
                            state.destinationPath = handlerState.destinationPath;
                            state.progress += handlerState.progress;
                        }

                        state.progress /= m_handlers.Count;

                        return state;
                    }
                    else
                    {
                        return DownloadState.Invalid;
                    }
                }
            }

            public Action<float> progresser;

            internal void SetProgress(float p)
            {
                progress = 0;
                if (m_handlers?.Count > 0)
                {
                    long totalFileSize = 0;
                    long avgFileSize = 0;
                    int validFileSizes = 0;

                    for (int i = 0; i < m_handlers.Count; i++)
                    {
                        if (m_handlers[i].fileSize > 0)
                        {
                            avgFileSize += m_handlers[i].fileSize;
                            validFileSizes++;
                        }
                    }

                    if (validFileSizes > 0) { avgFileSize /= validFileSizes; }
                    if (avgFileSize <= 0) { avgFileSize = 1; }

                    for (int i = 0; i < m_handlers.Count; i++)
                    {
                        if (m_handlers[i].fileSize == 0)
                        {
                            progress += m_handlers[i].state.progress * avgFileSize;
                            totalFileSize += avgFileSize;
                        }
                        else
                        {
                            progress += m_handlers[i].state.progress * m_handlers[i].fileSize;
                            totalFileSize += m_handlers[i].fileSize;
                        }
                    }

                    if (totalFileSize > 0)
                    {
                        progress /= totalFileSize;
                    }
                }

                //UnityEngine.Debug.Log($"Downlaod[{id}]: {progress}");
                progresser?.Invoke(progress);
            }

            public DownloadGroup(string id, string folder)
            {
                m_handlers = new List<DownloadHandler>();
                this.id = Path.GetFileNameWithoutExtension(id);
                folderPath = Path.Combine(folder, this.id);
            }

            public async Task Append(Request request, string filepath, Action<float> progressSetter, long fileSize)
            {
                if (progressSetter != null)
                {
                    progresser -= progressSetter;
                    progresser += progressSetter;
                }
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                if (TryGetDownloadHandler(filepath, out DownloadHandler handler))
                {
                    await handler.Wait();
                }
                else
                {
                    handler = new DownloadHandler(id, folderPath, request, filepath, SetProgress, fileSize);
                    m_handlers.Add(handler);
                    await handler.Start();
                }
            }

            public async Task Stop()
            {
                List<Task> stopTasks = new List<Task>();
                for (int i = 0; i < m_handlers.Count; i++)
                {
                    stopTasks.Add(m_handlers[i].Stop());
                }
                await Task.WhenAll(stopTasks);
            }

            public async Task Wait()
            {
                List<Task> waitTasks = new List<Task>();
                for (int i = 0; i < m_handlers.Count; i++)
                {
                    waitTasks.Add(m_handlers[i].Wait());
                }
                await Task.WhenAll(waitTasks);
            }

            public bool TryGetDownloadHandler(string filepath, out DownloadHandler handler)
            {
                for (int i = 0; i < m_handlers.Count; i++)
                {
                    if (m_handlers[i].destinationFilepath == filepath)
                    {
                        handler = m_handlers[i];
                        return true;
                    }
                }
                handler = null;
                return false;
            }

            public void Dispose()
            {
                for (int i = 0; i < m_handlers.Count; i++)
                {
                    m_handlers[i]?.Dispose();
                }
            }

            internal void RefreshProgress()
            {
                for (int i = 0; i < m_handlers.Count; i++)
                {
                    m_handlers[i].RefreshProgress();
                }
            }

            internal void ClearTemporaryFiles()
            {
                foreach (var handler in m_handlers.ToArray())
                {
                    if (handler.state.status != DownloadState.Status.InProgress)
                    {
                        handler.DeleteFile();
                        m_handlers.Remove(handler);
                    }
                }

                if (m_handlers.Count == 0 && Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath);
                }
            }
        }

        private class DownloadHandler : IDisposable
        {
            public readonly string id;
            public DownloadState state;
            public readonly string destinationFilepath;
            public string activeFilepath;
            public Action<float> progresser;
            public CancellationTokenSource cancellationSource;
            public Request request;
            public long fileSize;

            public DownloadHandler(string id, string directoryPath, Request request, string filePath, Action<float> progressSetter, long fileSize)
            {
                this.id = id;
                this.request = request;
                this.fileSize = fileSize;
                destinationFilepath = filePath;
                activeFilepath = Path.Combine(directoryPath, Path.GetFileName(filePath));
                progresser = v => state.progress = v;
                if (progressSetter != null)
                {
                    progresser += progressSetter;
                }
                cancellationSource = new CancellationTokenSource();
                state.status = DownloadState.Status.NotStarted;
                state.destinationPath = destinationFilepath;
            }

            public void Dispose()
            {
                cancellationSource?.Dispose();
            }

            public async Task Start()
            {
                await Stop();
                state.progress = 0;
                state.status = DownloadState.Status.InProgress;
                cancellationSource = new CancellationTokenSource();
                using (var downloadHandler = new DownloadHandlerFile(activeFilepath))
                {
                    try
                    {
                        var response = await new WeavrWebRequest().GET(request, downloadHandler, cancellationSource.Token, progresser);
                        if (response.WasCancelled)
                        {
                            state.status = DownloadState.Status.Cancelled;
                        }
                        else if (response.HasError)
                        {
                            state.status = DownloadState.Status.Faulted;
                            state.error = response.FullError;
                            response.Validate($"Download: id = {id} of file {destinationFilepath} failed");
                        }
                        else
                        {
                            // Everything is ok
                            if (!Directory.Exists(Path.GetDirectoryName(destinationFilepath)))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilepath));
                            }
                            File.Move(activeFilepath, destinationFilepath);
                            state.status = DownloadState.Status.Done;
                            state.progress = 1;
                        }
                    }
                    catch
                    {
                        state.status = DownloadState.Status.Faulted;
                        throw;
                    }
                    finally
                    {
                        DeleteFile();
                        cancellationSource?.Dispose();
                        cancellationSource = null;
                    }
                }
            }

            public async Task Stop()
            {
                if (cancellationSource != null && state.status == DownloadState.Status.InProgress)
                {
                    cancellationSource.Cancel();
                    while (state.status == DownloadState.Status.InProgress)
                    {
                        await Task.Yield();
                        state.status = DownloadState.Status.Cancelled;
                    }
                    state.status = DownloadState.Status.Cancelled;
                    cancellationSource.Dispose();
                }
                DeleteFile();
            }

            internal void DeleteFile()
            {
                if (File.Exists(activeFilepath))
                {
                    File.Delete(activeFilepath);
                }
            }

            public async Task Wait()
            {
                while (state.status == DownloadState.Status.InProgress)
                {
                    await Task.Yield();
                }
            }

            internal void RefreshProgress()
            {
                progresser?.Invoke(state.progress);
            }
        }
    }
}
