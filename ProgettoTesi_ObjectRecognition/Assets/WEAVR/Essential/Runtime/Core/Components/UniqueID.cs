using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.EditorBridge;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [Stateless]
    [DoNotExpose]
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Setup/Unique ID")]
    public class UniqueID : MonoBehaviour
    {
        [SerializeField]
        private string m_uniqueId;
        [SerializeField]
        private long m_timestamp;
        [NonSerialized]
        private long m_runtimeTimestamp = long.MaxValue;

        public string ID
        {
            get { return m_uniqueId; }
            //set {
            //    if (Application.isEditor && m_uniqueId != value) {
            //        m_uniqueId = value;
            //        m_timestamp = DateTime.Now.Ticks;
            //    }
            //}
        }

        public void UpdateTimestamp()
        {
            m_runtimeTimestamp = m_timestamp;
        }

        public long Timestamp => m_runtimeTimestamp;
    }
}
