using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TXT.WEAVR.Procedure
{
    public class ProcedureStateBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool s_saveState;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_saveState = ProcedureStateManager.Instance.SaveState;
            ProcedureStateManager.Instance.SaveState = false;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            ProcedureStateManager.Instance.SaveState = s_saveState;
        }
    }
}
