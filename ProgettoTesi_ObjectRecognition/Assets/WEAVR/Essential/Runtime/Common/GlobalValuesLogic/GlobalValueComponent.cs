using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Variables/Global Variable")]
    public class GlobalValueComponent : MonoBehaviour
    {
        [SerializeField]
        private string m_variableName;
        [SerializeField]
        private ValuesStorage.ValueType m_valueType = ValuesStorage.ValueType.Any;
        [SerializeField]
        private bool m_checkOnStart = false;
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        private bool m_caseSensitive = true;

        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Bool)]
        private PlainListBoolValueCheck m_booleanChecks = new PlainListBoolValueCheck();
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Integer)]
        private PlainListIntValueCheck m_integerChecks = new PlainListIntValueCheck();
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.Float)]
        private PlainListFloatValueCheck m_floatChecks = new PlainListFloatValueCheck();
        [SerializeField]
        [ShowOnEnum(nameof(m_valueType), (int)ValuesStorage.ValueType.String)]
        private PlainListStringValueCheck m_stringChecks = new PlainListStringValueCheck();

        [SerializeField]
        [Space]
        private UnityEvent m_onValueChanged;

        public IValueCheckCollection ValueChecks { get; private set; }

        private ValuesStorage.Variable m_variable;
        public object Value
        {
            get => m_variable?.Value;
            set { if (m_variable != null) m_variable.Value = value; }
        }


        private void TransformValueChecks()
        {
            var currentChecks = GetChecks(m_valueType);
            if(ValueChecks != null && currentChecks != null && ValueChecks != currentChecks)
            {
                currentChecks.TryCopyFrom(ValueChecks);
            }
            ValueChecks = currentChecks;
        }

        private IValueCheckCollection GetChecks(ValuesStorage.ValueType valueType)
        {
            switch (valueType)
            {
                case ValuesStorage.ValueType.Bool: return m_booleanChecks;
                case ValuesStorage.ValueType.Float: return m_floatChecks;
                case ValuesStorage.ValueType.Integer: return m_integerChecks;
                case ValuesStorage.ValueType.String: return m_stringChecks;
            }
            return default;
        }

        public void SetVariable()
        {
            GlobalValues.Current.SetVariable(m_variableName);
        }

        public void SetValue(bool value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(int value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(float value) => GlobalValues.Current.SetValue(m_variableName, value);
        public void SetValue(string value) => GlobalValues.Current.SetValue(m_variableName, value);

        private void OnValidate()
        {
            m_booleanChecks.Component = this;
            m_floatChecks.Component = this;
            m_integerChecks.Component = this;
            m_stringChecks.Component = this;

            TransformValueChecks();
        }

        private void OnEnable()
        {
            m_variable = GlobalValues.Current.GetOrCreateVariable(m_variableName);
            if(m_variable != null)
            {
                m_variable.ValueChanged -= Variable_ValueChanged;
                m_variable.ValueChanged += Variable_ValueChanged;
            }
        }

        private void Start()
        {
            if (m_checkOnStart)
            {
                RecheckValue();
            }
        }

        private void OnDisable()
        {
            if (m_variable != null)
            {
                m_variable.ValueChanged -= Variable_ValueChanged;
            }
        }

        private void Variable_ValueChanged(object value)
        {
            RecheckValue();
        }

        private void RecheckValue()
        {
            m_onValueChanged?.Invoke();
            if (m_variable != null && m_variable.Type == m_valueType && m_valueType != ValuesStorage.ValueType.Any)
            {
                if (ValueChecks is PlainListStringValueCheck stringList)
                {
                    foreach (var check in stringList)
                    {
                        check.CaseSensitive = m_caseSensitive;
                        check.Recheck();
                    }
                }
                else
                {
                    ValueChecks?.Recheck();
                }
            }
        }

        public interface IValueCheckCollection
        {
            ValuesStorage.ValueType ValueType { get; }
            bool Recheck();
            ValueCheck[] Checks { get; }
            void TryCopyFrom(IValueCheckCollection other);
        }

        [Serializable]
        public abstract class ValueCheck
        {
            internal GlobalValueComponent m_component;

            public string VariableName => m_component.m_variableName;
            public ValuesStorage.ValueType ValueType { get; internal set; }

            public abstract UnityEventBase OnEqualsEvent { get; }
            public abstract bool Recheck();
            public abstract void TryCopyFrom(ValueCheck other);
        }

        public abstract class ValueCheck<T> : ValueCheck
        {
            public T value;
            public override UnityEventBase OnEqualsEvent => OnValueEquals;
            public virtual WeavrUnityEvent<T> OnValueEquals { get; }

            public override bool Recheck()
            {
                if(CheckIfEquals(value, GlobalValues.ValueOf(VariableName)))
                {
                    OnEqualValue();
                    return true;
                }
                return false;
            }

            protected virtual bool CheckIfEquals(object value, object otherValue) => Equals(value, otherValue);

            protected virtual void OnEqualValue()
            {
                OnValueEquals?.Invoke(value);
            }

            public override void TryCopyFrom(ValueCheck other)
            {
                if(other.OnEqualsEvent is UnityEventBase ueCopy)
                {
                    OnValueEquals?.TryCopyFrom(ueCopy);
                }
            }
        }

        [Serializable] public class BoolValueCheck : ValueCheck<bool> 
        {
            public UnityEventBoolean m_onValueEquals = new UnityEventBoolean();
            public override WeavrUnityEvent<bool> OnValueEquals => m_onValueEquals;
        }

        [Serializable] public class FloatValueCheck : ValueCheck<float>
        {
            public UnityEventFloat m_onValueEquals = new UnityEventFloat();
            public override WeavrUnityEvent<float> OnValueEquals => m_onValueEquals;
        }

        [Serializable] public class IntValueCheck : ValueCheck<int>
        {
            public UnityEventInt m_onValueEquals = new UnityEventInt();
            public override WeavrUnityEvent<int> OnValueEquals => m_onValueEquals;
        }

        [Serializable] public class StringValueCheck : ValueCheck<string>
        {
            public UnityEventString m_onValueEquals = new UnityEventString();
            public bool CaseSensitive { get; set; } = true;
            public override WeavrUnityEvent<string> OnValueEquals => m_onValueEquals;

            protected override bool CheckIfEquals(object value, object otherValue)
            {
                return CaseSensitive && value is string s1 && otherValue is string s2 ? string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase) : base.CheckIfEquals(value, otherValue);
            }
        }

        public class PlainListValueCheck<T> : PlainList<T>, IValueCheckCollection where T : ValueCheck, new()
        {
            public GlobalValueComponent Component { get; set; }
            public virtual ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Any;

            public ValueCheck[] Checks => this.ToArray();

            public bool Recheck()
            {
                bool raised = false;
                foreach(var check in this)
                {
                    raised |= check.Recheck();
                }
                return raised;
            }

            protected T CreateValueCheck() => new T()
            {
                m_component = Component,
                ValueType = ValueType,
            };

            public void TryCopyFrom(IValueCheckCollection other)
            {
                var lastList = other.Checks;
                for (int i = 0; i < lastList.Length; i++)
                {
                    T currentCheck;
                    if (Count > i)
                    {
                        currentCheck = this[i];
                    }
                    else
                    {
                        currentCheck = new T()
                        {
                            m_component = Component,
                            ValueType = ValueType,
                        };

                        Add(currentCheck);
                    }

                    currentCheck?.TryCopyFrom(lastList[i]);
                }
            }
        }

        [Serializable] public class PlainListBoolValueCheck : PlainListValueCheck<BoolValueCheck> 
        {
            public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Bool;
        }
        [Serializable] public class PlainListFloatValueCheck : PlainListValueCheck<FloatValueCheck>
        {
            public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Float;
        }
        [Serializable] public class PlainListIntValueCheck : PlainListValueCheck<IntValueCheck>
        {
            public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Integer;
        }
        [Serializable] public class PlainListStringValueCheck : PlainListValueCheck<StringValueCheck>
        {
            public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.String;
        }
    }
}
