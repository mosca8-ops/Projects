using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureStateUtility
    {
        public static string PadNumbers(string _input, bool _removeSpaces = true)
        {
            if (_removeSpaces)
                _input = _input.Replace(" ", "");
            return Regex.Replace(_input, "[0-9]+", match => match.Value.PadLeft(10, '0'));
        }

        public static bool IsStringNullEmptyOrWhitespace(string s)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s))
                return true;

            return false;
        }
    } 
}
