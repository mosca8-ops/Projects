using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.ScreenGrab
{
    [CustomEditor(typeof(DXGI))]
    public class DXGIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(GUILayout.Button("Save Configuration"))
            {
                (target as DXGI).SaveCurrentConfigurationToDisk();
            }
        }
    }
}
