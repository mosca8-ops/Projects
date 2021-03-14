using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Photon.Pun;
using System;

namespace TXT.WEAVR.Networking
{
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Setup/Network Outliner")]
    public class NetworkOutliner : MonoBehaviour, IOnEventCallback
    {
        private enum OperationType { 
            OutlineWithColor, 
            RemoveOutline,
        }

        private BorderOutliner m_outliner;
        private Dictionary<string, GameObject> m_goCache = new Dictionary<string, GameObject>();

        [NonSerialized]
        private int m_setFrameCount = 0;

        private RaiseEventOptions m_raiseEventOptions;
        private SendOptions m_sendOptions;

        private void Awake()
        {
            m_raiseEventOptions = new RaiseEventOptions()
            {
                Receivers = ReceiverGroup.Others,
            };
            m_sendOptions = new SendOptions()
            {
                Reliability = true,
            };
        }

        private void OnEnable()
        {
            if (!m_outliner)
            {
                //m_outliner = this.TryGetSingleton<Outliner>();
            }
            if (m_outliner)
            {
                m_outliner.Outlined -= Outliner_Outlined;
                m_outliner.Outlined += Outliner_Outlined;
                m_outliner.OutlineRemoved -= Outliner_OutlineRemoved;
                m_outliner.OutlineRemoved += Outliner_OutlineRemoved;
            }

            PhotonNetwork.AddCallbackTarget(this);
        }

        protected virtual bool CanSendEvent() => m_setFrameCount < Time.frameCount;

        private void Outliner_OutlineRemoved(GameObject go)
        {
            if (CanSendEvent())
            {
                SendEvent(OperationType.RemoveOutline, go.GetHierarchyPath());
            }
        }

        private void Outliner_Outlined(GameObject go, Color color)
        {
            if (CanSendEvent())
            {
                SendEvent(OperationType.OutlineWithColor, go.GetHierarchyPath(), "#" + ColorUtility.ToHtmlStringRGBA(color));
            }
        }

        private void OnDisable()
        {
            if (m_outliner)
            {
                m_outliner.Outlined -= Outliner_Outlined;
                m_outliner.OutlineRemoved -= Outliner_OutlineRemoved;
            }
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void RemoteOutline(string goPath, string color)
        {
            m_setFrameCount = Time.frameCount;
            if (!m_goCache.TryGetValue(goPath, out GameObject go))
            {
                go = GameObjectExtensions.FindInScene(goPath);
                if (go) { m_goCache[goPath] = go; }
            }
            if (go && ColorUtility.TryParseHtmlString(color, out Color c))
            {
                Outliner.Outline(go, c);
            }
        }

        private void RemoteRemoveOutline(string goPath)
        {
            m_setFrameCount = Time.frameCount;
            if (!m_goCache.TryGetValue(goPath, out GameObject go))
            {
                go = GameObjectExtensions.FindInScene(goPath);
                if (go) { m_goCache[goPath] = go; }
            }
            if (go)
            {
                Outliner.RemoveOutline(go);
            }
        }

        private void SendEvent(OperationType opType, params object[] data)
        {
            // Create custom data
            object[] content = new object[data.Length + 1];
            content[0] = opType;
            for (int i = 0; i < data.Length; i++)
            {
                content[i + 1] = data[i];
            }

            PhotonNetwork.RaiseEvent(NetworkEvents.BorderOutlinerEvent, content, m_raiseEventOptions, m_sendOptions);
        }

        public void OnEvent(EventData photonEvent)
        {
            if(photonEvent.Code != NetworkEvents.BorderOutlinerEvent)
            {
                return;
            }

            var data = photonEvent.CustomData as object[];
            var opType = (OperationType)data[0];
            var goPath = data[1] as string;

            switch (opType)
            {
                case OperationType.OutlineWithColor:
                    var colorString = data[2] as string;
                    RemoteOutline(goPath, colorString);
                    break;
                case OperationType.RemoveOutline:
                    RemoteRemoveOutline(goPath);
                    break;
            }
        }
    }
}
