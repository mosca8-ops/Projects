using System;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using TXT.WEAVR.InteractionUI;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Common
{
    [RequireComponent(typeof(ObjectOrbit))]
    public class ObjectOrbitForPlayer : MonoBehaviour
    {
        private ObjectOrbit objectOrbit;

        private void OnEnable()
        {
            objectOrbit = gameObject.GetComponent<ObjectOrbit>();
            
        }

        private void Update()
        {
            if (!objectOrbit.TargetObject || !objectOrbit.TargetObject.gameObject.activeInHierarchy)
            {
                objectOrbit.TargetObject = WeavrCamera.CurrentCamera.transform;
            }
        }

    }
}
