using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Multiplayer/Advanced/Network Role")]
    public class NetworkRole : MonoBehaviour
    {

        public enum RoleType { Student, Instructor }

        public RoleType currentRole = RoleType.Student;
        [ShowAsReadOnly]
        public string userId;

        public static NetworkRole Instance { get; private set; }

        // Use this for initialization
        void Awake()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (FindObjectsOfType(GetType()).Length > 1 || (Instance != null && Instance != this))
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void MakeInstructor(bool shouldBeInstructor)
        {
            currentRole = shouldBeInstructor ? RoleType.Instructor : currentRole;
        }
    }
}