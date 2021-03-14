namespace TXT.WEAVR.Simulation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class SimulationVariableAttribute : WeavrAttribute
    {
        public SharedMemoryAccess AccessType { get; private set; }
        public string SharedMemoryId { get; private set; }

        public SimulationVariableAttribute(SharedMemoryAccess access, string sharedMemoryId) {
            AccessType = access;
            SharedMemoryId = sharedMemoryId;
        }
    }
}