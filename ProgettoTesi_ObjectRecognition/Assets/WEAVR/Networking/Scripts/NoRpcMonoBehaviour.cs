using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;

namespace TXT.WEAVR.Networking
{
    public abstract class NoRpcMonoBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether to send the calls to new players or not")]
        protected bool m_sendToNewPlayers = false;

        private bool m_didAwake;
        protected int m_setFrameCount;

        protected virtual bool IsImmediate => false;

        protected virtual bool CanRPC()
        {
            return m_setFrameCount < Time.frameCount;
        }

        protected virtual void Awake()
        {
            m_didAwake = true;
        }

        public virtual void Register()
        {
        }


        protected S GetNetComponent<S>(int viewId) where S : Component
        {
            return default;
        }

        protected bool SelfRPC(string methodName, params object[] parameters)
        {
            return false;
        }

        protected bool SelfBufferedRPC(string methodName, params object[] parameters)
        {
            return false;
        }
    }
}
