using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public abstract class ProcedureObject : ScriptableObject
    {
        public static Action<ProcedureObject> s_PreChange;
        public static Action<ProcedureObject> s_MarkDirty;
        public static Action<ProcedureObject, Procedure> s_AddToProcedureAsset;

        [SerializeField]
        private string m_guid;
        [NonSerialized]
        private bool m_muteEvents;

        [DoNotClone]
        [SerializeField]
        [Button(nameof(PropagateProcedure), "Propagate")]
        private Procedure m_procedure;

        public Procedure Procedure {
            get => m_procedure;
            set {
                if (m_procedure != value)
                {
                    if (m_procedure)
                    {
                        m_procedure.Unregister(this);
                    }
                    m_procedure = value;
                    if(m_procedure)
                    {
                        m_procedure.Register(this);
                    }
                    OnAssignedToProcedure(value);
                }
            }
        }

        public string Guid {
            get { return m_guid; }
            set {
                if (m_guid != value)
                {
                    BeginChange();
                    m_guid = value;
                    PropertyChanged(nameof(Guid));
                }
            }
        }

        public bool MuteEvents {
            get { return m_muteEvents; }
            set {
                if (m_muteEvents != value)
                {
                    m_muteEvents = value;
                }
            }
        }

        public event Action<ProcedureObject> OnModified;
        public event Action<ProcedureObject, string> OnPropertyChanged;

        public IExposedPropertyTable ReferenceResolver => Procedure?.ReferencesResolver ?? IDBookkeeper.GetSingleton();

        public void Modified()
        {
            s_MarkDirty?.Invoke(this);
            if (Application.isEditor)
            {
                try
                {
                    name = GetDescription();
                }
                catch { }
            }
            if (m_muteEvents) { return; }
            OnNotifyModified();
        }

        protected virtual void OnNotifyModified()
        {
            OnModified?.Invoke(this);
        }

        protected void BeginChange()
        {
            s_PreChange?.Invoke(this);
        }

        public virtual ProcedureObject Clone()
        {
            var clone = Instantiate(this);

            return clone;
        }

        protected virtual void OnAssignedToProcedure(Procedure value) { }

        public void PropagateProcedure()
        {
            OnAssignedToProcedure(m_procedure);
        }

        protected void InitializeGUID()
        {
            if (string.IsNullOrEmpty(m_guid))
            {
                m_guid = System.Guid.NewGuid().ToString();
            }
        }

        internal void ChangeGUID()
        {
            m_guid = System.Guid.NewGuid().ToString();
        }

        protected void PropertyChanged(string propertyName)
        {
            if (m_muteEvents) { return; }
            OnModified?.Invoke(this);
            OnPropertyChanged?.Invoke(this, propertyName);
        }

        public static T Create<T>(Procedure procedure) where T : ProcedureObject
        {
            T obj = CreateInstance<T>();
            obj.Procedure = procedure;
            s_AddToProcedureAsset?.Invoke(obj, procedure);
            return obj;
        }

        public static ProcedureObject Create(Type type, Procedure procedure)
        {
            ProcedureObject obj = CreateInstance(type) as ProcedureObject;
            obj.Procedure = procedure;
            s_AddToProcedureAsset?.Invoke(obj, procedure);
            return obj;
        }

        public virtual void Refresh()
        {
            OnEnable();
        }

        protected virtual void OnEnable()
        {
            if (Application.isEditor)
            {
                if (string.IsNullOrEmpty(name) || name.Contains("Clone"))
                {
                    name = GetType().Name;
                }
            }

            if (m_procedure)
            {
                m_procedure.Register(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (m_procedure)
            {
                m_procedure.Unregister(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (Application.isEditor)
            {
                //if (m_procedure)
                //{
                //    m_procedure.Unregister(this);
                //}
            }
        }

        public virtual void CollectProcedureObjects(List<ProcedureObject> list)
        {
            list.Add(this);
        }

        public virtual string GetDescription()
        {
            return name;
        }
    }
}
