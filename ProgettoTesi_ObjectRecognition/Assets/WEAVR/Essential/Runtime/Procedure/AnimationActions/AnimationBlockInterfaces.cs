using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public interface IAsyncAnimationBlock
    {
        bool IsAsync { get; }
    }
}
