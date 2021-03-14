    using System;
    using System.Collections.Generic;
    using UnityEngine.Networking;

namespace TXT.WEAVR.Communication
{
    [Serializable]
    public class Response
    {
        public static Response Cancelled { get; } = new Response() { WasCancelled = true };

        public Request Request { get; set; }
        public long Code { get; set; }
        public ulong DownloadedBytesCount { get; set; }
        public string Text { get; set; }
        public byte[] Bytes { get; set; }
        public Dictionary<string, string> ResponseHeaders { get; set; }

        public bool IsHttpError { get; set; }
        public bool IsNetworkError { get; set; }
        public string Error { get; set; }
        public bool WasCancelled { get; set; }
        public bool HasError => IsHttpError || IsNetworkError;

        public string FullError => Error + '\n' + Text;
        public string URL { get; set; }
        public long ContentLength => ResponseHeaders.TryGetValue("Content-Length", out string value) 
                                    && long.TryParse(value, out long length) ? 
                                    length : 0;

        public Response(UnityWebRequest www, Request request)
        {
            Request = request;
            Code = www.responseCode;
            DownloadedBytesCount = www.downloadedBytes;
            try 
            { 
                Text = www.downloadHandler.text;
                Bytes = www.downloadHandler.data;
            } catch { }
            ResponseHeaders = www.GetResponseHeaders();
            IsHttpError = www.isHttpError;
            IsNetworkError = www.isNetworkError;
            Error = www.error;
            URL = www.url;
        }

        public bool Validate(string errorPrefix = null)
        {
            if (IsNetworkError)
            {
                WeavrDebug.LogError(errorPrefix ?? ToString(), FullError);
            }
            else if (IsHttpError)
            {
                throw new Exception(FullError);
            }
            return !WasCancelled;
        }

        private Response()
        {

        }
    }
}