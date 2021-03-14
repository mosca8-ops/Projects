using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Utility;
using UnityEngine;

namespace TXT.WEAVR
{

    public static class DataTypesExtensions
    {
        //----------------------------- VECTORS EXTENSIONS ------------------------------------/
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        //-------------------------------------------------------------------------------------/

        public static AnimationCurve Duplicate(this AnimationCurve curve)
        {
            return new AnimationCurve(new List<Keyframe>(curve.keys).ToArray());
        }

        public static AnimationCurve Normalize(this AnimationCurve curve)
        {
            return Normalize(curve, 1);
        }

        public static bool IsSimilarTo(this AnimationCurve a, AnimationCurve b)
        {
            if(a.length != b.length || a.postWrapMode != b.postWrapMode || a.preWrapMode != b.preWrapMode)
            {
                return false;
            }

            for (int i = 0; i < a.length; i++)
            {
                if(a[i].time != b[i].time 
                    || a[i].value != b[i].value 
                    || a[i].weightedMode != b[i].weightedMode
                    || a[i].inTangent != b[i].inTangent
                    || a[i].outTangent != b[i].outTangent
                    || a[i].inWeight != b[i].inWeight
                    || a[i].outWeight != b[i].outWeight)
                {
                    return false;
                }
            }
            return true;
        }

        public static AnimationCurve Normalize(this AnimationCurve curve, float time)
        {
            if(curve.length == 0 || time <= 0) { return curve; }

            float hSlide = curve[0].time;
            float hScale = curve[curve.length - 1].time - curve[0].time;
            float hInverseScale = hScale;
            if (hScale > 0)
            {
                hInverseScale = time / hScale;
                hScale /= time;
            }
            else
            {
                hInverseScale = 1;
            }

            float minValue = curve[0].value;
            float maxValue = curve[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                float value = curve[i].value;
                if (value > maxValue)
                {
                    maxValue = value;
                }
                if (value < minValue)
                {
                    minValue = value;
                }
            }

            float vScale = maxValue - minValue;

            if (vScale > 0)
            {
                vScale = 1f / vScale;
            }
            else
            {
                vScale = 1f;
            }
            
            List<Keyframe> keys = new List<Keyframe>();
            for (int i = 0; i < curve.length; i++)
            {
                var key = curve[i];
                key.inTangent *= hScale;
                key.outTangent *= hScale;
                key.time = (key.time * hInverseScale) - hSlide;
                key.value = (key.value * vScale) - minValue;
                keys.Add(key);
                //curve.keys[i] = key;
            }

            curve.keys = keys.ToArray();
            return curve;
        }
    }

    public static class DamerauLevenshtein
    {
        public static int SimilarityDistanceTo(this string @string, string targetString)
        {
            return DamerauLevenshteinDistance(@string, targetString);
        }

        public static int DamerauLevenshteinDistance(string string1, string string2)
        {
            if (string.IsNullOrEmpty(string1))
            {
                if (!string.IsNullOrEmpty(string2))
                    return string2.Length;

                return 0;
            }

            if (string.IsNullOrEmpty(string2))
            {
                if (!string.IsNullOrEmpty(string1))
                    return string1.Length;

                return 0;
            }

            int length1 = string1.Length;
            int length2 = string2.Length;

            int[,] d = new int[length1 + 1, length2 + 1];

            int cost, del, ins, sub;

            for (int i = 0; i <= d.GetUpperBound(0); i++)
                d[i, 0] = i;

            for (int i = 0; i <= d.GetUpperBound(1); i++)
                d[0, i] = i;

            for (int i = 1; i <= d.GetUpperBound(0); i++)
            {
                for (int j = 1; j <= d.GetUpperBound(1); j++)
                {
                    if (string1[i - 1] == string2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    del = d[i - 1, j] + 1;
                    ins = d[i, j - 1] + 1;
                    sub = d[i - 1, j - 1] + cost;

                    d[i, j] = Math.Min(del, Math.Min(ins, sub));

                    if (i > 1 && j > 1 && string1[i - 1] == string2[j - 2] && string1[i - 2] == string2[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];
        }
    }
}
