using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Setup/Billboard Manager")]
    public class BillboardManager : MonoBehaviour, IWeavrSingleton
    {
        public static BillboardManager Instance => Weavr.GetInCurrentScene<BillboardManager>();

        [Serializable]
        public class UnityEventGameObject : UnityEvent<GameObject> { }

        [Serializable]
        public class UnityEventGameObjectString : UnityEvent<GameObject, string> { }

        [SerializeField]
        protected Billboard m_defaultBillboard;

        [Space]
        public UnityEventGameObject OnBillboardShow;
        public UnityEventGameObjectString OnBillboardShowWithText;
        public UnityEventGameObject OnBillboardHide;

        protected Dictionary<GameObject, List<Billboard>> m_activeBillboards;
        protected Dictionary<Billboard, Stack<Billboard>> m_inactiveBillboards;
        protected Dictionary<Billboard, BillboardData> m_billboardData;
        protected Transform m_billboardContainer;

        protected bool m_lastValue;

        public Billboard BillboardDefaultSample
        {
            get { return m_defaultBillboard; }
            set { m_defaultBillboard = value; }
        }

        protected class BillboardData
        {
            public List<Billboard> activeList;
            public Stack<Billboard> inactiveStack;
            public Billboard sample;
        }

        void Awake()
        {
            m_activeBillboards = new Dictionary<GameObject, List<Billboard>>();
            m_inactiveBillboards = new Dictionary<Billboard, Stack<Billboard>>();
            m_billboardData = new Dictionary<Billboard, BillboardData>();

            m_billboardContainer = (GameObject.Find("Billboards") ?? new GameObject("Billboards")).transform;
            if (m_billboardContainer.parent != Weavr.GetWEAVRInCurrentScene())
            {
                m_billboardContainer.SetParent(Weavr.GetWEAVRInCurrentScene(), false);
            }

            if (m_defaultBillboard)
            {
                m_inactiveBillboards[m_defaultBillboard] = new Stack<Billboard>();
            }
        }

        public bool HasBillboardWithText(GameObject go, string text)
        {
            if (m_activeBillboards.TryGetValue(go, out List<Billboard> billboards))
            {
                foreach (var billboard in billboards)
                {
                    if (billboard.Text == text)
                        return true;
                }
            }

            return false;
        }

        public bool HasBillboards(GameObject go)
        {
            if (m_activeBillboards.TryGetValue(go, out List<Billboard> billboards))
                return true;

            return false;
        }

        public bool HasBillboards(GameObject go, out List<Billboard> goBillboards)
        {
            if (m_activeBillboards.TryGetValue(go, out goBillboards))
                return true;
            return false;
        }

        public void HideBillboardOn(GameObject go)
        {
            if (go && m_activeBillboards.TryGetValue(go, out List<Billboard> billboards))
            {
                m_activeBillboards.Remove(go);
                foreach (var billboard in billboards)
                {
                    billboard.Hide();
                }
                OnBillboardHide.Invoke(go);
            }
        }

        public void HideBillboardOn(GameObject go, Billboard sample)
        {
            if (go && m_activeBillboards.TryGetValue(go, out List<Billboard> billboards))
            {
                var billboardsToHide = new List<Billboard>();
                foreach (var billboard in billboards)
                {
                    if (m_billboardData.TryGetValue(billboard, out BillboardData data) && data.sample == sample)
                    {
                        billboardsToHide.Add(billboard);
                    }
                }
                foreach (var billboard in billboardsToHide)
                {
                    billboard.Hide();
                    billboards.Remove(billboard);
                }
                if (billboards.Count == 0)
                {
                    m_activeBillboards.Remove(go);
                }
                if (billboardsToHide.Count > 0)
                {
                    OnBillboardHide.Invoke(go);
                }          
            }
        }

        public void HideLastBillboardOn(GameObject go)
        {
            if (go && m_activeBillboards.TryGetValue(go, out List<Billboard> billboards) && billboards.Count > 0)
            {
                var lastOne = billboards[billboards.Count - 1];
                if (lastOne)
                {
                    lastOne.Hide();
                }
                billboards.RemoveAt(billboards.Count - 1);
                if (billboards.Count == 0)
                {
                    m_activeBillboards.Remove(go);
                }
                OnBillboardHide.Invoke(go);
            }
        }

        public List<Billboard> GetBillboardsOn(GameObject go)
        {
            if (go && m_activeBillboards.TryGetValue(go, out List<Billboard> billboards))
            {
                return new List<Billboard>(billboards);
            }
            return new List<Billboard>();
        }

        public void ShowBillboardOn(GameObject go, string text)
        {
            var billboard = GetBillboard(m_defaultBillboard);
            billboard.Text = text;
            if (!m_billboardData.TryGetValue(billboard, out BillboardData data) || data.activeList == null || !data.activeList.Contains(billboard))
            {
                billboard.ShowOn(go);
            }
            OnBillboardShow.Invoke(go);
            OnBillboardShowWithText.Invoke(go, text);
        }

        public void OnEnable()
        {
            ShowBillboards();
            m_lastValue = WeavrManager.ShowBillboards;
        }

        public void ShowBillboards()
        {
            foreach (var keyValuePair in m_activeBillboards)
            {
                //keyValuePair.Value.Show(keyValuePair.Key, keyValuePair.Value.Text);
                foreach (var billboard in keyValuePair.Value)
                {
                    if (billboard)
                    {
                        billboard.gameObject.SetActive(true);
                    }
                }
            }
        }

        public void OnDisable()
        {
            HideForOnDisable();
        }

        public void OnDestroy()
        {
            m_activeBillboards.Clear();
        }

        public void ClearBillboards()
        {
            var dictionary = new Dictionary<GameObject, List<Billboard>>(m_activeBillboards);
            foreach (var billboards in dictionary.Values)
            {
                foreach (var billboard in billboards)
                {
                    if (billboard)
                    {
                        billboard.Hide();
                    }
                }
            }
        }

        public void HideBillboards()
        {
            foreach (var billboards in m_activeBillboards.Values)
            {
                foreach (var billboard in billboards)
                {
                    if (billboard != null)
                    {
                        billboard.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HideForOnDisable()
        {
            foreach (var billboards in m_activeBillboards.Values.ToArray())
            {
                foreach (var billboard in billboards.ToArray())
                {
                    if (billboard != null)
                    {
                        billboard.gameObject.SetActive(false);
                    }
                }
            }
        }

        public Billboard GetBillboard(Billboard sample, bool startActive = true)
        {
            if (!sample)
            {
                sample = m_defaultBillboard;
            }
            if (m_inactiveBillboards.TryGetValue(sample, out Stack<Billboard> billboards) && billboards.Count > 0)
            {
                var b = billboards.Pop();
                b.gameObject.SetActive(true);
                return b;
            }
            if (billboards == null)
            {
                billboards = new Stack<Billboard>();
                m_inactiveBillboards[sample] = billboards;
            }
            bool wasActive = sample.gameObject.activeInHierarchy;
            sample.gameObject.SetActive(startActive);
            GameObject newGO = Instantiate(sample.gameObject);
            sample.gameObject.SetActive(wasActive);
            newGO.transform.SetParent(m_billboardContainer, false);
            var newBillboard = newGO.GetComponent<Billboard>();
            newBillboard.ChangedVisibility -= Billboard_ChangedVisibility;
            newBillboard.ChangedVisibility += Billboard_ChangedVisibility;
            m_billboardData[newBillboard] = new BillboardData() { sample = sample, inactiveStack = billboards };
            return newBillboard;
        }

        private void Billboard_ChangedVisibility(Billboard billboard, bool visible)
        {
            if (visible)
            {
                RegisterBillboard(billboard);
            }
            else
            {
                ReclaimBillboard(billboard);
            }
        }

        public void RegisterBillboard(Billboard billboard)
        {
            if (billboard.Target && m_billboardData.TryGetValue(billboard, out BillboardData data))
            {
                if (!m_activeBillboards.TryGetValue(billboard.Target.gameObject, out List<Billboard> activeSet))
                {
                    activeSet = new List<Billboard>();
                    m_activeBillboards[billboard.Target.gameObject] = activeSet;
                }
                data.activeList = activeSet;
                if (!activeSet.Contains(billboard))
                {
                    activeSet.Add(billboard);
                }
                OnBillboardShow.Invoke(billboard.Target.gameObject);
            }
        }

        protected void ReclaimBillboard(Billboard billboard)
        {
            if (m_billboardData.TryGetValue(billboard, out BillboardData data))
            {
                data.activeList?.Remove(billboard);
                if (data.sample)
                {
                    billboard.CopyValuesFrom(data.sample);
                }
                billboard.gameObject.SetActive(false);
                data.inactiveStack.Push(billboard);
            }
        }

        private void Update()
        {
            if (m_lastValue != WeavrManager.ShowBillboards)
            {
                m_lastValue = WeavrManager.ShowBillboards;
                if (m_lastValue)
                {
                    ShowBillboards();
                }
                else
                {
                    HideBillboards();
                }
            }
        }
    }
}
