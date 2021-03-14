using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class SplitNode : BaseNode
    {
        [SerializeField]
        private List<BaseTransition> m_transitions;
    }
}
