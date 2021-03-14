using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TypeExtensions
{

    public static bool IsEnum(this Type type) {
        return type.IsEnum;
    }
}