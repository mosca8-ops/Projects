using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public abstract class BaseCommandUnit : MonoBehaviour, ICommandUnit
    {
        protected WeavrQuery Query { get; private set; }

        protected virtual void Start()
        {
            Query = WeavrRemoteControl.Query;
        }

        protected virtual void OnEnable()
        {
            WeavrRemoteControl.Register(this);
        }

        protected virtual void OnDisable()
        {
            WeavrRemoteControl.Unregister(this);
        }

        public virtual void RegisterCommands(DataInterface dataInterface) { }

        public virtual void UnregisterCommands(DataInterface dataInterface) { }
    }
}
