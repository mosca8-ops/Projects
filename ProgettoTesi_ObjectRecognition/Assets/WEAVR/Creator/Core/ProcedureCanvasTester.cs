using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ProcedureCanvasTester : ScriptableObject
    {
        [SerializeField]
        private ProcedureTestPanel m_testPanelSample;
        public ProcedureTestPanel TestPanelSample => m_testPanelSample;
    }
}
