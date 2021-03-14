using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [System.Serializable]
    public class Hint : ScriptableObject
    {
        [LongText]
        public LocalizedString text;
    }
}
