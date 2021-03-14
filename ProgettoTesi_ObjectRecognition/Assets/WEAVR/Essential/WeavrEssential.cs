using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Animation;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.UI;
using TXT.WEAVR.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR
{
    /// <summary>
    /// Used as module identifier for TXT.WEAVR.Essential
    /// </summary>
    [GlobalModule("Essential", "This is the core module for WEAVR Editor and Player")]
    public class WeavrEssential : WeavrModule
    {
        #region [  INSPECTOR FIELDS  ]

        [Space]
        [SerializeField]
        [HideInInspector]
        protected string m_weavrObjectName = "WEAVR";
        [SerializeField]
        protected GameObject m_weavrObject;
        [SerializeField]
#if !WEAVR_NETWORK
        [HideInInspector]
#endif
        protected bool m_multiplayer = true;
        [HiddenBy(nameof(m_multiplayer))]
#if !WEAVR_NETWORK
        [HideInInspector]
#endif
        [SerializeField]
        protected GameObject m_sceneNetwork;
        [SerializeField]
        protected GameObject m_playerRig;
        
        protected Camera m_weavrCamera;

        private Camera m_vrRigCamera;
        private Camera m_originalCamera;

        //[Header("User Interface")]
        //[SerializeField]
        //protected GameObject m_weavrUI;

        [Header("Components")]

        [SerializeField]
        [Tooltip("Enable object outlines in the world for a better visual feedback")]
        protected bool m_useOutlines = true;
        [SerializeField]
        [Tooltip("Enable object transparency switch control on objects")]
        protected bool m_useTransparencyController = true;
        [SerializeField]
        [Tooltip("Enable billboard popups on various objects in the world")]
        protected bool m_useBillboards = true;
        [SerializeField]
        [Tooltip("The billboard popup sample object")]
        [HiddenBy("m_useBillboards")]
        protected GameObject m_billboardSample = null;
        [SerializeField]
        [Tooltip("This will create an empty gameobject to hold camera positions")]
        protected bool m_createCameraPositions = true;

        [Header("Additional Data")]
        [SerializeField]
        protected GameObject[] m_prefabsToAdd;


        private bool m_applyingDataStarted;
#endregion

#region [  EXPOSED PROPERTIES  ]

        public Camera WEAVRCamera { get { return m_weavrCamera; } }
        public GameObject WEAVRObject { get { return m_weavrObject; } }
        //public GameObject WEAVRUI { get { return m_weavrUI; } }
        private GameObject weavrObjectInstance;
#endregion

        public override IEnumerator ApplyData(Scene scene, Dictionary<System.Type, WeavrModule> otherModules) {
            m_applyingDataStarted = true;
            // WEAVR Object part
            weavrObjectInstance = GetOrCreateSceneObject(scene, m_weavrObject, m_weavrObjectName);
            //GameObject weavrObjectInstance = Weavr.GetWEAVRInScene(scene).gameObject; //(scene, m_weavrObject, m_weavrObjectName);
            //CopyComponents(weavrObjectInScene, weavrObjectInstance);
            Weavr.MergeWith(weavrObjectInstance);

            //ProcedureEngine.AddCoreComponents(weavrObjectInstance);
            AddComponentIfNotPresent<SceneLoader>(weavrObjectInstance);
            AddComponentIfNotPresent<AnimationEngine>(weavrObjectInstance);

            m_applyProgress = 0.05f;
            m_weavrObject = weavrObjectInstance;

            RegisterObjectInScene(m_weavrObject);
            yield return new WaitForEndOfFrame();

            // CAMERA part
            GameObject weavrCamera = null;

            m_weavrCamera = WeavrManager.DefaultCamera;

            if (m_playerRig)
            {
                var playerRig = GetOrCreateSceneObject(scene, m_playerRig);
                if (!m_weavrCamera)
                {
                    m_weavrCamera = playerRig.GetComponentsInChildren<Camera>(true).FirstOrDefault(c => c.enabled);
                }
                if (!playerRig.transform.IsChildOf(weavrObjectInstance.transform))
                {
                    RegisterObjectInScene(playerRig);
                }
            }
            if (m_weavrCamera)
            {
                weavrCamera = !EditorBridge.PrefabUtility.IsPrefabAsset(m_weavrCamera) ? 
                                GetOrCreateSceneObject(scene, m_weavrCamera.gameObject, "WEAVR Camera") : 
                                m_weavrCamera.gameObject;
                var mainCamera = GameObject.Find("Main Camera");
                if (mainCamera && mainCamera != weavrCamera)
                {
                    if (Application.isEditor) { DestroyImmediate(mainCamera); }
                    else { Destroy(mainCamera); }
                }
                AddComponentIfNotPresent<PhysicsRaycaster>(weavrCamera);
                AddComponentIfNotPresent<AudioListener>(weavrCamera);
                AddComponentIfNotPresent<AudioSource>(weavrCamera);
                m_weavrCamera = weavrCamera.GetComponent<Camera>();
                
                if (!WeavrManager.DefaultCamera)
                {
                    WeavrManager.DefaultCamera = m_weavrCamera;
                }
            }

            var eventSystem = GameObject.Find("EventSystem");
            if (eventSystem == null) {
                var es = FindObjectOfType<EventSystem>();
                if (es == null) {
                    eventSystem = GetOrCreateSceneObject(scene, null, "EventSystem");
                }
                else {
                    eventSystem = es.gameObject;
                }
            }
            AddComponentIfNotPresent<EventSystem>(eventSystem);
            AddComponentIfNotPresent<StandaloneInputModule>(eventSystem);

            m_applyProgress = 0.1f;
            yield return new WaitForEndOfFrame();

            // Network
#if WEAVR_NETWORK
            if(m_multiplayer && m_sceneNetwork)
            {
                var sceneNetworkGO = GetOrCreateSceneObject(scene, m_sceneNetwork);
                var sceneNetwork = sceneNetworkGO.GetComponentInChildren<Networking.SceneNetwork>();
                if (sceneNetwork)
                {
                    //sceneNetwork.ScenePlayer = networkPlayer;
                    RegisterObjectInScene(sceneNetworkGO);
                }
            }
#endif
            // UI PART...
            //RegisterObjectInScene(m_weavrUI);
            

            //if (m_useOutlines) { AddComponentIfNotPresent<Outliner>(weavrCamera); }
            if (m_useTransparencyController) { AddComponentIfNotPresent<TransparencyController>(weavrObjectInstance); }
            if (m_useBillboards && m_billboardSample != null) {
                var billboardManager = weavrObjectInstance.GetSingleton<BillboardManager>();
                if (!billboardManager.BillboardDefaultSample)
                {
                    billboardManager.BillboardDefaultSample = GetOrCreateSceneObject(scene, m_billboardSample).GetComponent<Billboard>();
                }
            }
            if (m_createCameraPositions) {
                var camPositions = GameObject.Find("CameraPositions");
                if (camPositions == null) {
                    camPositions = new GameObject("CameraPositions");
                }
            }

            m_applyProgress = 0.2f;
            yield return new WaitForEndOfFrame();
            
            // Add Prefabs to scene
            for (int i = 0; i < m_prefabsToAdd.Length; i++) {
                if(m_prefabsToAdd[i] != null) {
                    //GetOrCreateSceneObject(scene, m_prefabsToAdd[i]);
                    RegisterObjectInScene(m_prefabsToAdd[i]);
                    m_applyProgress = 0.9f + 0.1f * (i / (float)m_prefabsToAdd.Length);
                    yield return new WaitForEndOfFrame();
                }
            }

            // Done
            m_applyProgress = 1;
        }

        protected override void OnSetupFinished()
        {
            base.OnSetupFinished();
            if (weavrObjectInstance)
            {
                EditorBridge.PrefabUtility.UnpackInstance(weavrObjectInstance, EditorBridge.PrefabUnpackMode.Completely);
            }
        }

        public override void InitializeData(Scene scene)
        {
            //Debug.Log($"Path to {GetType().Name}: {Editor.EditorTools.GetModulePath<WeavrEssential>()}");
            var innerCamera = m_weavrObject.GetComponentInChildren<Camera>(true);
            if (innerCamera)
            {
                m_weavrCamera = innerCamera;
                m_weavrObject.gameObject.SetActive(true);
            }

            m_originalCamera = m_weavrCamera;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if(!m_weavrCamera && m_playerRig)
            {
                m_weavrCamera = m_playerRig.GetComponentsInChildren<Camera>(true).FirstOrDefault(c => c.enabled);
            }
        }
    }
}