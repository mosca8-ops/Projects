using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace TXT.WEAVR
{

    public abstract class VectorNField<T, V, F> : Field<T> 
        where F : BaseField<V>, new()
    {
        LabeledField<F, V>[] m_Fields;

        protected abstract int componentCount { get; }
        public virtual string GetComponentName(int i)
        {
            switch (i)
            {
                case 0:
                    return "x";
                case 1:
                    return "y";
                case 2:
                    return "z";
                case 3:
                    return "w";
                default:
                    return "a";
            }
        }

        void CreateTextField()
        {
            m_Fields = new LabeledField<F, V>[componentCount];


            for (int i = 0; i < m_Fields.Length; ++i)
            {
                m_Fields[i] = new LabeledField<F, V>(GetComponentName(i));
                m_Fields[i].control.AddToClassList("fieldContainer");
                m_Fields[i].AddToClassList("fieldContainer");
                m_Fields[i].RegisterCallback<ChangeEvent<V>, int>(RegisterValueChangedCallback, i);
            }

            m_Fields[0].label.AddToClassList("first");
        }

        public override bool indeterminate
        {
            get
            {
                return m_Fields[0].indeterminate;
            }
            set
            {
                foreach (var field in m_Fields)
                {
                    field.indeterminate = value;
                }
            }
        }

        protected abstract void SetValueComponent(ref T value, int i, V componentValue);
        protected abstract V GetValueComponent(ref T value, int i);

        void RegisterValueChangedCallback(ChangeEvent<V> e, int component)
        {
            T newValue = value;
            SetValueComponent(ref newValue, component, m_Fields[component].value);
            value = newValue;
        }

        public VectorNField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;

            foreach (var field in m_Fields)
            {
                Add(field);
            }
        }

        protected override void ValueToGUI(bool force)
        {
            T value = this.value;
            for (int i = 0; i < m_Fields.Length; ++i)
            {
                if (!m_Fields[i].control.HasFocus() || force)
                {
                    m_Fields[i].SetValueWithoutNotify(GetValueComponent(ref value, i));
                }
            }
        }
    }

    public class Vector3Field : VectorNField<Vector3, float, FloatField>
    {
        protected override int componentCount { get { return 3; } }
        protected override void SetValueComponent(ref Vector3 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                default:
                    value.z = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector3 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }
    }

    public class Vector2Field : VectorNField<Vector2, float, FloatField>
    {
        protected override int componentCount { get { return 2; } }
        protected override void SetValueComponent(ref Vector2 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                default:
                    value.y = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector2 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                default:
                    return value.y;
            }
        }
    }

    public class Vector4Field : VectorNField<Vector4, float, FloatField>
    {
        protected override int componentCount { get { return 4; } }
        protected override void SetValueComponent(ref Vector4 value, int i, float componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                case 2:
                    value.z = componentValue;
                    break;
                default:
                    value.w = componentValue;
                    break;
            }
        }

        protected override float GetValueComponent(ref Vector4 value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                case 2:
                    return value.z;
                default:
                    return value.w;
            }
        }
    }

    public class Vector2IntField : VectorNField<Vector2Int, int, IntegerField>
    {
        protected override int componentCount => 2;

        protected override int GetValueComponent(ref Vector2Int value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                default:
                    return value.y;
            }
        }

        protected override void SetValueComponent(ref Vector2Int value, int i, int componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                default:
                    value.y = componentValue;
                    break;
            }
        }
    }

    public class Vector3IntField : VectorNField<Vector3Int, int, IntegerField>
    {
        protected override int componentCount => 3;

        protected override int GetValueComponent(ref Vector3Int value, int i)
        {
            switch (i)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        protected override void SetValueComponent(ref Vector3Int value, int i, int componentValue)
        {
            switch (i)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                default:
                    value.z = componentValue;
                    break;
            }
        }
    }
}
