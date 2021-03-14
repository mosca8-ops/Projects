namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DynamicEnumAttribute : OverrideAccessorsAttribute
    {
        public string CollectionName { get; private set; }
        public bool IsNullable { get; private set; }

        private bool _alreadySet;

        public DynamicEnumAttribute(string collectionMemberName, bool isNullable = false) {
            CollectionName = collectionMemberName;
            IsNullable = isNullable;
        }

        public override Action<object, object> GetSetter(object obj, MemberInfo memberInfo, Action<object, object> fallbackSetter) {
            var property = Property.Create(obj, CollectionName);
            if(property == null) {
                return fallbackSetter;
            }
            _alreadySet = false;
            return (o, v) => {
                if(_alreadySet || v == null) {
                    fallbackSetter(o, v);
                    return;
                }

                property.Owner = o;
                var collection = property.Value as IEnumerable<object>;
                if(collection != null) {
                    // now check if find by index or by string
                    int index = 0;
                    string vString = v.ToString();
                    if(int.TryParse(vString, out index) && index >= 0) {
                        int count = 0;
                        foreach(var elem in collection) {
                            if(index == count++) {
                                fallbackSetter(o, elem);
                                break;
                            }
                        }
                    } else {
                        // Here by string
                        foreach(var elem in collection) {
                            if(elem != null && elem.ToString().Equals(vString)) {
                                fallbackSetter(o, elem);
                                break;
                            }
                        }
                    }
                }

                _alreadySet = true;
            };
        }

        public override Action<object, object> GetSetter(Type ownerType, MemberInfo memberInfo, Action<object, object> fallbackSetter)
        {
            var property = Property.Create(ownerType, CollectionName);
            if (property == null)
            {
                return fallbackSetter;
            }
            _alreadySet = false;
            return (o, v) => {
                if (_alreadySet || v == null)
                {
                    fallbackSetter(o, v);
                    return;
                }

                property.Owner = o;
                var collection = property.Value as IEnumerable<object>;
                if (collection != null)
                {
                    // now check if find by index or by string
                    int index = 0;
                    string vString = v.ToString();
                    if (int.TryParse(vString, out index) && index >= 0)
                    {
                        int count = 0;
                        foreach (var elem in collection)
                        {
                            if (index == count++)
                            {
                                fallbackSetter(o, elem);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Here by string
                        foreach (var elem in collection)
                        {
                            if (elem != null && elem.ToString().Equals(vString))
                            {
                                fallbackSetter(o, elem);
                                break;
                            }
                        }
                    }
                }

                _alreadySet = true;
            };
        }
    }
}