using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public delegate void OnRequestDelegate(IRequest request);
    public delegate void OnDataPointEvent(IDataInterfacePoint dataPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct RequestData
    {
        public readonly byte[] Bytes;

        private string m_string;
        public string String
        {
            get
            {
                if (m_string == null)
                {
                    m_string = Encoding.ASCII.GetString(Bytes);
                }
                return m_string;
            }
        }

        public RequestData(string data)
        {
            Bytes = Encoding.ASCII.GetBytes(data);
            m_string = null;
        }

        public RequestData(byte[] data)
        {
            Bytes = new byte[data.Length];
            Array.Copy(data, Bytes, data.Length);
            m_string = null;
        }

        public static implicit operator RequestData(byte[] bytes)
        {
            return new RequestData(bytes);
        }

        public static implicit operator RequestData(string s)
        {
            return new RequestData(s);
        }

        public static implicit operator byte[](RequestData data)
        {
            return data.Bytes;
        }

        public static implicit operator string(RequestData data)
        {
            return data.String;
        }
    }

    public interface IRequest
    {
        IDataInterfacePoint Source { get; }
        RequestData GetRequest();
        void SendResponse(in RequestData response);
    }

    public interface ICommandChannel
    {
        void BroadcastResponse(in RequestData response);
        event OnRequestDelegate OnNewRequest;
        event OnDataPointEvent DataPointOpened;
        event OnDataPointEvent DataPointClosed;
    }
}
