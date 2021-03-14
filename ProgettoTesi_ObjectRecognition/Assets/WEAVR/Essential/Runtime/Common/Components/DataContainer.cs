using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Utilities/Data Container")]
    public class DataContainer : MonoBehaviour
    {
        public PlainListBool booleans;
        public PlainListInt integers;
        public PlainListFloat floats;
        public PlainListString strings;
        public PlainListGameObject gameObjects;

        public void InvalidateIndices()
        {
            booleans.Invalidate = true;
            integers.Invalidate = true;
            floats.Invalidate = true;
            strings.Invalidate = true;
            gameObjects.Invalidate = true;
        }

        public void SetCommonIndex(int index)
        {
            booleans.CurrentIndex = index;
            integers.CurrentIndex = index;
            floats.CurrentIndex = index;
            strings.CurrentIndex = index;
            gameObjects.CurrentIndex = index;
        }
    }
}
