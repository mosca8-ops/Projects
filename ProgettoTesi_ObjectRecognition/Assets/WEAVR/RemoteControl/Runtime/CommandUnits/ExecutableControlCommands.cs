using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Maintenance;

namespace TXT.WEAVR.RemoteControl
{
    [AddComponentMenu("WEAVR/Remote Control/Commands/Executable Controls")]
    public class ExecutableControlCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string> OnExecute;

        private AbstractExecutable m_lastExecutable;
        public AbstractExecutable LastExecutable
        {
            get => m_lastExecutable;
            set
            {
                if (m_lastExecutable != value)
                {
                    if (m_lastExecutable)
                    {
                        m_lastExecutable.onExecute.RemoveListener(ExecutableExecute);
                    }
                    m_lastExecutable = value;
                    if (m_lastExecutable)
                    {
                        m_lastExecutable.onExecute.RemoveListener(ExecutableExecute);
                        m_lastExecutable.onExecute.AddListener(ExecutableExecute);
                    }
                }
            }
        }

        [RemotelyCalled]
        public void Execute(Guid guid)
        {
            var executable = Query.GetComponentByGuid<AbstractExecutable>(guid);
            if (executable)
            {
                LastExecutable = executable;
                executable.Execute();
            }
        }

        [RemotelyCalled]
        public void Execute(string path)
        {
            var executable = Query.Find<AbstractExecutable>(QuerySearchType.Scene, path).First();
            if (executable)
            {
                LastExecutable = executable;
                executable.Execute();
            }
        }

        private void ExecutableExecute()
        {
            OnExecute?.Invoke(LastExecutable.GetHierarchyPath());
        }
    }
}