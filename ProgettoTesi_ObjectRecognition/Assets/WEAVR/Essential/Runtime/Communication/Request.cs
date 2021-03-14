using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace TXT.WEAVR.Communication
{
    [Serializable]
    public partial class Request
    {
        
        public string Url { get; set; }

        public Dictionary<string, string> QueryUrl { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public object Body { get; set; }

        [JsonIgnore]
        public DownloadHandler DownloadHandler { get; set; }

        public string FilePath { get; set; }

        public string TokenType { get; set; } = "Bearer";

        [JsonIgnore]
        public string ContentType
        {
            get => Headers != null && Headers.TryGetValue(RequestHeader.CONTENT_TYPE, out string contentType) ? contentType : string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value)) { Headers?.Remove(RequestHeader.CONTENT_TYPE); }
                else
                {
                    if(Headers == null) { Headers = new Dictionary<string, string>(); }
                    Headers[RequestHeader.CONTENT_TYPE] = value;
                }
            }
        }

        [JsonIgnore]
        public string TokenId
        {
            get => Headers != null && Headers.TryGetValue(RequestHeader.AUTHORIZATION, out string tokenId) ? tokenId.Replace(TokenType, string.Empty).Trim() : string.Empty;
            set
            {
                if (string.IsNullOrEmpty(value)) { Headers?.Remove(RequestHeader.AUTHORIZATION); }
                else
                {
                    if (Headers == null) { Headers = new Dictionary<string, string>(); }
                    Headers[RequestHeader.AUTHORIZATION] = string.Concat(TokenType, " ", value);
                }
            }
        }

        public Request() 
        { 
        }

        public Request(string url)
        {
            Url = url;
        }

        public Request AddQueryValue<T>(string key, T value)
        {
            if(QueryUrl == null)
            {
                QueryUrl = new Dictionary<string, string>();
            }
            QueryUrl[key] = value?.ToString();
            return this;
        }

        public Request AddQueryValue<T>(string key, T[] array)
        {
            if (QueryUrl == null)
            {
                QueryUrl = new Dictionary<string, string>();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach (var item in array)
            {
                sb.Append(item).Append(',');
            }
            if (sb.Length > 1)
            {
                sb.Length--;
            }
            sb.Append(']');
            QueryUrl[key] = sb.ToString();
            return this;
        }

        public Request AddQueryValue<T>(string key, IEnumerable<T> array)
        {
            if (QueryUrl == null)
            {
                QueryUrl = new Dictionary<string, string>();
            }
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach(var item in array)
            {
                sb.Append(item).Append(',');
            }
            if(sb.Length > 1)
            {
                sb.Length--;
            }
            sb.Append(']');
            QueryUrl[key] = sb.ToString();
            return this;
        }
    }
}