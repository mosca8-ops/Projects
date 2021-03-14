using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShowOnEnumAttribute : WeavrAttribute
    {
        public string FullEnumFieldString { get; private set; }
        public string[] EnumFields { get; private set; }
        public int EnumValue { get; private set; }
        public bool HideOnEnum { get; private set; }

        /// <summary>
        /// This attribute will show/hide the field/property when the <paramref name="fieldName"/> has value <paramref name="enumValue"/>
        /// </summary>
        /// <param name="fieldName">The name of the controlling field. Note: multiple fields (separated by ';') can be added instead</param>
        /// <param name="enumValue">The value of the enum</param>
        /// <param name="hideOnEnum">[Optional] Whether to hide when field has enum value</param>
        public ShowOnEnumAttribute(string fieldName, int enumValue, bool hideOnEnum = false)
        {
            FullEnumFieldString = fieldName;
            EnumFields = fieldName.Trim().Split(';');
            EnumValue = enumValue;
            HideOnEnum = hideOnEnum;
        }

        /// <summary>
        /// This attribute will show/hide the field/property when the <paramref name="fieldName"/> has value <paramref name="@enum"/>
        /// </summary>
        /// <param name="fieldName">The name of the controlling field. Note: multiple fields (separated by ';') can be added instead</param>
        /// <param name="@enum">The value of the enum</param>
        /// <param name="hideOnEnum">[Optional] Whether to hide when field has enum value</param>
        public ShowOnEnumAttribute(string fieldName, Enum @enum, bool hideOnEnum = false)
        {
            FullEnumFieldString = fieldName;
            EnumFields = fieldName.Trim().Split(';');
            EnumValue = GetEnumInteger(@enum);
            HideOnEnum = hideOnEnum;
        }

        private int GetEnumInteger(Enum e)
        {
            var names = Enum.GetNames(e.GetType());
            for (int i = 0; i < names.Length; i++)
            {
                if(names[i] == e.ToString())
                {
                    return (int)Enum.GetValues(e.GetType()).GetValue(i);
                }
            }
            return -1;
        }
    }
}