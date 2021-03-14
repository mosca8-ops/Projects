using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    public abstract class ValueIndicator : MonoBehaviour
    {
        public enum ValueImportance { Normal, Valid, Critical }
        public abstract string Measure { get; set; }
        public abstract string MeasureUnit { get; set; }
        public abstract void SetValue(float value);

        public virtual void SetValue(float value, ValueImportance valueImportance) => SetValue(value);
    }
}