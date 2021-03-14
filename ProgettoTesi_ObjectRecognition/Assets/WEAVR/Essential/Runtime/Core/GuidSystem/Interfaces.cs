using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    public interface IWeakGuid
    {
        Guid Guid { get; }
        void UpdateState();
    }

    public interface IGuidProvider
    {
        Guid Guid { get; }
    }
}
