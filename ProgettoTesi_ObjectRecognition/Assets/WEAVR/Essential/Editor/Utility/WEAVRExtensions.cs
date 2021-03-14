namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public static class WEAVRExtensions
    {
        public static bool DerivesFrom(this Type type, Type baseType) {
            if(baseType == null) {
                return false;
            }
            while(type != baseType && type != null) {
                type = type.BaseType;
            }
            return type == baseType;
        }

        /// <summary>
        /// Checks if a <see cref="UnityEngine.Object"/> has been destroyed.
        /// </summary>
        /// <param name="obj"><see cref="UnityEngine.Object"/> reference to check for destructedness</param>
        /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
        public static bool IsDestroyed(this UnityEngine.Object obj) {
            // UnityEngine overloads the == opeator for the Object type
            // and returns null when the object has been destroyed, but 
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return obj == null && !ReferenceEquals(obj, null);
        }
    }
}