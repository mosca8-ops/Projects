using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{

    [Serializable]
    public class OnCanvasStatusChange : UnityEvent { };

    public class VirtualTabletHandler : MonoBehaviour
    {
        [SerializeField]
        private GameObject targetCanvas;
        [SerializeField]
        private Camera cameraLook;
        [SerializeField]
        private int minCameraAlignmentX = 20;
        [SerializeField]
        private int minCameraAlignmentZ = 40;
        [SerializeField]
        private int enableTime = 1;
        [SerializeField]
        private float deltaMove = 0.1f;
        [SerializeField]
        private float stayTime = 3;
        [SerializeField]
        private VirtualTabletTarget[] pointerTargets;
        [SerializeField]
        private GameObject menuPlayer;
        [SerializeField]
        private Button menuButton;
        [SerializeField]
        private GameObject pointer;

        private Vector3 prevXRotation;
        private Vector3 XRotation;
        private float t = 0.0f;
        private float st = 0.0f;
        private bool hasEntered = false;
        private bool wasOn = false;
        private Transform oldParent;
        private Scene m_procedureScene;

        [Space]

        public OnCanvasStatusChange OnCanvasEnable;
        public OnCanvasStatusChange OnCanvasDisable;

        private IEnumerator Start()
        {
            yield return null;
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }


        private void OnSceneUnload(Scene procedureScene)
        {
            if (m_procedureScene == procedureScene)
            {
                this.transform.SetParent(oldParent);

            }
        }



        private void OnSceneLoad(Scene procedureScene, LoadSceneMode loadMode)
        {
            var weavr = Weavr.GetWEAVRInScene(procedureScene);
            if (WeavrElement.Find(weavr.gameObject, "Hand_Left"))
            {
                m_procedureScene = procedureScene;
                var handLeft = WeavrElement.Find(weavr.gameObject, "Hand_Left");

                if (pointer == null)
                {
                    pointer = WeavrElement.Find(weavr.gameObject, "PointingSource_Right");
                }

                if (cameraLook == null)
                {
                    cameraLook = WeavrCamera.CurrentCamera;
                }
                oldParent = this.transform.parent;
                this.transform.SetParent(handLeft.transform, false);
            }
                 
        }

        void Update()
        {
            if (!cameraLook || !cameraLook.isActiveAndEnabled)
            {
                cameraLook = WeavrCamera.CurrentCamera;
                if (!cameraLook )
                {
                    return;
                }
            }
           

            if (IsInCameraRange())
            {
                prevXRotation = XRotation;
                XRotation = transform.forward;

                if (!targetCanvas.activeInHierarchy && !wasOn)
                {
                    CheckForActivation();
                }

                else if (targetCanvas.activeInHierarchy)
                {
                    hasEntered = IsAnyPointerEntered();
                    st += Time.deltaTime;

                    CheckStayTime();
                }

            }
            else
            {
                if (targetCanvas.activeInHierarchy)
                {
                    CanvasOff();                   
                }
                wasOn = false;
            }
            
        }

        private bool IsInCameraRange()
        {
            return Vector3.Angle(cameraLook.transform.forward, targetCanvas.transform.forward) < minCameraAlignmentZ 
                && Vector3.Angle(cameraLook.transform.right, targetCanvas.transform.right) < minCameraAlignmentX;
        }

        private void CheckStayTime()
        {
            if (!hasEntered && st > stayTime)
            {
                CanvasOff();
            }
            else if (hasEntered)
            {
                st = 0;
            }
        }

        private bool IsAnyPointerEntered()
        {
            for (int i = 0; i < pointerTargets.Length; i++)
            {
                if (pointerTargets[i].HasEntered)
                {
                    return true;
                }              
            }
            return false;
        }

        private void CheckForActivation()
        {

            if (Mathf.Abs(Vector3.Angle(XRotation, prevXRotation)) < deltaMove)
            {
                if (t > enableTime)
                {
                    CanvasOn();
                }

            t += Time.deltaTime;
            }
            else
            {
                t = 0;
            }
        }

        private void ResetHasEntered()
        {
            for (int i = 0; i < pointerTargets.Length; i++)
            {
                pointerTargets[i].HasEntered = false;
            }
        }

        private void CanvasOn()
        {
            targetCanvas.SetActive(true);
            pointer.SetActive(true);
            wasOn = true;
            t = 0;
            st = 0;
            ResetHasEntered();
            OnCanvasEnable.Invoke();
        }

        private void CanvasOff()
        {
            targetCanvas.SetActive(false);
            t = 0;
            OnCanvasDisable.Invoke();
            if (!menuPlayer.activeInHierarchy)
            {
                pointer.SetActive(false);
            }
        }

        private void OnMenuButtonClicked()
        {
            menuPlayer.SetActive(true);
        }

    }

}
