using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace TXT.WEAVR.Core
{
    public class SymbolsHelper
    {
        public static bool HasSymbol(string symbol, BuildTargetGroup? buildGroup = null)
        {
            var targetGroup = buildGroup ?? BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup ?? targetGroup).Contains(symbol);
        }

        public static void SetSymbol(string symbol, BuildTargetGroup? buildGroup = null)
        {
            if (HasSymbol(symbol, buildGroup)) { return; }
            var targetGroup = buildGroup ?? BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            if (symbols.EndsWith(";")) { symbols += symbol + ";"; }
            else { symbols += ";" + symbol; }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
        }

        public static void RemoveSymbol(string symbol, BuildTargetGroup? buildGroup = null)
        {
            if (!HasSymbol(symbol, buildGroup)) { return; }
            var targetGroup = buildGroup ?? BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            symbols = symbols.Replace(symbol, "").Replace(";;", ";");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
        }
    }
}
