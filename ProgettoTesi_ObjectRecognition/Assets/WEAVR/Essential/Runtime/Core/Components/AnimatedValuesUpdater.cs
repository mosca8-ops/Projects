using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{

    [AddComponentMenu("")]
    public class AnimatedValuesUpdater : MonoBehaviour
    {
        #region [  STATIC PART  ]
        private static AnimatedValuesUpdater s_instance;

        public static AnimatedValuesUpdater Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<AnimatedValuesUpdater>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("AnimationValuesUpdater");
                        s_instance = go.AddComponent<AnimatedValuesUpdater>();
                    }
                    s_instance.Awake();
                }
                return s_instance;
            }
        }

        #endregion


        private Dictionary<AnimatedValue, Action<float>> m_updateActions;
        private List<AnimatedValue> m_valuesToRemove;

        private void Awake()
        {
            if(s_instance != null && s_instance != this)
            {
                Destroy(this);
                return;
            }

            s_instance = this;

            m_updateActions = new Dictionary<AnimatedValue, Action<float>>();
            m_valuesToRemove = new List<AnimatedValue>();
        }

        public void RegisterUpdateCallback(AnimatedValue client, Action<float> updateCallback)
        {
            m_updateActions[client] = updateCallback;
        }

        public void UnregisterUpdateCallback(AnimatedValue client)
        {
            m_valuesToRemove.Add(client);
        }

        private void Update()
        {
            foreach(var pair in m_updateActions)
            {
                pair.Value(Time.deltaTime);
                if (pair.Key.HasFinished)
                {
                    m_valuesToRemove.Add(pair.Key);
                }
            }

            if(m_valuesToRemove.Count > 0)
            {
                foreach(var value in m_valuesToRemove)
                {
                    m_updateActions.Remove(value);
                }
            }
        }
    }
}
