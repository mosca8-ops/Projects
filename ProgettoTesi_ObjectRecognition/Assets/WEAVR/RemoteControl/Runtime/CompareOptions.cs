using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{
    public class CompareOptions
    {
        public bool convertToLower;
        public bool removeSpaces;

        public string Apply(string s)
        {
            if (removeSpaces)
                s = s.Replace(" ", "");

            if (convertToLower)
                s = s.ToLower();

            return s;
        }
    }
}
