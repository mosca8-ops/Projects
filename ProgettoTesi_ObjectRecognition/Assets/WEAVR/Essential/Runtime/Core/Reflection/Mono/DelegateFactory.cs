namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    using Getter = System.Func<object, object>;
    using Setter = System.Action<object, object>;

    public static class DelegateFactory
    {

        private static Getter[] s_getters = new Getter[12];
        private static Setter[] s_setters = new Setter[12];

        private const BindingFlags k_DefaultFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        private static Dictionary<string, PropertyInfo> m_properties = new Dictionary<string, PropertyInfo>();
        private static Dictionary<string, FieldInfo> m_fields = new Dictionary<string, FieldInfo>();

        private static PropertyInfo GetPropertyInfo(Type source, string propertyName)
        {
            string key = source.FullName + '.' + propertyName;
            if (!m_properties.TryGetValue(key, out PropertyInfo info))
            {
                PropertyInfo propertyInfo = source.GetProperty(propertyName, k_DefaultFlags);
                if (propertyInfo == null)
                {
                    source = source.BaseType;
                    while (propertyInfo == null && source != null)
                    {
                        propertyInfo = source.GetProperty(propertyName, k_DefaultFlags);
                        source = source.BaseType;
                    }
                }

                m_properties[key] = propertyInfo;
                return propertyInfo;
            }
            return info as PropertyInfo;
        }

        private static FieldInfo GetFieldInfo(Type source, string fieldName)
        {
            string key = source.FullName + '.' + fieldName;
            if (!m_fields.TryGetValue(key, out FieldInfo info))
            {
                FieldInfo fieldInfo = source.GetField(fieldName, k_DefaultFlags);
                if (fieldInfo == null)
                {
                    source = source.BaseType;
                    while (fieldInfo == null && source != null)
                    {
                        fieldInfo = source.GetField(fieldName, k_DefaultFlags);
                        source = source.BaseType;
                    }
                }

                m_fields[key] = fieldInfo;
                return fieldInfo;
            }
            return info as FieldInfo;
        }

        public static MemberInfo GetMemberInfo(this Type type, string name)
        {
            return GetFieldInfo(type, name) ??  GetPropertyInfo(type, name) as MemberInfo;
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified property name
        /// </summary>
        /// <param name="type">The type to get the property from</param>
        /// <param name="propertyName">The property name to get the accessor from</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> PropertyGet(this Type type, string propertyName)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException(string.Format("Property '{0}.{1}' was not found",
                                            type.FullName, propertyName));
            }
            return FastGetter(propertyInfo);
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified property name
        /// </summary>
        /// <param name="type">The type to get the property from</param>
        /// <param name="propertyName">The property name to get the accessor from</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> PropertyGetNoThrow(this Type type, string propertyName)
        {
            return GetPropertyInfo(type, propertyName)?.FastGetter();
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified property info
        /// </summary>
        /// <param name="propertyInfo">The property info</param>
        /// <param name="declaringType">The type holding the property</param>
        /// <param name="throwOnMissing">Whether to throw an exception when getter is missing</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> FastGetter(this PropertyInfo propertyInfo, Type declaringType = null, bool throwOnMissing = false)
        {
            var getMethod = propertyInfo.GetMethod;
            if (getMethod == null)
            {
                if (throwOnMissing)
                {
                    throw new ArgumentException(string.Format("Property '{0}.{1}' does not have a Get accessor",
                                                    propertyInfo.DeclaringType.FullName, propertyInfo.Name));
                }
                else
                {
                    return null;
                }
            }
            
            var sourceObjectParam = Expression.Parameter(typeof(object));
            Expression returnExpression =
                Expression.Call(Expression.Convert
                (sourceObjectParam, declaringType ?? propertyInfo.DeclaringType), getMethod);
            if (!propertyInfo.PropertyType.IsClass)
            {
                returnExpression = Expression.Convert(returnExpression, typeof(object));
            }
            return (Func<object, object>)Expression.Lambda
            (returnExpression, sourceObjectParam).Compile();
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified property name
        /// </summary>
        /// <param name="type">The type to get the property from</param>
        /// <param name="propertyName">The property name to get the accessor from</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> PropertySet(this Type type, string propertyName)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException(string.Format("Property '{0}.{1}' was not found",
                                            type.FullName, propertyName));
            }
            return FastSetter(propertyInfo, type);
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified property name
        /// </summary>
        /// <param name="type">The type to get the property from</param>
        /// <param name="propertyName">The property name to get the accessor from</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> PropertySetNoThrow(this Type type, string propertyName)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName);
            return propertyInfo != null ? FastSetter(propertyInfo, type) : null;
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified property info
        /// </summary>
        /// <param name="propInfo">The property info</param>
        /// <param name="declaringType">The type holding the property</param>
        /// <param name="throwOnMissing">Whether to throw an exception if setter is missing</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> FastSetter(this PropertyInfo propInfo, Type declaringType = null, bool throwOnMissing = false)
        {
            var setMethod = propInfo.SetMethod;
            if (setMethod == null)
            {
                if (throwOnMissing)
                {
                    throw new ArgumentException(string.Format("Property '{0}.{1}' does not have a Set accessor",
                                                (declaringType ?? propInfo.DeclaringType).FullName, propInfo.Name));
                }
                else
                {
                    return null;
                }
            }
            var sourceObjectParam = Expression.Parameter(typeof(object));
            Type type = declaringType ?? propInfo.DeclaringType;
            if (type.IsValueType)
            {
                // Value types cannot handle left hand conversion and assignment
                return (o, v) => propInfo.SetValue(o, v);
            }
            ParameterExpression propertyValueParam;
            Expression valueExpression;
            if (propInfo.PropertyType == typeof(object))
            {
                propertyValueParam = Expression.Parameter(propInfo.PropertyType);
                valueExpression = propertyValueParam;
            }
            else
            {
                propertyValueParam = Expression.Parameter(typeof(object));
                valueExpression = Expression.Convert(propertyValueParam, propInfo.PropertyType);
            }
            return (Action<object, object>)Expression.Lambda(Expression.Call
                   (Expression.Convert(sourceObjectParam, type),
                   setMethod, valueExpression),
                   sourceObjectParam, propertyValueParam).Compile();
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified field name
        /// </summary>
        /// <param name="type">The type to get the field from</param>
        /// <param name="fieldName">The field name to get the accessor from</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> FieldGet(this Type type, string fieldName)
        {
            var fieldInfo = GetFieldInfo(type, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentException(string.Format("Field '{0}.{1}' was not found",
                                            type.FullName, fieldName));
            }
            return FastGetter(fieldInfo, type);
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified field name
        /// </summary>
        /// <param name="type">The type to get the field from</param>
        /// <param name="fieldName">The field name to get the accessor from</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> FieldGetNoThrow(this Type type, string fieldName)
        {
            return GetFieldInfo(type, fieldName)?.FastGetter(type);
        }

        /// <summary>
        /// Gets a Get accessor delegate (which is >20x faster than Reflection) for specified field info
        /// </summary>
        /// <param name="fieldInfo">The field info</param>
        /// <param name="declaringType">The type holding the field</param>
        /// <returns>The Get accessor</returns>
        public static Func<object, object> FastGetter(this FieldInfo fieldInfo, Type declaringType = null)
        {
            var sourceParam = Expression.Parameter(typeof(object));
            Expression returnExpression = Expression.Field
            (Expression.Convert(sourceParam, declaringType ?? fieldInfo.DeclaringType), fieldInfo);
            if (!fieldInfo.FieldType.IsClass)
            {
                returnExpression = Expression.Convert(returnExpression, typeof(object));
            }
            var lambda = Expression.Lambda(returnExpression, sourceParam);
            return (Func<object, object>)lambda.Compile();
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified field name
        /// </summary>
        /// <param name="type">The type to get the field from</param>
        /// <param name="fieldName">The field name to get the accessor from</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> FieldSet(this Type type, string fieldName)
        {
            var fieldInfo = GetFieldInfo(type, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentException(string.Format("Field '{0}.{1}' was not found",
                                            type.FullName, fieldName));
            }
            return FastSetter(fieldInfo, type);
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified field name
        /// </summary>
        /// <param name="type">The type to get the field from</param>
        /// <param name="fieldName">The field name to get the accessor from</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> FieldSetNoThrow(this Type type, string fieldName)
        {
            var fieldInfo = GetFieldInfo(type, fieldName);
            return fieldInfo != null ? FastSetter(fieldInfo, type) : null;
        }

        public static MemberInfo GetMemberInfoFromPath(this Type type, string memberPath)
        {
            var splits = memberPath.Split('.');
            if(splits.Length == 1)
            {
                return GetMemberInfo(type, splits[0]);
            }

            Type t = type;
            MemberInfo mInfo = null;

            for (int i = 0; i < splits.Length; i++)
            {
                if (splits[i] == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // Get the index
                    int elemIndex = int.Parse(splits[i + 1].Substring(5, splits[i + 1].Length - 6));
                    if (t.IsArray && t.HasElementType)
                    {
                        i++;
                        continue;
                    }
                    else if (typeof(IList).IsAssignableFrom(t))
                    {
                        i++;
                        continue;
                    }
                }

                mInfo = GetMemberInfo(t, splits[i]);
                if(mInfo is FieldInfo fInfo)
                {
                    t = fInfo.FieldType;
                }
                else if(mInfo is PropertyInfo pInfo)
                {
                    t = pInfo.PropertyType;
                }
            }

            return mInfo;
        }

        public static Func<object, object> FieldPathGet(this Type type, string fieldsPath)
        {
            if (string.IsNullOrEmpty(fieldsPath))
            {
                throw new ArgumentException("FieldPath is empty");
            }

            var splits = fieldsPath.Split('.');
            if (splits.Length == 1)
            {
                return FieldGet(type, splits[0]);
            }

            FieldInfo info = null;
            Type t = type;
            
            var getters = new Getter[splits.Length];

            int index = splits.Length;
            int f = 0;
            for (int i = 0; i < splits.Length; i++)
            {
                if (splits[i] == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // Get the index
                    int elemIndex = int.Parse(splits[i + 1].Substring(5, splits[i + 1].Length - 6));
                    if (t.IsArray && t.HasElementType)
                    {
                        getters[f] = o => (o as Array).GetValue(elemIndex);
                        i++;
                        f++;
                        index--;
                        continue;
                    }
                    else if (typeof(IList).IsAssignableFrom(t))
                    {
                        getters[f] = o => (o as IList)[elemIndex];
                        i++;
                        f++;
                        index--;
                        continue;
                    }
                }

                info = GetFieldInfo(t, splits[i]);
                getters[f++] = info.FastGetter(t);
                t = info.FieldType;
            }

            return o =>
            {
                for (int i = 0; i < index; i++)
                {
                    o = getters[i](o);
                }
                return o;
            };
        }

        public static Action<object, object> FieldPathSet(this Type type, string fieldsPath)
        {
            if (string.IsNullOrEmpty(fieldsPath))
            {
                throw new ArgumentException("FieldPath is empty");
            }

            var splits = fieldsPath.Split('.');
            if(splits.Length == 1)
            {
                return FieldSet(type, splits[0]);
            }
            
            FieldInfo info = null;
            Type t = type;

            var setters = new Setter[splits.Length];
            var getters = new Getter[splits.Length];

            var values = new object[splits.Length];

            int index = splits.Length - 1;
            int f = 0;
            for (int i = 0; i < splits.Length; i++)
            {
                if (splits[i] == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // Get the index
                    int elemIndex = int.Parse(splits[i + 1].Substring(5, splits[i + 1].Length - 6));
                    if (t.IsArray && t.HasElementType)
                    {
                        getters[f] = o => (o as Array).GetValue(elemIndex);
                        setters[f] = (o, v) => (o as Array).SetValue(v, elemIndex);
                        i++;
                        f++;
                        index--;
                        t = t.GetElementType();
                        continue;
                    }
                    else if (typeof(IList).IsAssignableFrom(t))
                    {
                        getters[f] = o => (o as IList)[elemIndex];
                        setters[f] = (o, v) => (o as IList)[elemIndex] = v;
                        i++;
                        f++;
                        index--;
                        t = t.GetGenericArguments()[0];
                        continue;
                    }
                }

                info = GetFieldInfo(t, splits[i]);
                getters[f] = info.FastGetter(t);
                if (info.FieldType.IsValueType || f == index)
                {
                    setters[f] = info.FastSetter(t);
                }
                f++;
                t = info.FieldType;
            }

            return (o, v) =>
            {
                object obj = o;
                for (int i = 0; i < index; i++)
                {
                    values[i] = obj;
                    obj = getters[i](obj);
                }
                values[index] = obj;
                setters[index](obj, v);
                int j = index;
                while(j-- > 0)
                {
                    setters[j]?.Invoke(values[j], values[j + 1]);
                }
            };
        }

        public static Action<object, object> ValuePathSet(this Type type, string fieldsPath)
        {
            if (string.IsNullOrEmpty(fieldsPath))
            {
                throw new ArgumentException("FieldPath is empty");
            }

            var splits = fieldsPath.Split('.');
            return ValuePathSet(type, splits);
        }

        private static Setter ValuePathSet(Type type, params string[] splits)
        {
            if (splits.Length == 1)
            {
                return FieldSet(type, splits[0]);
            }

            FieldInfo fieldInfo = null;
            PropertyInfo propertyInfo = null;
            Type t = type;

            var setters = new Setter[splits.Length];
            var getters = new Getter[splits.Length];


            int index = splits.Length - 1;
            int f = 0;
            for (int i = 0; i < splits.Length; i++)
            {
                if(splits[i] == "Array" && splits.Length > i + 1 && splits[i + 1].StartsWith("data"))
                {
                    // Get the index
                    int elemIndex = int.Parse(splits[i + 1].Substring(5, splits[i + 1].Length - 6));
                    if(t.IsArray && t.HasElementType)
                    {
                        getters[f] = o => (o as Array).GetValue(elemIndex);
                        setters[f] = (o, v) => (o as Array).SetValue(v, elemIndex);
                        i++;
                        f++;
                        index--;
                        t = t.GetElementType();
                        continue;
                    }
                    else if(typeof(IList).IsAssignableFrom(t))
                    {
                        getters[f] = o => (o as IList)[elemIndex];
                        setters[f] = (o, v) => (o as IList)[elemIndex] = v;
                        i++;
                        f++;
                        index--;
                        t = t.GetGenericArguments()[0];
                        continue;
                    }
                }

                fieldInfo = GetFieldInfo(t, splits[i]);
                if (fieldInfo != null)
                {
                    getters[f] = fieldInfo.FastGetter(t);
                    if (fieldInfo.FieldType.IsValueType || f == index)
                    {
                        setters[f] = fieldInfo.FastSetter(t);
                    }
                    t = fieldInfo.FieldType;
                }
                else
                {
                    propertyInfo = GetPropertyInfo(t, splits[i]);
                    if(propertyInfo == null)
                    {
                        throw new ArgumentException("ValuePath cannot be parsed");
                    }
                    getters[f] = propertyInfo.FastGetter(t);
                    if (propertyInfo.PropertyType.IsValueType || f == index)
                    {
                        setters[f] = propertyInfo.FastSetter(t);
                    }
                    t = propertyInfo.PropertyType;
                }
                f++;
            }

            var values = new object[splits.Length];
            return (o, v) =>
            {
                object obj = o;
                for (int i = 0; i < index; i++)
                {
                    values[i] = obj;
                    obj = getters[i](obj);
                }
                values[index] = obj;
                setters[index](obj, v);
                int j = index;
                while (j-- > 0)
                {
                    setters[j]?.Invoke(values[j], values[j + 1]);
                }
            };
        }


        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified field info
        /// </summary>
        /// <param name="fieldInfo">The field info</param>
        /// <param name="declaringType">The type holding the field</param>
        /// <returns>The Set accessor</returns>
        public static Action<object, object> FastSetter(this FieldInfo fieldInfo, Type declaringType = null)
        {
            Type type = declaringType ?? fieldInfo.DeclaringType;
            if (type.IsValueType)
            {
                // Value types cannot handle left hand conversion and assignment
                return (o, v) => fieldInfo.SetValue(o, v);
            }
            var sourceParam = Expression.Parameter(typeof(object));
            var valueParam = Expression.Parameter(typeof(object));
            var convertedValueExpr = Expression.Convert(valueParam, fieldInfo.FieldType);
            Expression returnExpression = Expression.Assign(Expression.Field
            (Expression.Convert(sourceParam, type), fieldInfo), convertedValueExpr);
            if (!fieldInfo.FieldType.IsClass)
            {
                returnExpression = Expression.Convert(returnExpression, typeof(object));
            }
            var lambda = Expression.Lambda(typeof(Action<object, object>),
                returnExpression, sourceParam, valueParam);
            return (Action<object, object>)lambda.Compile();
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified field name
        /// </summary>
        /// <typeparam name="TSource">The caller type</typeparam>
        /// <typeparam name="TProperty">The field type</typeparam>
        /// <param name="fieldName">The field name</param>
        /// <returns>The Set accessor</returns>
        public static Action<TSource, TProperty> FieldSet<TSource, TProperty>(string fieldName)
        {
            return FieldSet<TSource, TProperty>(typeof(TSource), fieldName);
        }

        /// <summary>
        /// Gets a Set accessor delegate (which is >10x faster than Reflection) for specified field name
        /// </summary>
        /// <typeparam name="TSource">The caller type</typeparam>
        /// <typeparam name="TProperty">The field type</typeparam>
        /// <param name="type">The type to get the field from</param>
        /// <param name="fieldName">The field name</param>
        /// <returns>The Set accessor</returns>
        public static Action<TSource, TProperty> FieldSet<TSource, TProperty>(Type type, string fieldName)
        {
            var fieldInfo = GetFieldInfo(type, fieldName);
            if (fieldInfo == null)
            {
                throw new ArgumentException(string.Format("Field '{0}.{1}' was not found",
                                            type.FullName, fieldName));
            }
            var sourceParam = Expression.Parameter(typeof(TSource));
            var valueParam = Expression.Parameter(typeof(TProperty));
            var te = Expression.Lambda(typeof(Action<TSource, TProperty>),
                Expression.Assign(Expression.Field(sourceParam, fieldInfo), valueParam),
                sourceParam, valueParam);
            return (Action<TSource, TProperty>)te.Compile();
        }
    }
}