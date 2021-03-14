using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public class GlobalValuesState : SpecialComponentState<GlobalValues>
    {
        private VarState[] m_variables;
        private GlobalValues m_storage;

        public GlobalValuesState(GlobalValues storage)
        {
            m_storage = storage;
        }

        public override bool Snapshot()
        {
            if (!m_storage) { return false; }
            m_variables = new VarState[m_storage.Count];
            int index = 0;
            foreach(var variable in m_storage.AllVariables)
            {
                m_variables[index++] = new VarState()
                {
                    name = variable.Name,
                    value = variable.Value,
                    type = variable.Type,
                };
            }
            return true;
        }

        protected override bool Restore(GlobalValues values)
        {
            if(m_storage != values) { return false; }

            foreach(var varToRemove in values.AllVariables.Where(v => !m_variables.Any(s => s.name == v.Name)).ToArray())
            {
                values.RemoveVariable(varToRemove.Name);
            }
            
            foreach(var variable in m_variables)
            {
                values.GetOrCreateVariable(variable.name, variable.type).Value = variable.value;
            }


            return true;
        }

        private struct VarState
        {
            public string name;
            public object value;
            public ValuesStorage.ValueType type;
        }
    }
}
