using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public static class RemoteControlExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] SubArray(this byte[] data, int startIndex) => SubArray(data, startIndex, data.Length - startIndex);

        public static byte[] SubArray(this byte[] data, int startIndex, int length)
        {
            byte[] array = new byte[length];
            Array.Copy(data, startIndex, array, 0, length);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Range(this byte[] data, int from, int toExcluded) => SubArray(data, from, toExcluded - from);
    }
}
