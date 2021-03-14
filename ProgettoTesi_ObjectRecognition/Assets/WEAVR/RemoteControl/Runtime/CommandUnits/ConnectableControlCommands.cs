using System;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/Connectable Controls")]
    public class ConnectableControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string> OnConnect;
        [RemoteEvent]
        public event Action<string> OnDisconnect;

        private (AbstractConnectable connectableA, AbstractConnectable connectableB) m_lastConnectables;
        public (AbstractConnectable connectableA, AbstractConnectable connectableB) LastConnectables
        {
            get => m_lastConnectables;
            set
            {
                if (m_lastConnectables != value)
                {
                    if (m_lastConnectables.connectableA && m_lastConnectables.connectableB)
                    {
                        m_lastConnectables.connectableA.OnConnected.RemoveListener(InvokeConnectableAConnect);
                        m_lastConnectables.connectableB.OnConnected.RemoveListener(InvokeConnectableBConnect);
                    }
                    m_lastConnectables = value;
                    if (m_lastConnectables.connectableA && m_lastConnectables.connectableB)
                    {
                        m_lastConnectables.connectableA.OnConnected.RemoveListener(InvokeConnectableAConnect);
                        m_lastConnectables.connectableA.OnConnected.AddListener(InvokeConnectableAConnect);
                        m_lastConnectables.connectableB.OnConnected.RemoveListener(InvokeConnectableBConnect);
                        m_lastConnectables.connectableB.OnConnected.AddListener(InvokeConnectableBConnect);
                    }
                }
            }
        }

        private AbstractConnectable m_lastConnectable;
        public AbstractConnectable LastConnectable
        {
            get => m_lastConnectable;
            set
            {
                if (m_lastConnectable != value)
                {
                    if (m_lastConnectables.connectableA && m_lastConnectables.connectableB)
                    {
                        m_lastConnectables.connectableA.OnDisconnected.RemoveListener(InvokeConnectableDisconnect);
                    }
                    m_lastConnectable = value;
                    if (m_lastConnectables.connectableA && m_lastConnectables.connectableB)
                    {
                        m_lastConnectables.connectableA.OnDisconnected.RemoveListener(InvokeConnectableDisconnect);
                        m_lastConnectables.connectableA.OnDisconnected.AddListener(InvokeConnectableDisconnect);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void Connect(Guid guidConnectableA, Guid guidConnectableB)
        {
            var connectableA = Query.GetComponentByGuid<AbstractConnectable>(guidConnectableA);
            var connectableB = Query.GetComponentByGuid<AbstractConnectable>(guidConnectableB);
            if (connectableA && connectableB)
            {
                LastConnectables = (connectableA, connectableB);
                connectableA.ConnectedObject = connectableB;
            }
        }

        [RemotelyCalled]
        public void Connect(string pathConnectableA, string pathConnectableB)
        {
            var connectableA = Query.Find<AbstractConnectable>(QuerySearchType.Scene, pathConnectableA).First();
            var connectableB = Query.Find<AbstractConnectable>(QuerySearchType.Scene, pathConnectableB).First();
            if (connectableA && connectableB)
            {
                LastConnectables = (connectableA, connectableB);
                connectableA.ConnectedObject = connectableB;
            }
        }

        [RemotelyCalled]
        public void Disconnect(Guid guid)
        {
            var connectable = Query.GetComponentByGuid<AbstractConnectable>(guid);
            if (connectable)
            {
                LastConnectable = connectable;
                connectable.Disconnect();
            }
        }

        [RemotelyCalled]
        public void Disconnect(string path)
        {
            var connectable = Query.Find<AbstractConnectable>(QuerySearchType.Scene, path).First();
            if (connectable)
            {
                LastConnectable = connectable;
                connectable.Disconnect();
            }
        }

        public void InvokeConnectableConnect(string path)
        {
            OnConnect?.Invoke(path);
        }
        public void InvokeConnectableDisconnect(string path)
        {
            OnDisconnect?.Invoke(path);
        }

        public void InvokeConnectableConnect(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeConnectableConnect(path);
        }

        public void InvokeConnectableDisconnect(GameObject gameObject)
        {
            if (gameObject == null) return;

            var path = SceneTools.GetGameObjectPath(gameObject);
            InvokeConnectableDisconnect(path);
        }

        private void InvokeConnectableAConnect()
        {
            InvokeConnectableConnect(LastConnectables.connectableA.GetHierarchyPath());
        }

        private void InvokeConnectableBConnect()
        {
            InvokeConnectableConnect(LastConnectables.connectableB.GetHierarchyPath());
        }

        private void InvokeConnectableDisconnect()
        {
            InvokeConnectableDisconnect(LastConnectable.GetHierarchyPath());
        }
    }
}
