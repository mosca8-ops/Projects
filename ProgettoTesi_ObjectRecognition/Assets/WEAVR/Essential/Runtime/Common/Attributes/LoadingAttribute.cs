using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LoadingAttribute : WeavrAttribute
    {
        public string Label { get; set; }
        public string IsLoadingMethodName { get; private set; }
        public string LoadingProgressMethodName { get; private set; }

        public LoadingAttribute(string isLoadingMethod)
        {
            IsLoadingMethodName = isLoadingMethod;
        }

        public LoadingAttribute(string isLoadingMethod, string loadingProgressMethod)
        {
            IsLoadingMethodName = isLoadingMethod;
            LoadingProgressMethodName = loadingProgressMethod;
        }
    }
}
