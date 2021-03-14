using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class LocalTransition : BaseTransition
    {
        public BaseNode NodeA { get => m_from as BaseNode; set => m_from = value; }

        public BaseNode NodeB { get => m_to as BaseNode; set => m_to = value; }

    }
}