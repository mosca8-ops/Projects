using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TXT.WEAVR
{
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Setup/Guid")]
    public class GuidComponent : MonoBehaviour, IGuidProvider, ISerializationCallbackReceiver, IExecuteDisabled, IWeakGuid
    {
        public delegate bool IsAssetOnDiskDelegate(GameObject go);
        public static IsAssetOnDiskDelegate IsAnyTypeOfPrefabAsset;

        [SerializeField]
        private byte[] m_guid;

        public Guid Guid { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAssetOnDisk() => IsAnyTypeOfPrefabAsset?.Invoke(gameObject) == true;

        // Gets called on copy and prefab apply even on disabled objects
        // Can be used when the scene is initialized even for disabled objects
        // Be Careful: This function is called only in Editor, need to find alternative for runtime --> Maybe GuidStorage...
        private void OnValidate()
        {
            if (IsAssetOnDisk())
            {
                Guid = Guid.Empty;
                m_guid = null;
            }
            else
            {
                RegisterToManager();
            }
        }

        public GuidComponent()
        {
            // This is to handle the case when the component is added to a disabled gameobject
            GuidManager.RegisterWeak(this);
        }

        public void InitDisabled() => Awake();

        private void Awake()
        {
            RegisterToManager();
        }

        public static GuidComponent NewGuid(GameObject go)
        {
            var guidComponent = go.AddComponent<GuidComponent>();
            //if (!guidComponent.isActiveAndEnabled)
            //{
            //    guidComponent.RegisterToManager();
            //}
            return guidComponent;
        }

        /// <summary>
        /// Assigns a guid to the specified <see cref="GameObject"/>.
        /// </summary>
        /// <remarks>This method will overwrite the existing <see cref="GuidComponent"/> if there is any!</remarks>
        /// <param name="go">The <see cref="GameObject"/> to assign the guid to</param>
        /// <param name="guid">The <see cref="System.Guid"/> to be assignd</param>
        /// <returns>The newly created or overwritten <see cref="GuidComponent"/></returns>
        public static GuidComponent AssignGuid(GameObject go, Guid guid)
        {
            var guidComponent = go.GetComponent<GuidComponent>();
            if (!guidComponent)
            {
                guidComponent = go.AddComponent<GuidComponent>();
            }

            // Unregister the previous guid
            GuidManager.Unregister(guidComponent.Guid);

            // Assign and register the new one
            guidComponent.Guid = guid;
            guidComponent.m_guid = guid.ToByteArray();
            guidComponent.RegisterToManager();

            return guidComponent;
        }

        public static implicit operator Guid(GuidComponent component) => component ? component.Guid : Guid.Empty;

        private void OnDestroy()
        {
            if(Guid != Guid.Empty)
            {
                GuidManager.Unregister(Guid);
                foreach(var childGuid in GetComponentsInChildren<GuidComponent>(true))
                {
                    if (childGuid && childGuid != this)
                    {
                        childGuid.OnDestroy();
                    }
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_guid?.Length == 16)
            {
                Guid = new Guid(m_guid);
            }
            
            //GuidManager.RegisterWeak(this);
        }

        public void OnBeforeSerialize()
        {
            if (IsAssetOnDisk())
            {
                Guid = Guid.Empty;
                m_guid = null;
            }
            else if(Guid == Guid.Empty)
            {
                m_guid = null;
            }
            else
            {
                m_guid = Guid.ToByteArray();
            }
        }


        private bool RegisterToManager()
        {
            if (!this)
            {
                return false;
            }
            if (m_guid?.Length != 16)
            {
                if (IsAssetOnDisk())
                {
                    Guid = Guid.Empty;
                    m_guid = null;
                    return false;
                }

                Guid = Guid.NewGuid();
                m_guid = Guid.ToByteArray();
            }
            else if(Guid == Guid.Empty)
            {
                Guid = new Guid(m_guid);
            }
            
            if(Guid != Guid.Empty && !GuidManager.Register(this))
            {
                Guid = Guid.NewGuid();
                m_guid = Guid.ToByteArray();
                RegisterToManager();
            }
            return true;
        }

        public void UpdateState()
        {
            if (!RegisterToManager())
            {
                GuidManager.Unregister(this);
            }
        }
    }
}
