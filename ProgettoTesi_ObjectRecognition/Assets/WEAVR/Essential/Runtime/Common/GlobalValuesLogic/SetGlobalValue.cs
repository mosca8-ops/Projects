using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Variables/Set Variable Value")]
    public class SetGlobalValue : MonoBehaviour
    {
        [SerializeField]
        private string m_variableName;

        public void SetVariable()
        {
            GlobalValues.Current.SetVariable(m_variableName);
        }

        public void SetValue(bool value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(int value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(float value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(string value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(Color value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(Vector3 value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(Component value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(GameObject value) => GlobalValues.Current.SetValue(m_variableName, value);
    }
}
