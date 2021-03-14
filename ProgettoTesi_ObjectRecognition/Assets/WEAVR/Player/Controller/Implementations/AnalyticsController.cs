using System;
using System.Collections.Generic;
using TXT.WEAVR.Communication;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Player.DataSources;
using TXT.WEAVR.Procedure;

namespace TXT.WEAVR.Player.Controller
{

    public class AnalyticsController : BaseController, IAnalyticsController
    {
        public IEnumerable<IAnalyticsUnit> Units { get; private set; }

        public AnalyticsController(IDataProvider provider, IEnumerable<IAnalyticsUnit> analyticsUnits) : base(provider)
        {
            Units = analyticsUnits;
        }

        public async void Begin(IProcedureProxy proxy)
        {
            if(Units == null) { return; }
            var entity = await proxy.GetEntity();
            var asset = await proxy.GetAsset();
            var runner = Weavr.GetInCurrentScene<ProcedureRunner>();
            foreach(var unit in Units)
            {
                try
                {
                    if (unit.Active)
                    {
                        unit.Start(entity, asset, runner.ExecutionMode);
                    }
                }
                catch(Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }
        }

        public void End()
        {
            foreach (var unit in Units)
            {
                try
                {
                    if (unit.Active)
                    {
                        unit.Stop();
                    }
                }
                catch (Exception ex)
                {
                    WeavrDebug.LogException(this, ex);
                }
            }
        }
    }
}
