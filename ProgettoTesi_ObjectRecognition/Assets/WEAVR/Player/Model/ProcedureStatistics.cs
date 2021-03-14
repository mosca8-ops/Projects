using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Player.Model
{
    public class ProcedureStatistics
    {
        [JsonProperty]
        public Guid Id { get; private set; }
        [JsonProperty]
        public List<ExecutionStatistics> Executions { get; private set; }
        
        public float GetAverageExecutionTime()
        {
            int count = 0;
            float sum = 0;
            foreach(var exec in Executions)
            {
                if(exec.Status == ExecutionStatus.Finished)
                {
                    sum += (float)(exec.LastTime - exec.StartTime).TotalSeconds;
                    count++;
                }
            }
            return sum / count;
        }

        public float GetTotalExecutionTime() => Executions.Sum(e => e.Status != ExecutionStatus.Started ? (float)(e.LastTime - e.StartTime).TotalSeconds : 0);

        public ProcedureStatistics(Guid id)
        {
            Id = id;
            Executions = new List<ExecutionStatistics>();
        }

        public ExecutionStatistics NewExecution()
        {
            var exec = new ExecutionStatistics();
            Executions.Add(exec);
            return exec;
        }
    }

    public enum ExecutionStatus
    {
        Started,
        Faulted,
        Finished,
    }

    public class ExecutionStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime LastTime { get; set; }
        public ExecutionStatus Status { get; set; }
        public int StepsExecuted { get; set; }

        public void Snapshot() => LastTime = DateTime.Now;
    }
}
