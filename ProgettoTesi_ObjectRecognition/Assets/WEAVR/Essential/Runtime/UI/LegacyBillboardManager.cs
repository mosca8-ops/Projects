using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("")]
    [Obsolete("Use new Billboard Manager instead")]
    public class LegacyBillboardManager : MonoBehaviour
    {
        [Serializable]
        public class UnityEventGameObject : UnityEvent<GameObject> { }

        [Serializable]
        public class UnityEventGameObjectString : UnityEvent<GameObject, string> { }

        #region [  STATIC PART  ]
        private static LegacyBillboardManager s_instance;
        private static float _screenRatio;

        public static LegacyBillboardManager Instance {
            get {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<LegacyBillboardManager>();
                    if (s_instance == null)
                    {
                        // If no object is active, then create a new one
                        GameObject go = new GameObject("BillboardManager");
                        s_instance = go.AddComponent<LegacyBillboardManager>();
                    }
                    s_instance.Awake();
                }
                return s_instance;
            }
        }
        #endregion

        [SerializeField]
        protected BillboardPopup m_billboardSample;

        [Space]
        public UnityEventGameObject OnBillboardShow;
        public UnityEventGameObjectString OnBillboardShowWithText;
        public UnityEventGameObject OnBillboardHide;

        protected Dictionary<GameObject, BillboardPopup> m_activeBillboards;
        protected Stack<BillboardPopup> m_inactiveBillboards;
        protected Transform m_billboardContainer;

        protected bool m_lastValue;

        public BillboardPopup BillboardSample {
            get { return m_billboardSample; }
            set { m_billboardSample = value; }
        }

        // Use this for initialization
        void Awake()
        {
            m_activeBillboards = new Dictionary<GameObject, BillboardPopup>();
            m_inactiveBillboards = new Stack<BillboardPopup>();
            m_billboardContainer = (GameObject.Find("Billboards") ?? new GameObject("Billboards")).transform;
        }

        public void HideBillboardOn(GameObject go)
        {
            _HideBillboardOn(go);
            //this.RPC(nameof(HideBillboardOn), go.GetHierarchyPath());
        }

        // TODO
//#if WEAVR_NETWORK
//        [PunRPC]
//        private void HideBillboardOn(int viewId, string goPath)
//        {
//            var go = Common.GameObjectExtensions.FindInScene(goPath);
//            if (go != null)
//            {
//                this.OnReceivedRPC(viewId)?._HideBillboardOn(Common.GameObjectExtensions.FindInScene(goPath));
//            }
//        }
//#endif

        private void _HideBillboardOn(GameObject go)
        {
            BillboardPopup billboard = null;
            if (m_activeBillboards.TryGetValue(go, out billboard))
            {
                m_activeBillboards.Remove(go);
                billboard.Hide();
                ReclaimBillboard(billboard);

                OnBillboardHide.Invoke(go);
            }
        }

        public void ShowBillboardOn(GameObject go, string text)
        {
            _ShowBillboardOn(go, text);
            //this.RPC(nameof(ShowBillboardOn), go.GetHierarchyPath(), text);
        }

        // TODO
        //#if WEAVR_NETWORK
        //        [PunRPC]
        //        private void ShowBillboardOn(int viewId, string goPath, string text)
        //        {
        //            var go = Common.GameObjectExtensions.FindInScene(goPath);
        //            if (go != null)
        //            {
        //                this.OnReceivedRPC(viewId)?._ShowBillboardOn(Common.GameObjectExtensions.FindInScene(goPath), text);
        //            }
        //        }
        //#endif

        private void _ShowBillboardOn(GameObject go, string text)
        {
            BillboardPopup billboard = null;
            if (m_activeBillboards.TryGetValue(go, out billboard))
            {
                if (billboard.gameObject.activeInHierarchy)
                {
                    billboard.Text = text;
                }
                else
                {
                    billboard.Show(go, text);
                    OnBillboardShow.Invoke(go);
                    OnBillboardShowWithText.Invoke(go, text);
                }
                return;
            }
            billboard = GetBillboard();
            m_activeBillboards[go] = billboard;
            if (enabled)
            {
                billboard.Show(go, text);
            }
            else
            {
                billboard.Text = text;
            }
            OnBillboardShow.Invoke(go);
            OnBillboardShowWithText.Invoke(go, text);
        }

        public bool IsBillboardOn(GameObject go)
        {
            return _ISBillboardOn(go);
            //this.RPC(nameof(IsBillboardOn), go.GetHierarchyPath());
        }

        // TODO
        //#if WEAVR_NETWORK
        //        [PunRPC]
        //        private void IsBillboardOn(int viewId, string goPath)
        //        {
        //            var go = Common.GameObjectExtensions.FindInScene(goPath);
        //            if (go != null)
        //            {
        //                this.OnReceivedRPC(viewId)?._ISBillboardOn(Common.GameObjectExtensions.FindInScene(goPath));
        //            }
        //        }
        //#endif

        private bool _ISBillboardOn(GameObject go)
        {
            return m_activeBillboards.ContainsKey(go);
        }

        public void OnEnable()
        {
            ShowBillboards();
            m_lastValue = WeavrManager.ShowBillboards;
        }

        private void ShowBillboards()
        {
            foreach (var keyValuePair in m_activeBillboards)
            {
                keyValuePair.Value.Show(keyValuePair.Key, keyValuePair.Value.Text);
            }
        }

        public void OnDisable()
        {
            HideBillboards();
        }

        public void OnDestroy()
        {
            m_activeBillboards.Clear();
        }

        public void ClearBillboards()
        {
            var dictionary = new Dictionary<GameObject, BillboardPopup>(m_activeBillboards);
            foreach (var go in dictionary)
            {
                HideBillboardOn(go.Key);
            }
        }

        private void HideBillboards()
        {
            foreach (var billboard in m_activeBillboards.Values)
            {
                if (billboard != null)
                {
                    billboard.Hide();
                }
            }
        }

        protected BillboardPopup GetBillboard()
        {
            if (m_inactiveBillboards.Count > 0 && false)
            {
                return m_inactiveBillboards.Pop();
            }
            bool wasActive = m_billboardSample.gameObject.activeInHierarchy;
            m_billboardSample.gameObject.SetActive(true);
            GameObject newGO = Instantiate(m_billboardSample.gameObject);
            m_billboardSample.gameObject.SetActive(wasActive);
            newGO.transform.SetParent(m_billboardContainer, false);
            return newGO.GetComponent<BillboardPopup>();
        }

        protected void ReclaimBillboard(BillboardPopup billboard)
        {
            m_inactiveBillboards.Push(billboard);
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
