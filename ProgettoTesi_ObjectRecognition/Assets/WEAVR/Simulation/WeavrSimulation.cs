using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR
{
    /// <summary>
    /// Used as module identifier for TXT.WEAVR.Simulation
    /// </summary>
    [GlobalModule("Simulation", "This module handles the simulation data exchange between WEAVR HUB and WEAVR Editor/Player")]
    public class WeavrSimulation : WeavrModule
    {
        public static readonly string MODULE_NAME = "Simulation";

        [SerializeField]
        protected string m_simFrameworkName = "SimulationFramework";
        [SerializeField]
        [Tooltip("The simulation framework object which handles shared memory and simulation")]
        protected GameObject m_simulationFramework;

        [Header("Additional Data")]
        [SerializeField]
        protected GameObject[] m_prefabsToAdd;

        public override IEnumerator ApplyData(Scene scene, Dictionary<System.Type, WeavrModule> otherModules) {
            GameObject simulationFwk = GetOrCreateSceneObject(scene, m_simulationFramework, m_simFrameworkName);
            AddComponentIfNotPresent<SimulationEvalEngine>(simulationFwk);
            AddComponentIfNotPresent<SharedMemoryManager>(simulationFwk);

            m_applyProgress = 0.5f;
            RegisterObjectInScene(m_simulationFramework);
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