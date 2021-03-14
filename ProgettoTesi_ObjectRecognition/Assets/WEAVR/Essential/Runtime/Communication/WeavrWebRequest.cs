using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace TXT.WEAVR.Communication
{
	/// <summary>
	/// This class is a wrapper for UnityWebRequest to be used in async/await mode
	/// and is working only with WEAVR's <see cref="Request"/> and <see cref="Response"/> objects
	/// </summary>
	public class WeavrWebRequest
	{
        public static IList<IInterceptor> RequestInterceptors { get; } = new List<IInterceptor>();
        public static IList<IInterceptor> ResponseInterceptors { get; } = new List<IInterceptor>();

		public int timeout { get; set; } = -1;

		public WeavrWebRequest(int timeout = -1)
        {
            this.timeout = timeout;
        }

		public async Task<Response> GET(Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbGET, request, progressUpdate, token);
        }

        public async Task<Response> GET(Request request, DownloadHandler downloadHandler, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbGET, request, progressUpdate, token, downloadHandler);
        }

        public async Task<Response> GET(Request request, Action<float> progressUpdate = null)
		{
			return await SendRequest(UnityWebRequest.kHttpVerbGET, request, progressUpdate, CancellationToken.None);
		}

        public async Task<Response> POST(Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbPOST, request, progressUpdate, token);
        }

        public async Task<Response> POST(Request request, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbPOST, request, progressUpdate, CancellationToken.None);
        }

        public async Task<Response> PUT(Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbPUT, request, progressUpdate, token);
        }

        public async Task<Response> PUT(Request request, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbPUT, request, progressUpdate, CancellationToken.None);
        }

        public async Task<Response> DELETE(Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbDELETE, request, progressUpdate, token);
        }

        public async Task<Response> DELETE(Request request, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbDELETE, request, progressUpdate, CancellationToken.None);
        }

        public async Task<Response> CREATE(Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbCREATE, request, progressUpdate, token);
        }

        public async Task<Response> CREATE(Request request, Action<float> progressUpdate = null)
        {
            return await SendRequest(UnityWebRequest.kHttpVerbCREATE, request, progressUpdate, CancellationToken.None);
        }

        public async Task<Response> Send(string method, Request request, CancellationToken token, Action<float> progressUpdate = null)
        {
            return await SendRequest(method, request, progressUpdate, token);
        }

        public async Task<Response> Send(string method, Request request, Action<float> progressUpdate = null)
        {
            return await SendRequest(method, request, progressUpdate, CancellationToken.None);
        }

        private async Task<Response> SendRequest(string method, Request request, Action<float> progressUpdate, CancellationToken cancellationToken, DownloadHandler handler = null)
        {
            using (var www = new UnityWebRequest())
            {

                if (timeout > 0)
                {
                    www.timeout = timeout;
                }

                for (int i = 0; i < RequestInterceptors.Count; i++)
                {
                    var interceptor = RequestInterceptors[i];
                    await interceptor.Intercept(www, request, cancellationToken);
                }

                www.SetRequestParameters(method, request);

                if (cancellationToken.IsCancellationRequested)
                {
                    return Response.Cancelled;
                }

                if(handler != null)
                {
                    www.downloadHandler = handler;
                }

                var operation = www.SendWebRequest();
                while (!operation.isDone && !cancellationToken.IsCancellationRequested)
                {
                    progressUpdate?.Invoke(operation.progress);
                    await Task.Yield();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return Response.Cancelled;
                }

                for (int i = 0; i < ResponseInterceptors.Count; i++)
                {
                    var interceptor = ResponseInterceptors[i];
                    var outcome = await interceptor.Intercept(www, request, cancellationToken);
                    if(outcome == InterceptOutcome.RequiresResend)
                    {
                        return await SendRequest(method, request, progressUpdate, cancellationToken, handler);
                    }
                }

                return new Response(www, request) { WasCancelled = cancellationToken.IsCancellationRequested };
            }
        }
    }

    public static class WeavrWebRequestExtensions
    {
        public static void AddUnique<T>(this IList<IInterceptor> list, T interceptor) where T : IInterceptor
        {
            RemoveAll<T>(list);

            list.Add(interceptor);
        }

        public static void RemoveAll<T>(this IList<IInterceptor> list) where T : IInterceptor
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T)
                {
                    list.RemoveAt(i--);
                }
            }
        }

        public static T Get<T>(this IList<IInterceptor> list, bool createIfNotPresent = true) where T : IInterceptor, new()
        {
            for (int i = 0; i < list.Count; i++)
            {
                if(list[i] is T ti)
                {
                    return ti;
                }
            }

            if (createIfNotPresent)
            {
                T interceptor = new T();
                list.Add(interceptor);
                return interceptor;
            }
            return default;
        }

        public static void SetRequestParameters(this UnityWebRequest www, string method, Request request)
        {

            // Set the method
            www.method = method;

            // Set the url 
            www.url = AttachQueryToUrl(request.Url, request.QueryUrl);

            // Set the headers
            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    if (string.IsNullOrEmpty(header.Key)) { continue; }
                    www.SetRequestHeader(header.Key, header.Value);
                }
            }

            // Set the body with uploadHandler
            if (request.Body != null)
            {

                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request.Body)));

            }

            // Set the downloadHandler
            if (request.DownloadHandler != null)
            {
                www.downloadHandler = request.DownloadHandler;
            }
            else
            {
                www.downloadHandler = new DownloadHandlerBuffer();
            }
        }

        private static string AttachQueryToUrl(string url, Dictionary<string, string> queryUrl)
        {
            if (queryUrl == null || queryUrl.Count == 0)
            {
                return url;
            }

            StringBuilder builder = new StringBuilder(url);
            builder.Append("?");
            foreach (var pair in queryUrl)
            {
                if (string.IsNullOrEmpty(pair.Key)) { continue; }
                //builder.AppendFormat("{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value));
                builder.AppendFormat("{0}={1}&", pair.Key, pair.Value);
            }

            return builder.ToString().Substring(0, builder.ToString().Length - 1);
        }
    }
}