using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TXT.WEAVR.Common;

namespace TXT.WEAVR
{

    /// <summary>
    /// Used as module identifier for TXT.WEAVR.Maintenance
    /// </summary>
    [GlobalModule("Maintenance", "This module provides various tools and helpers to map and interact with the maintenace environment")]
    public class WeavrMaintenance : WeavrModule
    {
        [SerializeField]
        [Tooltip("The menu to select the command to apply to an object")]
        protected GameObject m_commandsMenu;
        [SerializeField]
        [Tooltip("The menu which sets the value of specified properties")]
        protected GameObject m_valueChangeMenu;

        [Header("Additional Data")]
        [SerializeField]
        protected GameObject[] m_prefabsToAdd;

        public override IEnumerator ApplyData(Scene scene, Dictionary<System.Type, WeavrModule> otherModules) {
            var essentialModule = otherModules[typeof(WeavrEssential)] as WeavrEssential;

            GameObject commandMenu = GetOrCreateSceneObject(scene, m_commandsMenu, "CommandsMenu");
            AddComponentIfNotPresent<LookAtCamera>(commandMenu).cameraToFace = essentialModule.WEAVRCamera;
            AddComponentIfNotPresent<ContextMenu3D>(commandMenu).canvasCamera = essentialModule.WEAVRCamera;
            AddComponentIfNotPresent<LineRenderingPopup3D>(commandMenu);

            m_applyProgress = 0.25f;
            RegisterObjectInScene(m_commandsMenu);
            yield return new WaitForEndOfFrame();

            GameObject valueChangeMenu = GetOrCreateSceneObject(scene, m_valueChangeMenu, "ValueChangerMenu");
            AddComponentIfNotPresent<LookAtCamera>(valueChangeMenu).cameraToFace = essentialModule.WEAVRCamera;
            AddComponentIfNotPresent<ValueChangerMenu>(valueChangeMenu).canvasCamera = essentialModule.WEAVRCamera;
            AddComponentIfNotPresent<LineRenderingPopup3D>(valueChangeMenu);

            m_applyProgress = 0.5f;
            RegisterObjectInScene(m_valueChangeMenu);
            yield return new WaitForEndOfFrame();

            // Add Prefabs to scene
            for (int i = 0; i < m_prefabsToAdd.Length; i++) {
                if (m_prefabsToAdd[i] != null) {
                    //GetOrCreateSceneObject(scene, m_prefabsToAdd[i]);
                    RegisterObjectInScene(m_prefabsToAdd[i]);
                    m_applyProgress = 0.5f + 0.5f * (i / (float)m_prefabsToAdd.Length);
                    yield return new WaitForEndOfFrame();
                }
            }

            // Done
            m_applyProgress = 1.0f;
        }

        public override void InitializeData(Scene scene) {
            
        }
    }
}