using TXT.WEAVR.Procedure;

using ProcedureEntity = TXT.WEAVR.Communication.Entities.Procedure;
using ProcedureAsset = TXT.WEAVR.Procedure.Procedure;

namespace TXT.WEAVR.Communication
{
    public interface IAnalyticsUnit
    {
        bool Active { get; }
        void Start(ProcedureEntity entity, ProcedureAsset asset, ExecutionMode executionMode);
        void Stop();
    }
}