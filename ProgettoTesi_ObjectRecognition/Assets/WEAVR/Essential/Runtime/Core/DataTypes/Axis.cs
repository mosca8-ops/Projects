using System.Text;
using UnityEngine;

namespace TXT.WEAVR
{
    public enum Axis
    {
        None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
    }

    public static class AxisExtensions
    {
        /// <summary>
        /// Gets only the specified axis from a vector, or 0 if axis is not set
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Filter(this Axis axis, Vector3 v)
        {
            return new Vector3(axis.HasFlag(Axis.X) ? v.x : 0, axis.HasFlag(Axis.Y) ? v.y : 0, axis.HasFlag(Axis.Z) ? v.z : 0);
        }

        /// <summary>
        /// Gets only the specified axis from <paramref name="v1"/>, or values from <paramref name="v2"/> if axis is not set
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="v1">The vector to get the active axes from</param>
        /// <param name="v2">The vector to get the unset axes from</param>
        /// <returns></returns>
        public static Vector3 Filter(this Axis axis, Vector3 v1, Vector3 v2)
        {
            return new Vector3(axis.HasFlag(Axis.X) ? v1.x : v2.x, axis.HasFlag(Axis.Y) ? v1.y : v2.y, axis.HasFlag(Axis.Z) ? v1.z : v2.z);
        }

        /// <summary>
        /// Gets only the specified axis from a vector, or 0 if axis is not set
        /// </summary>
        /// <param name="v"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Vector3 Filter(this Vector3 v, Axis axis)
        {
            return new Vector3(axis.HasFlag(Axis.X) ? v.x : 0, axis.HasFlag(Axis.Y) ? v.y : 0, axis.HasFlag(Axis.Z) ? v.z : 0);
        }

        /// <summary>
        /// Gets only the specified axis from <paramref name="v1"/>, or values from <paramref name="v2"/> if axis is not set
        /// </summary>
        /// <param name="v1">The vector to get the active axes from</param>
        /// <param name="axis"></param>
        /// <param name="v2">The vector to get the unset axes from</param>
        /// <returns></returns>
        public static Vector3 Filter(this Vector3 v1, Axis axis, Vector3 v2)
        {
            return new Vector3(axis.HasFlag(Axis.X) ? v1.x : v2.x, axis.HasFlag(Axis.Y) ? v1.y : v2.y, axis.HasFlag(Axis.Z) ? v1.z : v2.z);
        }

        public static string GetString(this Axis axis)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('[');
            if (axis.HasFlag(Axis.X))
            {
                builder.Append('x').Append(',').Append(' ');
            }
            if (axis.HasFlag(Axis.Y))
            {
                builder.Append('y').Append(',').Append(' ');
            }
            if (axis.HasFlag(Axis.Z))
            {
                builder.Append('z').Append(',').Append(' ');
            }
            if(builder[builder.Length - 1] == ' ')
            {
                builder.Length -= 2;
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}